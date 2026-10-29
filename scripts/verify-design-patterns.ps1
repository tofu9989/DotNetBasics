param(
    [string]$DocPath = (Join-Path $PSScriptRoot '..\DESIGN_PATTERNS.md')
)

$ErrorActionPreference = 'Stop'
$doc = (Resolve-Path -LiteralPath $DocPath).Path
$lines = Get-Content -LiteralPath $doc -Encoding utf8
$failures = [System.Collections.Generic.List[string]]::new()

$inFence = $false
$slugs = [System.Collections.Generic.HashSet[string]]::new()

foreach ($line in $lines) {
    if ($line -match '^```') {
        $inFence = -not $inFence
        continue
    }

    if (-not $inFence -and $line -match '^#{1,6}\s+(.+?)\s*$') {
        $text = $Matches[1]
        $slug = $text.ToLowerInvariant() -replace '[^\p{L}\p{N}\s-]', '' -replace '\s+', '-'
        [void]$slugs.Add($slug)
    }
}

if ($inFence) {
    $failures.Add('Code fences are unbalanced.')
}

$tocLinks = [regex]::Matches(($lines -join "`n"), '\]\(#([^)]+)\)')
foreach ($match in $tocLinks) {
    $target = $match.Groups[1].Value
    if (-not $slugs.Contains($target)) {
        $failures.Add("Dead table-of-contents link: #$target")
    }
}

$numbers = [System.Collections.Generic.List[int]]::new()
foreach ($line in $lines) {
    if ($line -match '^### (\d+)\.\s+(.+)$') {
        $numbers.Add([int]$Matches[1])
    }
}

if ($numbers.Count -ne 23) {
    $failures.Add("Expected 23 numbered pattern sections, found $($numbers.Count).")
}

for ($i = 1; $i -le 23; $i++) {
    if (-not $numbers.Contains($i)) {
        $failures.Add("Missing pattern section $i.")
    }
}

$sdk = (dotnet --version).Split('.')[0]
$tfm = "net$sdk.0"
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ('pattern-verify-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null

try {
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$tfm</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
"@
    Set-Content -LiteralPath (Join-Path $temp 'Verify.csproj') -Value $project -Encoding utf8
    Set-Content -LiteralPath (Join-Path $temp 'Program.cs') -Value 'return 0;' -Encoding utf8

    $current = $null
    $blockNumber = 0
    $compiled = 0

    foreach ($line in $lines) {
        if ($line -match '^```csharp\s*$') {
            $current = [System.Collections.Generic.List[string]]::new()
            continue
        }

        if ($line -match '^```\s*$') {
            if ($null -ne $current) {
                $blockNumber++
                $code = $current -join "`n"
                if ($code -match 'namespace Patterns\.' -or
                    $code -match 'public abstract class Specification<T>' -or
                    $code -match 'public abstract record Result<T>' -or
                    $code -match 'public sealed class SmtpOptions') {
                    $compiled++
                    Set-Content -LiteralPath (Join-Path $temp "Block$blockNumber.cs") -Value $code -Encoding utf8
                }
                $current = $null
            }
            continue
        }

        if ($null -ne $current) {
            [void]$current.Add($line)
        }
    }

    if ($compiled -eq 0) {
        $failures.Add('No pattern code blocks found.')
    }

    $buildOutput = & dotnet build $temp --nologo -v q 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("Pattern code blocks failed to compile with warnings as errors.`n$buildOutput")
    }
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Output "FAIL: $_" }
    exit 1
}

Write-Output "PASS: $compiled pattern and architecture code blocks compiled with warnings as errors."
Write-Output "PASS: $($tocLinks.Count) table-of-contents links resolve."
Write-Output "PASS: 23 numbered pattern sections present."
