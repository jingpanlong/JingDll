using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
class CatalogBuilder {
 static int Main(string[] args) {
  var records=new SortedDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  string win=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
  var roots=new[]{Path.Combine(win,"System32"),Path.Combine(win,"SysWOW64"),Path.Combine(win,"Microsoft.NET"),Path.Combine(win,"WinSxS")};
  foreach(var root in roots) { Walk(root,root,records, root.EndsWith("WinSxS")?2:8); Console.WriteLine("Completed "+root+": "+records.Count); }
  File.WriteAllLines(args[0],new[]{"Name\tArchitecture\tVersion\tCategory\tDescription\tSource"}.Concat(records.Values),new UTF8Encoding(false));
  Console.WriteLine("Records: "+records.Count+"; unique names: "+records.Values.Select(x=>x.Split('\t')[0]).Distinct(StringComparer.OrdinalIgnoreCase).Count());
  return records.Count>=7000?0:1;
 }
 static string Clean(string s){return (s??"").Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');}
 static void Walk(string dir,string origin,SortedDictionary<string,string> rows,int depth){
  try { foreach(string p in Directory.EnumerateFiles(dir,"*.dll")) try {
   string name=Path.GetFileName(p).ToLowerInvariant();if(!name.EndsWith(".dll",StringComparison.Ordinal))continue;
   string arch="Unknown";
   using(var s=File.Open(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var b=new BinaryReader(s)) {
    if(s.Length<64||b.ReadUInt16()!=0x5a4d)continue;s.Position=0x3c;int offset=b.ReadInt32();if(offset<0||offset+24>s.Length)continue;
    s.Position=offset;if(b.ReadUInt32()!=0x4550)continue;ushort m=b.ReadUInt16();arch=m==0x8664?"x64":m==0x14c?"x86":m==0xaa64?"ARM64":"Other";
   }
   var v=FileVersionInfo.GetVersionInfo(p);string ver=Clean(v.FileVersion);string cat=name.StartsWith("api-ms-")||name.StartsWith("ext-ms-")?"API Set":name.StartsWith("msvc")||name.StartsWith("vcruntime")||name.StartsWith("ucrt")?"Visual C++":origin.EndsWith("Microsoft.NET")?".NET":"Windows";
   string row=String.Join("\t",new[]{name,arch,ver,cat,Clean(v.FileDescription),"Windows reference inventory / "+Path.GetFileName(origin)});
   rows[name+"|"+arch+"|"+ver]=row;
  }catch{} if(depth>0) foreach(string sub in Directory.EnumerateDirectories(dir)) Walk(sub,origin,rows,depth-1);
  }catch{}
 }
}
