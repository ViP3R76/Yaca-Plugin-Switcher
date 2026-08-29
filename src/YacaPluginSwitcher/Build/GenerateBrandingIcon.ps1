param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [Parameter(Mandatory = $true)]
    [string]$Output
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$outputPath = [System.IO.Path]::GetFullPath($Output)
$outputDirectory = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$sourceBitmap = [System.Drawing.Bitmap]::FromFile($sourcePath)
try {
    # Windows Explorer and the shell expect a real multi-resolution ICO.
    # A single GetHicon() result is only a single low-resolution icon and can
    # therefore produce the wrong/poor icon in Explorer and shortcuts.
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $pngPayloads = New-Object System.Collections.Generic.List[byte[]]

    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.DrawImage($sourceBitmap, 0, 0, $size, $size)

            $stream = New-Object System.IO.MemoryStream
            try {
                $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
                $pngPayloads.Add($stream.ToArray())
            }
            finally {
                $stream.Dispose()
            }
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }

    # ICO header: reserved, type=icon, image count.
    $fileStream = [System.IO.File]::Create($outputPath)
    $writer = New-Object System.IO.BinaryWriter $fileStream
    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$sizes.Count)

        $offset = 6 + (16 * $sizes.Count)
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $size = $sizes[$i]
            $payload = $pngPayloads[$i]

            # ICONDIRENTRY
            $writer.Write([byte]$(if ($size -ge 256) { 0 } else { $size }))
            $writer.Write([byte]$(if ($size -ge 256) { 0 } else { $size }))
            $writer.Write([byte]0)       # color count
            $writer.Write([byte]0)       # reserved
            $writer.Write([uint16]1)     # planes
            $writer.Write([uint16]32)    # bits per pixel
            $writer.Write([uint32]$payload.Length)
            $writer.Write([uint32]$offset)

            $offset += $payload.Length
        }

        foreach ($payload in $pngPayloads) {
            $writer.Write($payload)
        }
    }
    finally {
        $writer.Dispose()
        $fileStream.Dispose()
    }
}
finally {
    $sourceBitmap.Dispose()
}

Write-Host "Generated multi-resolution branding icon: $outputPath"