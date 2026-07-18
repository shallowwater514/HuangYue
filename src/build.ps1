$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir = Join-Path $projectDir 'dist'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $projectDir 'Program.cs'
$realtimeSource = Join-Path $projectDir 'RealtimeBattle.cs'
$output = Join-Path $outputDir 'HuangYueDemo.exe'

if (-not (Test-Path $compiler)) {
    throw "C# compiler not found: $compiler"
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /out:$output $source $realtimeSource

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code: $LASTEXITCODE"
}

Write-Output $output
