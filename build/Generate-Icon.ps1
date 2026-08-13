param(
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'icon.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$bitmap = New-Object System.Drawing.Bitmap 256, 256
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::FromArgb(20, 24, 27))

$orange = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(239, 130, 39))
$horn = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(91, 53, 43))
$cream = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(248, 241, 205))
$dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(31, 25, 35))
$pink = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(232, 67, 101))
$outline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(9, 11, 14)), 8
$outline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$detail = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(59, 35, 36)), 5
$detail.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$detail.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$tonguePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(232, 67, 101)), 16
$tonguePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$tonguePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$tonguePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$toothOutline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(31, 25, 35)), 3
$toothOutline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$textOuter = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(9, 11, 14)), 8
$textOuter.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$textInner = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(248, 241, 205)), 4
$textInner.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$fontFamily = New-Object System.Drawing.FontFamily 'Arial Black'

# Reserve clear bands for the title while preserving the character's proportions.
$characterState = $graphics.Save()
$characterTransform = New-Object System.Drawing.Drawing2D.Matrix 0.69, 0, 0, 0.69, 39.68, 43
$graphics.Transform = $characterTransform

# The two segmented horns are the strongest silhouette cue from the reference Scout.
$leftHorn = New-Object System.Drawing.Drawing2D.GraphicsPath
$leftHorn.AddBezier(63, 77, 49, 71, 43, 59, 49, 48)
$leftHorn.AddBezier(49, 48, 39, 39, 43, 27, 54, 23)
$leftHorn.AddBezier(54, 23, 58, 12, 72, 12, 79, 21)
$leftHorn.AddBezier(79, 21, 91, 21, 97, 32, 92, 42)
$leftHorn.AddBezier(92, 42, 99, 53, 94, 64, 84, 67)
$leftHorn.AddBezier(84, 67, 81, 76, 72, 81, 63, 77)
$leftHorn.CloseFigure()
$graphics.FillPath($horn, $leftHorn)
$graphics.DrawPath($outline, $leftHorn)

$rightHorn = New-Object System.Drawing.Drawing2D.GraphicsPath
$rightHorn.AddBezier(193, 77, 207, 71, 213, 59, 207, 48)
$rightHorn.AddBezier(207, 48, 217, 39, 213, 27, 202, 23)
$rightHorn.AddBezier(202, 23, 198, 12, 184, 12, 177, 21)
$rightHorn.AddBezier(177, 21, 165, 21, 159, 32, 164, 42)
$rightHorn.AddBezier(164, 42, 157, 53, 162, 64, 172, 67)
$rightHorn.AddBezier(172, 67, 175, 76, 184, 81, 193, 77)
$rightHorn.CloseFigure()
$graphics.FillPath($horn, $rightHorn)
$graphics.DrawPath($outline, $rightHorn)

$graphics.DrawBezier($detail, 46, 35, 59, 31, 77, 38, 93, 34)
$graphics.DrawBezier($detail, 46, 51, 59, 47, 79, 54, 94, 49)
$graphics.DrawBezier($detail, 52, 66, 64, 61, 76, 68, 87, 65)
$graphics.DrawBezier($detail, 210, 35, 197, 31, 179, 38, 163, 34)
$graphics.DrawBezier($detail, 210, 51, 197, 47, 177, 54, 162, 49)
$graphics.DrawBezier($detail, 204, 66, 192, 61, 180, 68, 169, 65)

$graphics.FillEllipse($orange, 38, 46, 180, 177)
$graphics.DrawEllipse($outline, 38, 46, 180, 177)

# Three eyes and the offset pupils mirror the user's orange Scout head.
$graphics.FillEllipse($cream, 105, 62, 46, 40)
$graphics.DrawEllipse($outline, 105, 62, 46, 40)
$graphics.FillEllipse($dark, 122, 74, 13, 17)

$leftEye = New-Object System.Drawing.Drawing2D.GraphicsPath
$leftEye.AddBezier(62, 109, 77, 94, 103, 95, 120, 110)
$leftEye.AddBezier(120, 110, 114, 135, 82, 142, 65, 125)
$leftEye.AddBezier(65, 125, 61, 120, 60, 114, 62, 109)
$leftEye.CloseFigure()
$graphics.FillPath($dark, $leftEye)
$graphics.FillEllipse($cream, 88, 105, 23, 28)

$rightEye = New-Object System.Drawing.Drawing2D.GraphicsPath
$rightEye.AddBezier(136, 110, 153, 95, 179, 94, 194, 109)
$rightEye.AddBezier(194, 109, 196, 114, 195, 120, 191, 125)
$rightEye.AddBezier(191, 125, 174, 142, 142, 135, 136, 110)
$rightEye.CloseFigure()
$graphics.FillPath($dark, $rightEye)
$graphics.FillEllipse($cream, 145, 105, 23, 28)

$mouth = New-Object System.Drawing.Drawing2D.GraphicsPath
$mouth.AddBezier(79, 153, 101, 165, 151, 169, 177, 152)
$mouth.AddBezier(177, 152, 174, 181, 155, 198, 129, 198)
$mouth.AddBezier(129, 198, 102, 198, 84, 180, 79, 153)
$mouth.CloseFigure()
$graphics.FillPath($dark, $mouth)
$graphics.DrawPath($toothOutline, $mouth)

$tooth1 = New-Object System.Drawing.Drawing2D.GraphicsPath
$tooth1.AddPolygon([System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 96, 162),
    (New-Object System.Drawing.Point 112, 166),
    (New-Object System.Drawing.Point 102, 178)
))
$tooth1.CloseFigure()
$graphics.FillPath($cream, $tooth1)
$graphics.DrawPath($toothOutline, $tooth1)

$tooth2 = New-Object System.Drawing.Drawing2D.GraphicsPath
$tooth2.AddPolygon([System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 119, 167),
    (New-Object System.Drawing.Point 136, 167),
    (New-Object System.Drawing.Point 127, 181)
))
$tooth2.CloseFigure()
$graphics.FillPath($cream, $tooth2)
$graphics.DrawPath($toothOutline, $tooth2)

$tooth3 = New-Object System.Drawing.Drawing2D.GraphicsPath
$tooth3.AddPolygon([System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 143, 166),
    (New-Object System.Drawing.Point 160, 162),
    (New-Object System.Drawing.Point 153, 177)
))
$tooth3.CloseFigure()
$graphics.FillPath($cream, $tooth3)
$graphics.DrawPath($toothOutline, $tooth3)

$tongue = New-Object System.Drawing.Drawing2D.GraphicsPath
$tongue.AddBezier(127, 181, 143, 197, 159, 216, 184, 216)
$tongue.AddBezier(184, 216, 205, 217, 213, 205, 202, 197)
$graphics.DrawPath($tonguePen, $tongue)
$graphics.FillEllipse($pink, 194, 190, 17, 17)
$graphics.Restore($characterState)

$topText = New-Object System.Drawing.Drawing2D.GraphicsPath
$topText.AddString(
    'FROG',
    $fontFamily,
    [int][System.Drawing.FontStyle]::Bold,
    36,
    (New-Object System.Drawing.PointF 0, 0),
    [System.Drawing.StringFormat]::GenericTypographic
)
$topBounds = $topText.GetBounds()
$topTransform = New-Object System.Drawing.Drawing2D.Matrix
$topTransform.Translate(128 - ($topBounds.Left + ($topBounds.Width / 2)), 6 - $topBounds.Top)
$topText.Transform($topTransform)
$graphics.DrawPath($textOuter, $topText)
$graphics.DrawPath($textInner, $topText)
$graphics.FillPath($orange, $topText)

$bottomText = New-Object System.Drawing.Drawing2D.GraphicsPath
$bottomText.AddString(
    'SKILL',
    $fontFamily,
    [int][System.Drawing.FontStyle]::Bold,
    36,
    (New-Object System.Drawing.PointF 0, 0),
    [System.Drawing.StringFormat]::GenericTypographic
)
$bottomBounds = $bottomText.GetBounds()
$bottomTransform = New-Object System.Drawing.Drawing2D.Matrix
$bottomTransform.Translate(128 - ($bottomBounds.Left + ($bottomBounds.Width / 2)), 217 - $bottomBounds.Top)
$bottomText.Transform($bottomTransform)
$graphics.DrawPath($textOuter, $bottomText)
$graphics.DrawPath($textInner, $bottomText)
$graphics.FillPath($orange, $bottomText)

$directory = Split-Path -Parent $OutputPath
if ($directory -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}
$bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$bottomTransform.Dispose()
$bottomText.Dispose()
$topTransform.Dispose()
$topText.Dispose()
$characterTransform.Dispose()
$fontFamily.Dispose()
$textInner.Dispose()
$textOuter.Dispose()
$tongue.Dispose()
$tooth3.Dispose()
$tooth2.Dispose()
$tooth1.Dispose()
$mouth.Dispose()
$rightEye.Dispose()
$leftEye.Dispose()
$rightHorn.Dispose()
$leftHorn.Dispose()
$toothOutline.Dispose()
$tonguePen.Dispose()
$detail.Dispose()
$outline.Dispose()
$pink.Dispose()
$dark.Dispose()
$cream.Dispose()
$horn.Dispose()
$orange.Dispose()
$graphics.Dispose()
$bitmap.Dispose()

Write-Output "Generated $OutputPath (256x256 PNG)."
