using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
namespace JingDll {
 public static class SelfTest {
  static List<string> results=new List<string>();
  static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);results.Add("PASS: "+name);}
  static void Throws(Action action,string name){bool threw=false;try{action();}catch{threw=true;}Check(threw,name);}
  public static int Run(string folder){Directory.CreateDirectory(folder);string run=Path.Combine(Path.GetFullPath(folder),"run-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(run);Engine.Home=Path.Combine(run,"data");try{
   var cat=Engine.Catalog();Check(cat.Count>=7000,"at least 7000 real catalog records");Check(cat.Select(x=>x.Name+"|"+x.Architecture+"|"+x.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count()==cat.Count,"catalog keys unique");Check(cat.All(x=>x.Name.EndsWith(".dll")&&!String.IsNullOrEmpty(x.Source)),"catalog provenance present");
   string app=Path.Combine(run,"fixture.exe"),origin=Path.Combine(run,"source"),target=Path.Combine(run,"testdep.dll");Directory.CreateDirectory(origin);string dll=Path.Combine(origin,"testdep.dll");Fixture(app,false,false,"FuncA");Fixture(dll,true,false,"FuncA");Check(Pe.Read(app).Imports.Single().Symbols.Single()=="FuncA","PE import names and symbols");Check(Pe.Read(dll).Exports.Contains("FuncA")&&Pe.Read(dll).Exports.Contains("#1"),"PE exports and ordinal");
   var scan=Engine.Scan(app,n=>{});Check(scan.Single().Problem&&scan.Single().Status=="未找到候选文件","missing dependency detection");var rec=Engine.Replace(app,dll);Check(File.Exists(target)&&Engine.Hash(target)==Engine.Hash(dll),"deploy missing private DLL");Check(Engine.Scan(app,n=>{}).Single().Status=="静态检查通过","compatible DLL passes scan");Engine.Rollback(rec);Check(!File.Exists(target),"rollback removes newly deployed DLL");
   Fixture(target,true,false,"WrongFunc");Check(Engine.Scan(app,n=>{}).Single().Status=="缺少导出函数","export conflict detection");string before=Engine.Hash(target);rec=Engine.Replace(app,dll);Check(File.Exists(rec.Backup)&&Engine.Hash(rec.Backup)==before,"existing file backup integrity");Engine.Rollback(rec);Check(Engine.Hash(target)==before,"restore original bytes");
   Fixture(dll,true,true,"FuncA");Throws(()=>Engine.Replace(app,dll),"wrong architecture blocked");Check(Engine.Scan(app,n=>{}).Count==1,"scan remains usable");Fixture(target,true,true,"FuncA");Check(Engine.Scan(app,n=>{}).Single().Status=="架构冲突","architecture conflict detection");Fixture(dll,true,false,"Other");Throws(()=>Engine.Replace(app,dll),"missing export replacement blocked");
   Fixture(dll,true,false,"FuncA");rec=Engine.Replace(app,dll);File.AppendAllText(target,"changed");Throws(()=>Engine.Rollback(rec),"external change prevents rollback");File.WriteAllBytes(Path.Combine(run,"bad.dll"),new byte[]{1,2,3});Throws(()=>Pe.Read(Path.Combine(run,"bad.dll")),"malformed PE rejected");Check(Engine.IsProtected(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","test.dll")),"Windows directories protected");Check(!Trust.Verify(dll),"unsigned fixture not trusted");
   var kernel=Pe.Read(Path.Combine(Environment.SystemDirectory,"kernel32.dll"));Check(kernel.Exports.Count>100,"real Windows DLL export parse");Check(Engine.Records().Count==3,"repair history persisted");
   Check(Pe.Read(typeof(Program).Assembly.Location).Architecture=="AnyCPU","CLR AnyCPU architecture detection");
   Fixture(dll,true,false,"FuncA");Throws(()=>Engine.ValidateMicrosoftPackage(dll),"unsigned package execution blocked");Check(Engine.IsSystemContract("kernel32.dll"),"KnownDLLs replacement protection");
   Fixture(app,false,true,"FuncA");Check(Pe.Read(app).Architecture=="x64"&&Pe.Read(app).Imports.Single().Symbols.Single()=="FuncA","PE32+ import parsing");
   Fixture(app,false,false,"FuncA");byte[] delay=File.ReadAllBytes(app);Action<int,uint>put=(p,v)=>Array.Copy(BitConverter.GetBytes(v),0,delay,p,4);put(152+104,0);put(152+96+13*8,0x1400);put(0x600,1);put(0x604,0x1150);put(0x610,0x1180);File.WriteAllBytes(app,delay);Check(Pe.Read(app).Imports.Single().Delay,"delay import parsing");
   results.Add("\r\nTotal: "+results.Count+" checks passed. Run: "+run);File.WriteAllLines(Path.Combine(folder,"self-test-results.txt"),results,Encoding.UTF8);return 0;
  }catch(Exception ex){results.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"self-test-results.txt"),results,Encoding.UTF8);return 1;}}
  public static void Fixture(string path,bool dll,bool x64,string symbol){byte[] bytes=new byte[0x1200];Action<int,ushort> w16=(p,v)=>Array.Copy(BitConverter.GetBytes(v),0,bytes,p,2);Action<int,uint>w32=(p,v)=>Array.Copy(BitConverter.GetBytes(v),0,bytes,p,4);Action<int,string>str=(p,s)=>Array.Copy(Encoding.ASCII.GetBytes(s+"\0"),0,bytes,p,s.Length+1);w16(0,0x5a4d);w32(60,128);w32(128,0x4550);w16(132,x64?(ushort)0x8664:(ushort)0x14c);w16(134,1);w16(148,x64?(ushort)240:(ushort)224);w16(150,dll?(ushort)0x2102:(ushort)0x102);int opt=152;w16(opt,x64?(ushort)0x20b:(ushort)0x10b);w32(opt+60,512);w32(opt+(x64?108:92),16);int sec=opt+(x64?240:224),directory=opt+(x64?112:96);str(sec,".rdata");w32(sec+8,4096);w32(sec+12,4096);w32(sec+16,4096);w32(sec+20,512);
   if(!dll){w32(directory+8,0x1100);w32(directory+12,40);w32(0x300,0x1180);w32(0x30c,0x1150);w32(0x310,0x1180);str(0x350,"testdep.dll");w32(0x380,0x11c0);str(0x3c2,symbol);}else{w32(directory,0x1200);w32(directory+4,0x100);w32(0x410,1);w32(0x414,1);w32(0x418,1);w32(0x41c,0x1280);w32(0x420,0x1290);w32(0x424,0x1298);w32(0x480,0x1300);w32(0x490,0x12a0);w16(0x498,0);str(0x4a0,symbol);}File.WriteAllBytes(path,bytes);
  }
 }
}
