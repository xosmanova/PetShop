$testPort = 5164
$baseUrl = "http://localhost:$testPort"

Write-Host "Starting PetShop on port $testPort..."
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --no-build --urls $baseUrl"
$psi.WorkingDirectory = "c:\Users\Lenovo\source\repos\PetShop\PetShop"
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true

$process = [System.Diagnostics.Process]::Start($psi)

try {
    $started = $false
    $timeout = 30
    $elapsed = 0
    while (-not $started -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $elapsed++
        try {
            $resp = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing -TimeoutSec 3 -ErrorAction SilentlyContinue
            if ($resp.StatusCode -eq 200) {
                $started = $true
            }
        } catch { }
    }

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -UseBasicParsing
    $loginHtml = [System.Text.Encoding]::UTF8.GetString($loginPage.RawContentStream.ToArray())
    $m = [regex]::Match($loginHtml, '__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $m.Success) {
        $m = [regex]::Match($loginHtml, 'value="([^"]+)"[^>]*__RequestVerificationToken"')
    }
    $token = $m.Groups[1].Value
    Write-Host "Token retrieved: $($token.Substring(0, [Math]::Min(10, $token.Length)))..."

    $loginBody = "Email=admin%40petshop.com&Password=Admin123!&RememberMe=false&__RequestVerificationToken=$([System.Uri]::EscapeDataString($token))"
    $loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -ContentType "application/x-www-form-urlencoded" -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
    Write-Host "Login response status: $($loginResp.StatusCode)"
    Write-Host "Login response location: $($loginResp.Headers['Location'])"

    # Check cookies in session
    $cookies = $session.Cookies.GetCookies((New-Object System.Uri($baseUrl)))
    Write-Host "Cookies count: $($cookies.Count)"
    foreach ($c in $cookies) {
        Write-Host "Cookie: $($c.Name)"
    }

    # Now request /Admin/Blog
    $adminResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -UseBasicParsing
    Write-Host "Admin/Blog status: $($adminResp.StatusCode)"
    $adminHtml = [System.Text.Encoding]::UTF8.GetString($adminResp.RawContentStream.ToArray())
    if ($adminHtml -match "10-reasons-to-be-helpful-towards-any-animals") {
        Write-Host "SUCCESS: Found seed post in Admin/Blog table!" -ForegroundColor Green
    } else {
        Write-Host "FAILED to find seed post in Admin/Blog" -ForegroundColor Red
    }
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.Kill()
    }
}
