param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$resolvedInputPath = (Resolve-Path -LiteralPath $InputPath).Path
$source = New-Object System.Windows.Media.Imaging.BitmapImage
$source.BeginInit()
$source.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
$source.UriSource = [System.Uri]::new($resolvedInputPath, [System.UriKind]::Absolute)
$source.EndInit()
$source.Freeze()

$sizes = @(16, 24, 32, 48, 64, 96, 128, 256)
$pngImages = @()

foreach ($size in $sizes) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    [System.Windows.Media.RenderOptions]::SetBitmapScalingMode(
        $visual,
        [System.Windows.Media.BitmapScalingMode]::HighQuality)

    $scale = [Math]::Min(
        $size / [double]$source.PixelWidth,
        $size / [double]$source.PixelHeight)
    $width = $source.PixelWidth * $scale
    $height = $source.PixelHeight * $scale
    $left = ($size - $width) / 2
    $top = ($size - $height) / 2

    $drawingContext = $visual.RenderOpen()
    $drawingContext.DrawImage(
        $source,
        [System.Windows.Rect]::new($left, $top, $width, $height))
    $drawingContext.Close()

    $rendered = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
        $size,
        $size,
        96,
        96,
        [System.Windows.Media.PixelFormats]::Pbgra32)
    $rendered.Render($visual)
    $rendered.Freeze()

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add(
        [System.Windows.Media.Imaging.BitmapFrame]::Create($rendered))

    $stream = New-Object System.IO.MemoryStream
    try {
        $encoder.Save($stream)
        $pngImages += ,$stream.ToArray()
    }
    finally {
        $stream.Dispose()
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$fileStream = New-Object System.IO.FileStream(
    $OutputPath,
    [System.IO.FileMode]::Create,
    [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)
$writer = New-Object System.IO.BinaryWriter($fileStream)

try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$sizes.Count)

    $dataOffset = 6 + (16 * $sizes.Count)
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $size = $sizes[$index]
        $dimension = if ($size -eq 256) { 0 } else { $size }

        $writer.Write([Byte]$dimension)
        $writer.Write([Byte]$dimension)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngImages[$index].Length)
        $writer.Write([UInt32]$dataOffset)

        $dataOffset += $pngImages[$index].Length
    }

    foreach ($pngImage in $pngImages) {
        $writer.Write($pngImage)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}
