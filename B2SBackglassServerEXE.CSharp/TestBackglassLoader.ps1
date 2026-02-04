# Quick test to check what's in a backglass file
param(
    [string]$BackglassFile = "D:\vPinball\VisualPinball\Tables\VPX\1-2-3 (Automaticos 1973).directb2s"
)

if (-not (Test-Path $BackglassFile)) {
    Write-Host "File not found: $BackglassFile" -ForegroundColor Red
    exit 1
}

Write-Host "Loading: $BackglassFile" -ForegroundColor Green

[xml]$xml = Get-Content $BackglassFile

Write-Host "`n=== SCORES ===" -ForegroundColor Cyan
$scores = $xml.SelectNodes("//Scores/Score")
Write-Host "Found $($scores.Count) score displays"

foreach ($score in $scores) {
    Write-Host "`nScore ID=$($score.ID)" -ForegroundColor Yellow
    Write-Host "  Parent: $($score.Parent)"
    Write-Host "  ReelType: $($score.ReelType)"
    Write-Host "  Digits: $($score.Digits)"
    Write-Host "  Location: ($($score.LocX), $($score.LocY))"
    Write-Host "  Size: $($score.Width) x $($score.Height)"
    Write-Host "  DisplayState: $($score.DisplayState)"
}

Write-Host "`n=== REEL IMAGES ===" -ForegroundColor Cyan
$reelImages = $xml.SelectNodes("//Reels/Images/Image")
if ($reelImages.Count -eq 0) {
    $reelImages = $xml.SelectNodes("//Reels/Image")
}
Write-Host "Found $($reelImages.Count) reel images"

if ($reelImages.Count -gt 0) {
    Write-Host "`nFirst 10 reel image names:" -ForegroundColor Yellow
    foreach ($img in $reelImages | Select-Object -First 10) {
        Write-Host "  - $($img.Name)"
    }
}

Write-Host "`n=== ILLUMINATIONS ===" -ForegroundColor Cyan
$illums = $xml.SelectNodes("//Illumination")
Write-Host "Found $($illums.Count) illuminations"

Write-Host "`nDone!" -ForegroundColor Green
