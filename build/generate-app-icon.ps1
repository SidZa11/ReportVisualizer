param(
    [Parameter(Mandatory = $true)]
    [string]$CfgPath,
    [Parameter(Mandatory = $true)]
    [string]$LogoDir,
    [Parameter(Mandatory = $true)]
    [string]$DstIcoPath
)

$ErrorActionPreference = 'SilentlyContinue'

$replaceApp = $false
if (Test-Path -LiteralPath $CfgPath) {
    $cfgRaw = [IO.File]::ReadAllText($CfgPath)
    $pattern = '"replace_app"\s*:\s*(true|false)'
    $m = [regex]::Match($cfgRaw, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($m.Success -and $m.Groups[1].Value.ToLowerInvariant() -eq 'true') {
        $replaceApp = $true
    }
}

$exts = @('.ico', '.png', '.jpg', '.jpeg', '.bmp', '.gif')
function Find-LogoFile([string]$base) {
    foreach ($e in $exts) {
        $p = Join-Path $LogoDir ($base + $e)
        if (Test-Path -LiteralPath $p) {
            return $p
        }
    }
    return $null
}

if ($replaceApp) {
    $chosen = Find-LogoFile 'client'
    if (-not $chosen) { $chosen = Find-LogoFile 'app' }
}
else {
    $chosen = Find-LogoFile 'app'
    if (-not $chosen) { $chosen = Find-LogoFile 'client' }
}

if (-not $chosen) {
    Write-Host 'LOGO: no logo files found'
    exit 0
}

Write-Host ('LOGO: chosen=' + $chosen + ' (replace_app=' + $replaceApp + ')')

$ext = [IO.Path]::GetExtension($chosen).ToLowerInvariant()
$dstDir = Split-Path -Parent $DstIcoPath
if (-not (Test-Path -LiteralPath $dstDir)) {
    New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
}

if ($ext -eq '.ico') {
    Copy-Item -LiteralPath $chosen -Destination $DstIcoPath -Force
    Write-Host ('LOGO: copied to ' + $DstIcoPath)
    exit 0
}

try {
    Add-Type -AssemblyName System.Drawing
    $sizes = @(16, 32, 48, 64, 128, 256)
    $src = [Drawing.Image]::FromFile($chosen)
    $frames = New-Object 'System.Collections.Generic.List[byte[]]'
    $count = $sizes.Length
    $header = New-Object byte[] (6 + 16 * $count)
    $header[0] = 0; $header[1] = 0
    $header[2] = 1; $header[3] = 0
    $header[4] = [byte]($count -band 0xFF)
    $header[5] = [byte](($count -shr 8) -band 0xFF)
    $offset = 6 + 16 * $count

    for ($i = 0; $i -lt $count; $i++) {
        $s = $sizes[$i]
        $bmp = New-Object Drawing.Bitmap($s, $s, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.Clear([Drawing.Color]::Transparent)
        $ratio = [Math]::Min([double]$s / $src.Width, [double]$s / $src.Height)
        $w = [int][Math]::Round($src.Width * $ratio)
        $h = [int][Math]::Round($src.Height * $ratio)
        $x = [int](($s - $w) / 2)
        $y = [int](($s - $h) / 2)
        $g.DrawImage($src, $x, $y, $w, $h)
        $g.Dispose()
        $ms = New-Object IO.MemoryStream
        $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png)
        $bytes = $ms.ToArray()
        $frames.Add($bytes)
        $bmp.Dispose()

        $idx = 6 + 16 * $i
        if ($s -eq 256) {
            $szByte = [byte]0
        }
        else {
            $szByte = [byte]$s
        }
        $header[$idx + 0] = $szByte
        $header[$idx + 1] = $szByte
        $header[$idx + 2] = 0
        $header[$idx + 3] = 0
        $header[$idx + 4] = 1
        $header[$idx + 5] = 0
        $header[$idx + 6] = 32
        $header[$idx + 7] = 0
        $len = $bytes.Length
        $header[$idx + 8]  = [byte]($len -band 0xFF)
        $header[$idx + 9]  = [byte](($len -shr 8) -band 0xFF)
        $header[$idx + 10] = [byte](($len -shr 16) -band 0xFF)
        $header[$idx + 11] = [byte](($len -shr 24) -band 0xFF)
        $header[$idx + 12] = [byte]($offset -band 0xFF)
        $header[$idx + 13] = [byte](($offset -shr 8) -band 0xFF)
        $header[$idx + 14] = [byte](($offset -shr 16) -band 0xFF)
        $header[$idx + 15] = [byte](($offset -shr 24) -band 0xFF)
        $offset += $len
    }

    $fs = [IO.File]::Open($DstIcoPath, [IO.FileMode]::Create)
    $fs.Write($header, 0, $header.Length)
    foreach ($b in $frames) {
        $fs.Write($b, 0, $b.Length)
    }
    $fs.Close()
    $src.Dispose()
    Write-Host ('LOGO: generated multi-size ICO at ' + $DstIcoPath)
}
catch {
    Write-Host ('LOGO WARNING: ' + $_.Exception.Message)
    try {
        Copy-Item -LiteralPath $chosen -Destination $DstIcoPath -Force
    }
    catch { }
}
