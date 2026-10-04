# Genera app.ico (16/24/32/48/256 px, PNG dentro ICO) per l'eseguibile PhoneVolume.
Add-Type -AssemblyName System.Drawing
$sizes = 16, 24, 32, 48, 256
$pngs = @()
foreach ($s in $sizes) {
    $u = $s / 32.0
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    $r = New-Object System.Drawing.RectangleF (7 * $u), (1.5 * $u), (18 * $u), (29 * $u)
    $rad = 4.5 * $u; $d = $rad * 2
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $p.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $p.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $p.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(32, 38, 48))), $p)
    $g.DrawPath((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(0, 150, 255)), (2.2 * $u)), $p)

    # barre del volume crescenti
    $blue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0, 150, 255))
    $bw = 2.4 * $u; $gap = 0.9 * $u; $x0 = 10.4 * $u; $base = 26 * $u
    for ($i = 0; $i -lt 4; $i++) {
        $h = (5 + $i * 4.5) * $u
        $g.FillRectangle($blue, ($x0 + $i * ($bw + $gap)), ($base - $h), $bw, $h)
    }
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $dim = if ($s -ge 256) { 0 } else { $s }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32)
    $w.Write([UInt32]$pngs[$i].Length); $w.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($b in $pngs) { $w.Write($b) }
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'app.ico'), $out.ToArray())
