$ErrorActionPreference = 'Stop'
$lines = [Collections.Generic.List[string]]::new()
$known = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($line in [IO.File]::ReadLines((Join-Path $PSScriptRoot 'catalog-reference.tsv'))) {
    if (-not ($line.StartsWith("Name`t") -or $line.Split("`t")[0].EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase))) { continue }
    $lines.Add($line)
    [void]$known.Add($line.Split("`t")[0])
}
foreach ($name in [IO.File]::ReadLines((Join-Path $PSScriptRoot 'winbindex-dll-names.txt'))) {
    $normalizedName = $name.ToLowerInvariant()
    if ($known.Add($normalizedName)) {
        $lines.Add("$normalizedName`t未标注`t未标注`tWindows 索引`t历史 DLL 名称索引；未收录版本/架构，不表示当前系统必须安装`tWinbindex (GPL-3.0) / https://winbindex.m417z.com/data/filenames.json / 2026-10-24")
    }
}
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'catalog.tsv'), $lines, [Text.UTF8Encoding]::new($false))
Write-Host "数据库 $($lines.Count - 1) 条记录 / $($known.Count - 1) 个独立 DLL 名称"
