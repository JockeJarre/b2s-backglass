# B2S Registry Test Script
# Run this while B2SBackglassServerEXE.exe is running

Write-Host "B2S Registry Test Script" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Create registry key if needed
$regPath = "HKCU:\Software\B2S"
if (!(Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
    Write-Host "Created registry key: $regPath" -ForegroundColor Green
}

Write-Host "Testing lamp changes..." -ForegroundColor Yellow
Write-Host ""

# Test 1: All lamps OFF
Write-Host "1. Setting all lamps OFF..." -ForegroundColor White
Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value ("0" * 401)
Start-Sleep -Seconds 2

# Test 2: Turn on first 10 lamps
Write-Host "2. Turning ON lamps 0-9..." -ForegroundColor White
$lamps = "1" * 10 + "0" * 391
Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value $lamps
Start-Sleep -Seconds 2

# Test 3: Turn on first 20 lamps
Write-Host "3. Turning ON lamps 0-19..." -ForegroundColor White
$lamps = "1" * 20 + "0" * 381
Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value $lamps
Start-Sleep -Seconds 2

# Test 4: Blink test
Write-Host "4. Blinking lamps 0-19 (10 times)..." -ForegroundColor White
for ($i = 0; $i -lt 10; $i++) {
    if ($i % 2 -eq 0) {
        $lamps = "1" * 20 + "0" * 381
    } else {
        $lamps = "0" * 401
    }
    Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value $lamps
    Start-Sleep -Milliseconds 300
}

# Test 5: Wave pattern
Write-Host "5. Wave pattern..." -ForegroundColor White
for ($wave = 0; $wave -lt 20; $wave++) {
    $lamps = "0" * 401
    $start = $wave * 5
    $end = [Math]::Min($start + 10, 401)
    
    if ($start -lt 401) {
        $lamps = "0" * $start + "1" * ($end - $start) + "0" * (401 - $end)
    }
    
    Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value $lamps
    Start-Sleep -Milliseconds 200
}

# Test 6: All ON
Write-Host "6. All lamps ON..." -ForegroundColor White
Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value ("1" * 401)
Start-Sleep -Seconds 2

# Test 7: All OFF
Write-Host "7. All lamps OFF..." -ForegroundColor White
Set-ItemProperty -Path $regPath -Name "B2SLamps" -Value ("0" * 401)

Write-Host ""
Write-Host "Test complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Did the backglass lamps change?" -ForegroundColor Yellow
Write-Host "Check the Debug Output in Visual Studio for registry messages" -ForegroundColor Yellow
