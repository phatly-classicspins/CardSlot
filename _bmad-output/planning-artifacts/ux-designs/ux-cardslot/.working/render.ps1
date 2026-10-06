# Render mock.html to PNG with headless Edge (D-016).
# Usage: .\render.ps1 -OutDir directions -Jobs "a:home:1920","a:gameplay:1920"
#   job = <theme>:<screen>:<height>; output file = <theme>-<screen>[-<height>].png
param(
  [string]$OutDir = "directions",
  [string[]]$Jobs,
  [string]$Page = "mock.html"
)
$edge = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $here $OutDir
New-Item -ItemType Directory -Force $out | Out-Null
$profile = Join-Path $env:TEMP "cardslot-mock-edge"
$url = "file:///" + ($here -replace "\\", "/") + "/$Page"
foreach ($j in $Jobs) {
  $t, $s, $h = $j.Split(':')
  $name = if ($h -eq "1920") { "$t-$s.png" } else { "$t-$s-$h.png" }
  $argv = @("--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run", "--user-data-dir=$profile",
            "--force-device-scale-factor=1", "--window-size=1080,$h", "--virtual-time-budget=4000",
            "--screenshot=$(Join-Path $out $name)", "$url`?theme=$t&screen=$s&h=$h")
  Start-Process -FilePath $edge -ArgumentList $argv -Wait -NoNewWindow -RedirectStandardError (Join-Path $env:TEMP "cardslot-edge-err.txt")
  if (Test-Path (Join-Path $out $name)) { "ok   $name" } else { "FAIL $name" }
}
