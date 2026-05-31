# Chạy Tosix Decor (BE + FE)
$be = Start-Process -FilePath "dotnet" -ArgumentList "run","--project","src/Tosix.Api/Tosix.Api.csproj" -WorkingDirectory "D:\Projects1\betosix" -PassThru -NoNewWindow
Start-Sleep -Seconds 3
$fe = Start-Process -FilePath "npm" -ArgumentList "run","dev" -WorkingDirectory "D:\Projects1\fetosix" -PassThru -NoNewWindow
Write-Host "Backend PID: $($be.Id) — http://localhost:5208"
Write-Host "Frontend PID: $($fe.Id) — http://localhost:5174"
Write-Host "Admin: admin@tosix.local / Admin123!"
