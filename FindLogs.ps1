# Script to find and monitor MermaidDiagramApp logs

Write-Host "Looking for MermaidDiagramApp log files..." -ForegroundColor Green

# Check common locations for UWP/WinUI app logs
$possibleLocations = @(
    "$env:LOCALAPPDATA\Packages\*Mermaid*\LocalState\Logs",
    "$env:TEMP\MermaidDiagramApp\Logs",
    "$env:APPDATA\MermaidDiagramApp\Logs",
    ".\Logs",
    "$env:USERPROFILE\AppData\Local\MermaidDiagramApp\Logs"
)

$foundLogs = @()

foreach ($location in $possibleLocations) {
    if (Test-Path $location) {
        $logFiles = Get-ChildItem $location -Filter "*.log" -ErrorAction SilentlyContinue
        if ($logFiles) {
            $foundLogs += $logFiles
            Write-Host "Found logs in: $location" -ForegroundColor Yellow
            foreach ($file in $logFiles) {
                Write-Host "  - $($file.Name) (Size: $($file.Length) bytes, Modified: $($file.LastWriteTime))" -ForegroundColor Cyan
            }
        }
    }
}

# Also check for any recent log files in temp directories
Write-Host "`nChecking for recent log files in temp directories..." -ForegroundColor Green
$recentLogs = Get-ChildItem $env:TEMP -Filter "*mermaid*.log" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-2) }
if ($recentLogs) {
    Write-Host "Found recent logs in temp:" -ForegroundColor Yellow
    foreach ($file in $recentLogs) {
        Write-Host "  - $($file.FullName) (Modified: $($file.LastWriteTime))" -ForegroundColor Cyan
        $foundLogs += $file
    }
}

if ($foundLogs.Count -eq 0) {
    Write-Host "No log files found. The application may not have been run yet or logs may be in a different location." -ForegroundColor Red
    Write-Host "Try running the application first, then run this script again." -ForegroundColor Yellow
} else {
    Write-Host "`nFound $($foundLogs.Count) log file(s). Opening the most recent one..." -ForegroundColor Green
    $mostRecent = $foundLogs | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "Opening: $($mostRecent.FullName)" -ForegroundColor Cyan
    
    # Open the log file in notepad
    Start-Process notepad.exe -ArgumentList $mostRecent.FullName
    
    # Also display the last 50 lines
    Write-Host "`nLast 50 lines of the log:" -ForegroundColor Green
    Write-Host "=" * 80 -ForegroundColor Gray
    Get-Content $mostRecent.FullName -Tail 50
    Write-Host "=" * 80 -ForegroundColor Gray
}

Write-Host "`nTo monitor logs in real-time, run:" -ForegroundColor Yellow
if ($foundLogs.Count -gt 0) {
    $mostRecent = $foundLogs | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "Get-Content '$($mostRecent.FullName)' -Wait -Tail 10" -ForegroundColor Cyan
}