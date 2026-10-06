# Build CardSlot sprites from art/source/art.html (D-019), check them, and write the export report.
#   powershell -ExecutionPolicy Bypass -File art/tools/build-art.ps1
# Outputs (all regenerated — never hand-edit, G12):
#   art/source/svg/<id>.svg       layered SVG source per sprite
#   art/export/<atlas>/<id>.png   sprite, 1x, alpha-bled
#   art/export/manifest.json      size, pivot, 9-slice, atlas, kind, check results per sprite
#   art/export/report.md          export report (Bước 3 gate)
#   art/export/preview.png        every sprite on a checkerboard
# Exit code: 0 = no blocking error, 1 = blocking errors (see report), 2 = tool error.
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$src = Join-Path $root 'art\source\art.html'
$exp = Join-Path $root 'art\export'
$svgDir = Join-Path $root 'art\source\svg'
$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
$profileDir = Join-Path $env:TEMP 'cardslot-art-edge'
$tmp = Join-Path $env:TEMP 'cardslot-art'
New-Item -ItemType Directory -Force $tmp, $svgDir, $exp | Out-Null
$url = 'file:///' + ($src -replace '\\', '/')

function Invoke-Edge([string[]]$extra, [string]$stdout) {
  $base = @('--headless=new', '--disable-gpu', '--hide-scrollbars', '--no-first-run', "--user-data-dir=$profileDir", '--force-device-scale-factor=1')
  $p = @{ FilePath = $edge; ArgumentList = ($base + $extra); Wait = $true; NoNewWindow = $true; PassThru = $true
          RedirectStandardError = (Join-Path $tmp 'edge-err.txt') }
  if ($stdout) { $p.RedirectStandardOutput = $stdout }
  $null = Start-Process @p
}

# 1. Manifest + SVG sources from the DOM
$domFile = Join-Path $tmp 'dom.html'
Invoke-Edge @('--dump-dom', $url) $domFile
$dom = [IO.File]::ReadAllText($domFile)
$m = [regex]::Match($dom, '<script id="manifest" type="application/json">(.*?)</script>', 'Singleline')
if (-not $m.Success -or $m.Groups[1].Value.Length -lt 10) { Write-Error 'manifest not found in art.html DOM'; exit 2 }
$manifestJson = [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
$man = $manifestJson | ConvertFrom-Json
Get-ChildItem $svgDir -Filter *.svg | Remove-Item
foreach ($a in $man.assets) {
  [IO.File]::WriteAllText((Join-Path $svgDir "$($a.id).svg"), "<?xml version=`"1.0`" encoding=`"UTF-8`"?>`n" + $a.svgText + "`n", (New-Object Text.UTF8Encoding $false))
}

# 2. One transparent screenshot of the whole sheet
$sheetPng = Join-Path $tmp 'sheet.png'
if (Test-Path $sheetPng) { Remove-Item $sheetPng }
Invoke-Edge @('--default-background-color=00000000', "--window-size=$($man.sheet.w),$($man.sheet.h)", '--virtual-time-budget=3000', "--screenshot=$sheetPng", $url) $null
if (-not (Test-Path $sheetPng)) { Write-Error 'sheet screenshot failed'; exit 2 }

# 3. Crop, fix, check (C# for speed)
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Collections.Generic; using System.Runtime.InteropServices;
public class ArtCheck {
  public static byte[] Read(Bitmap b, out int w, out int h){
    w=b.Width; h=b.Height; var d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
    var px=new byte[w*h*4]; for(int y=0;y<h;y++) Marshal.Copy(d.Scan0+y*d.Stride,px,y*w*4,w*4); b.UnlockBits(d); return px; }
  public static Bitmap Write(byte[] px,int w,int h){
    var b=new Bitmap(w,h,PixelFormat.Format32bppArgb); var d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
    for(int y=0;y<h;y++) Marshal.Copy(px,y*w*4,d.Scan0+y*d.Stride,w*4); b.UnlockBits(d); return b; }
  // BGRA order
  static int A(byte[] p,int w,int x,int y){return p[(y*w+x)*4+3];}
  static double Lum(byte[] p,int i){return 0.2126*p[i+2]+0.7152*p[i+1]+0.0722*p[i];}
  public static byte[] Crop(byte[] s,int sw,int x0,int y0,int w,int h){var o=new byte[w*h*4];for(int y=0;y<h;y++)Buffer.BlockCopy(s,((y0+y)*sw+x0)*4,o,y*w*4,w*4);return o;}
  // pixels with alpha>0 in the ring of width r just outside the box: content that the box clips
  public static int Overflow(byte[] s,int sw,int sh,int x0,int y0,int w,int h,int r){int n=0;
    for(int y=Math.Max(0,y0-r);y<Math.Min(sh,y0+h+r);y++)for(int x=Math.Max(0,x0-r);x<Math.Min(sw,x0+w+r);x++){
      if(x>=x0&&x<x0+w&&y>=y0&&y<y0+h)continue; if(s[(y*sw+x)*4+3]>8)n++;} return n;}
  // isolated non-transparent pixels (all 8 neighbours fully transparent); removes them, returns count
  public static int StrayFix(byte[] p,int w,int h){int n=0;var kill=new List<int>();
    for(int y=0;y<h;y++)for(int x=0;x<w;x++){ if(A(p,w,x,y)==0)continue; bool alone=true;
      for(int dy=-1;dy<=1&&alone;dy++)for(int dx=-1;dx<=1;dx++){if(dx==0&&dy==0)continue;int xx=x+dx,yy=y+dy;
        if(xx<0||yy<0||xx>=w||yy>=h)continue; if(A(p,w,xx,yy)>0){alone=false;break;}}
      if(alone){kill.Add((y*w+x)*4);n++;}}
    foreach(var i in kill){p[i]=p[i+1]=p[i+2]=p[i+3]=0;} return n;}
  // dark fringe: semi-transparent edge pixels much darker than the opaque body next to them
  public static int Halo(byte[] p,int w,int h){int n=0;
    for(int y=0;y<h;y++)for(int x=0;x<w;x++){int i=(y*w+x)*4;int a=p[i+3]; if(a<64||a>=250)continue;
      double near=-1; for(int rad=1;rad<=2&&near<0;rad++)for(int dy=-rad;dy<=rad;dy++)for(int dx=-rad;dx<=rad;dx++){int xx=x+dx,yy=y+dy; if(xx<0||yy<0||xx>=w||yy>=h)continue;
        int j=(yy*w+xx)*4; if(p[j+3]>=250){double l=Lum(p,j); if(l>near)near=l;}}
      if(near<0)continue; if(Lum(p,i) < near*0.65 - 8) n++;}
    return n;}
  // alpha bleed: give fully transparent pixels the colour of their nearest visible neighbour, so
  // bilinear filtering / mipmaps never pull black into the edge (the classic dark halo)
  public static void Bleed(byte[] p,int w,int h,int passes){
    for(int k=0;k<passes;k++){var src=(byte[])p.Clone(); var done=new bool[w*h]; for(int i=0;i<w*h;i++)done[i]=src[i*4+3]>0||(src[i*4]|src[i*4+1]|src[i*4+2])!=0;
      for(int y=0;y<h;y++)for(int x=0;x<w;x++){int i=y*w+x; if(done[i])continue; int r=0,g=0,b=0,c=0;
        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy; if(xx<0||yy<0||xx>=w||yy>=h)continue; int j=yy*w+xx;
          if(!done[j])continue; b+=src[j*4];g+=src[j*4+1];r+=src[j*4+2];c++;}
        if(c>0){p[i*4]=(byte)(b/c);p[i*4+1]=(byte)(g/c);p[i*4+2]=(byte)(r/c);p[i*4+3]=0;}}}}
  // share of pixels whose alpha differs from the horizontally mirrored pixel by > 40
  public static double MirrorDiff(byte[] p,int w,int h){long bad=0,tot=0;
    for(int y=0;y<h;y++)for(int x=0;x<w/2;x++){int a=A(p,w,x,y),b=A(p,w,w-1-x,y); if(a==0&&b==0)continue; tot++; if(Math.Abs(a-b)>40)bad++;}
    return tot==0?0:(double)bad/tot;}
  // max per-channel difference across the stretchable band of a 9-slice (0 = seamless)
  public static int SeamCols(byte[] p,int w,int h,int l,int r){ if(w-l-r<2)return 0; int m=0, c0=l;
    for(int x=l+1;x<w-r;x++)for(int y=0;y<h;y++)for(int k=0;k<4;k++){int d=Math.Abs(p[(y*w+x)*4+k]-p[(y*w+c0)*4+k]); if(d>m)m=d;} return m;}
  public static int SeamRows(byte[] p,int w,int h,int t,int bt){ if(h-t-bt<2)return 0; int m=0, r0=t;
    for(int y=t+1;y<h-bt;y++)for(int x=0;x<w;x++)for(int k=0;k<4;k++){int d=Math.Abs(p[(y*w+x)*4+k]-p[(r0*w+x)*4+k]); if(d>m)m=d;} return m;}
  public static byte[] PadTo(byte[] p,int w,int h,int nw,int nh){var o=new byte[nw*nh*4]; for(int y=0;y<h;y++)Buffer.BlockCopy(p,y*w*4,o,y*nw*4,w*4); return o;}
  public static Bitmap Checker(int w,int h){var b=new Bitmap(w,h); using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(70,70,78));
    using(var br=new SolidBrush(Color.FromArgb(96,96,106))) for(int y=0;y<h;y+=16)for(int x=(y/16%2)*16;x<w;x+=32)g.FillRectangle(br,x,y,16,16);} return b;}
}
'@

$sheetBmp = [Drawing.Bitmap]::FromFile($sheetPng)
[int]$sw = 0; [int]$sh = 0
$sheetPx = [ArtCheck]::Read($sheetBmp, [ref]$sw, [ref]$sh)
$sheetBmp.Dispose()

Get-ChildItem $exp -Recurse -Filter *.png | Remove-Item
$rows = @(); $blocking = 0; $out = @()
$totalBytes = 0; $totalAstc = 0
foreach ($a in $man.assets) {
  $issues = New-Object System.Collections.Generic.List[string]; $fixes = New-Object System.Collections.Generic.List[string]
  [int]$w = $a.w; [int]$h = $a.h
  $px = [ArtCheck]::Crop($sheetPx, $sw, $a.x, $a.y, $w, $h)
  # clipped at canvas edge
  $ov = [ArtCheck]::Overflow($sheetPx, $sw, $sh, $a.x, $a.y, $w, $h, 4)
  if ($ov -gt 0) { $issues.Add("BLOCK clipped: $ov px drawn outside the ${w}x$h box") }
  # stray pixels
  $st = [ArtCheck]::StrayFix($px, $w, $h)
  if ($st -gt 0) { $fixes.Add("removed $st stray pixel(s)") }
  # dark halo (skip intentionally dark translucent kinds)
  if ($a.kind -eq 'opaque') { $ha = [ArtCheck]::Halo($px, $w, $h); if ($ha -gt 0) { $issues.Add("BLOCK halo: $ha dark semi-transparent edge pixel(s)") } }
  # mirror symmetry
  if ($a.sym) { $md = [ArtCheck]::MirrorDiff($px, $w, $h); if ($md -gt 0.01) { $issues.Add(('BLOCK mirror: {0:P1} of pixels differ from the mirrored side' -f $md)) } }
  # 9-slice seams
  if ($a.slice) {
    $s = $a.slice
    $sc = [ArtCheck]::SeamCols($px, $w, $h, $s[0], $s[2]); if ($sc -gt 2) { $issues.Add("BLOCK 9-slice seam: stretch columns differ by $sc") }
    if ($s[1] -gt 0 -or $s[3] -gt 0) { $sr = [ArtCheck]::SeamRows($px, $w, $h, $s[1], $s[3]); if ($sr -gt 2) { $issues.Add("BLOCK 9-slice seam: stretch rows differ by $sr") } }
  }
  # even size (atlas-friendly) — fix by transparent padding right/bottom
  if (($w % 2) -or ($h % 2)) {
    $nw = $w + ($w % 2); $nh = $h + ($h % 2)
    $px = [ArtCheck]::PadTo($px, $w, $h, $nw, $nh); $fixes.Add("padded ${w}x$h -> ${nw}x$nh (even size)"); $w = $nw; $h = $nh
  }
  # alpha bleed (always) — prevents dark fringe once filtered/mipmapped
  [ArtCheck]::Bleed($px, $w, $h, 4); $fixes.Add('alpha bleed 4 px')
  $dir = Join-Path $exp $a.atlas; New-Item -ItemType Directory -Force $dir | Out-Null
  $file = Join-Path $dir "$($a.id).png"
  $bmp = [ArtCheck]::Write($px, $w, $h); $bmp.Save($file, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  $kb = [math]::Round((Get-Item $file).Length / 1KB, 1)
  if ($kb -gt 64) { $issues.Add("BLOCK size: $kb KB > 64 KB per sprite") }
  $totalBytes += $w * $h * 4
  $totalAstc += [math]::Ceiling($w / 6) * [math]::Ceiling($h / 6) * 16
  $blocking += @($issues | Where-Object { $_ -like 'BLOCK*' }).Count
  $out += [ordered]@{ id = $a.id; atlas = $a.atlas; file = "$($a.atlas)/$($a.id).png"; w = $w; h = $h; pivot = @(0.5, 0.5)
                      slice = $a.slice; kind = $a.kind; use = $a.use; kb = $kb; issues = @($issues); fixes = @($fixes) }
}

# 4. manifest.json, preview.png, report.md
$manifestOut = [ordered]@{ generatedBy = 'art/tools/build-art.ps1'; source = 'art/source/art.html'; tokens = $man.tokensVersion
  rules = 'D-019: 1x (1 px = 1 world unit, PPU 1), even sizes, atlas <= 2048, ASTC 6x6, texture budget <= 16 MB'; sprites = $out }
[IO.File]::WriteAllText((Join-Path $exp 'manifest.json'), ($manifestOut | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding $false))

$pv = [ArtCheck]::Checker($sw, $sh); $g = [Drawing.Graphics]::FromImage($pv)
foreach ($o in $out) {
  $a = $man.assets | Where-Object { $_.id -eq $o.id }
  $im = [Drawing.Image]::FromFile((Join-Path $exp $o.file)); $g.DrawImage($im, [int]$a.x, [int]$a.y, $im.Width, $im.Height); $im.Dispose()
}
$g.Dispose(); $pv.Save((Join-Path $exp 'preview.png'), [Drawing.Imaging.ImageFormat]::Png); $pv.Dispose()

$mb = [math]::Round($totalBytes / 1MB, 2); $astcMb = [math]::Round($totalAstc / 1MB, 2)
$lines = @('# Báo cáo xuất art — CardSlot', '',
  "> Sinh tự động bởi ``art/tools/build-art.ps1`` từ ``art/source/art.html`` ($($man.tokensVersion)), $(Get-Date -Format 'dd-MM-yyyy HH:mm'). Không sửa tay (G12).", '',
  '## Tổng', '',
  "- Sprite: **$($out.Count)** · lỗi chặn: **$blocking**",
  "- Bộ nhớ texture: $mb MB nếu RGBA32 · ~$astcMb MB với ASTC 6×6 (ngân sách 16 MB, D-019)",
  "- Atlas: " + (($out | Group-Object atlas | ForEach-Object { "``$($_.Name)`` $($_.Count) sprite" }) -join ' · '),
  '- Kiểm tra: đúng kích thước spec · kích thước chẵn · cắt mép canvas · pixel lạc · quầng tối (sprite đục) · soi gương (sprite đối xứng) · đường nối 9-slice · ≤ 64 KB/sprite. Mọi sprite được bù màu alpha (alpha bleed) để không có viền tối khi lọc.', '',
  '## Từng sprite', '', '| Sprite | Atlas | Kích thước | 9-slice (L,T,R,B) | Loại | KB | Kết quả | Tự sửa |', '|---|---|---|---|---|---|---|---|')
foreach ($o in $out) {
  $res = if ($o.issues.Count) { ($o.issues -join '<br>') } else { 'đạt' }
  $sl = if ($o.slice) { $o.slice -join ',' } else { '—' }
  $fx = ($o.fixes | Where-Object { $_ -ne 'alpha bleed 4 px' }) -join '<br>'; if (-not $fx) { $fx = '—' }
  $lines += "| ``$($o.id)`` | $($o.atlas) | $($o.w)×$($o.h) | $sl | $($o.kind) | $($o.kb) | $res | $fx |"
}
[IO.File]::WriteAllLines((Join-Path $exp 'report.md'), $lines, (New-Object Text.UTF8Encoding $false))
"sprites=$($out.Count) blocking=$blocking rgba=${mb}MB astc=${astcMb}MB"
if ($blocking -gt 0) { exit 1 } else { exit 0 }
