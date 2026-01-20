# View the latest log file
$logPath = "$env:LOCALAPPDATA\MermaidDiagramApp\Logs"

if (Test-Path $logPath) {
    $latestLog = Get-ChildItem $logPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($latestLog) {
        Write-Host "Latest log file: $($latestLog.FullName)" -ForegroundColor Green
        Write-Host "Last 100 lines:" -ForegroundColor Yellow
        Write-Host "=" * 80
        Get-Content $latestLog.FullName -Tail 100
    } else {
        Write-Host "No log files found" -ForegroundColor Red
    }
} else {
    Write-Host "Log directory not found: $logPath" -ForegroundColor Red
}
