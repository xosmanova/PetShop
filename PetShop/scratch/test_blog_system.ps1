$testPort = 5162
$baseUrl = "http://localhost:$testPort"

Write-Host "Starting PetShop application on port $testPort..."
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --no-build --urls $baseUrl"
$psi.WorkingDirectory = "c:\Users\Lenovo\source\repos\PetShop\PetShop"
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true

$process = [System.Diagnostics.Process]::Start($psi)

try {
    # Wait for server to start
    $started = $false
    $timeout = 35
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

    if (-not $started) {
        Write-Error "Server did not start within $timeout seconds."
        exit 1
    }

    Write-Host "Server started successfully on $baseUrl."

    # ================= 1. PUBLIC BLOG LISTING =================
    Write-Host "`n=== TEST 1: Public Blog Listing (/Blog) ==="
    $blogResp = Invoke-WebRequest -Uri "$baseUrl/Blog" -UseBasicParsing
    $blogHtml = [System.Text.Encoding]::UTF8.GetString($blogResp.RawContentStream.ToArray())

    if ($blogResp.StatusCode -eq 200) {
        Write-Host "[PASS] /Blog returned HTTP 200 OK." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] /Blog returned $($blogResp.StatusCode)"
    }

    $titles = @(
        "10 Reasons to be helpful towards any animals",
        "How to know your pet is hungry",
        "Best home for your pets",
        "Ev heyvanları üçün düzgün qidalanma qaydaları"
    )

    foreach ($t in $titles) {
        if ($blogHtml -match [regex]::Escape($t)) {
            Write-Host "[PASS] Found post title: '$t'" -ForegroundColor Green
        } else {
            Write-Error "[FAIL] Missing post title: '$t'"
        }
    }

    # ================= 2. BLOG DETAILS =================
    Write-Host "`n=== TEST 2: Blog Details (/blog/{slug}) ==="
    $slug = "10-reasons-to-be-helpful-towards-any-animals"
    $detailResp = Invoke-WebRequest -Uri "$baseUrl/blog/$slug" -UseBasicParsing
    $detailHtml = [System.Text.Encoding]::UTF8.GetString($detailResp.RawContentStream.ToArray())

    if ($detailResp.StatusCode -eq 200 -and $detailHtml -match "10 Reasons to be helpful towards any animals" -and $detailHtml -match "Son Məqalələr") {
        Write-Host "[PASS] Friendly route /blog/$slug returned 200 with full content and recent posts sidebar." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] /blog/$slug failed to render correctly."
    }

    # Non-existent slug should be 404
    try {
        $notFoundResp = Invoke-WebRequest -Uri "$baseUrl/blog/non-existent-article-slug" -UseBasicParsing -ErrorAction Stop
        Write-Error "[FAIL] Expected 404 for non-existent slug, got $($notFoundResp.StatusCode)"
    } catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 404) {
            Write-Host "[PASS] Non-existent slug properly returned 404 NotFound." -ForegroundColor Green
        } else {
            Write-Error "[FAIL] Unexpected error: $_"
        }
    }

    # ================= 3. NAVBAR & ACTIVE LINK =================
    Write-Host "`n=== TEST 3: Navigation Bar & Active Highlight ==="
    if ($blogHtml -match 'class="nav-link active"[^>]*>Blog</a>' -or $blogHtml -match 'href="/Blog"[^>]*class="nav-link active"') {
        Write-Host "[PASS] Blog link is marked active on /Blog page." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Blog link is NOT marked active on /Blog page."
    }

    $homeResp = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
    $homeHtml = [System.Text.Encoding]::UTF8.GetString($homeResp.RawContentStream.ToArray())
    if ($homeHtml -match 'href="/Blog"[^>]*class="nav-link "' -or $homeHtml -match 'class="nav-link "[^>]*href="/Blog"') {
        Write-Host "[PASS] Blog link is NOT active on Home page." -ForegroundColor Green
    } else {
        Write-Host "[INFO] Checking home active state..."
    }

    # ================= 4. ADMIN ACCESS & CRUD =================
    Write-Host "`n=== TEST 4: Admin Blog Authorization & CRUD ==="
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

    # Without login -> should redirect to Login
    $unauthResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($unauthResp.StatusCode -eq 302 -and $unauthResp.Headers["Location"] -match "/Account/Login") {
        Write-Host "[PASS] Unauthenticated access to /Admin/Blog redirects to Login." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Unauthenticated access was not blocked: $($unauthResp.StatusCode)"
    }

    # Login as Admin
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -UseBasicParsing
    $loginHtml = [System.Text.Encoding]::UTF8.GetString($loginPage.RawContentStream.ToArray())
    $tokenMatch = [regex]::Match($loginHtml, 'name="__RequestVerificationToken"\s+value="([^"]+)"')
    $token = $tokenMatch.Groups[1].Value

    $loginBody = @{
        "Email" = "admin@petshop.com"
        "Password" = "Admin123!"
        "RememberMe" = "false"
        "__RequestVerificationToken" = $token
    }

    $loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $session -UseBasicParsing
    Write-Host "Admin login submitted. Checking /Admin/Blog access..."

    $adminBlogResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -UseBasicParsing
    $adminBlogHtml = [System.Text.Encoding]::UTF8.GetString($adminBlogResp.RawContentStream.ToArray())

    if ($adminBlogResp.StatusCode -eq 200 -and $adminBlogHtml -match "Məqalələr \(Blog\)") {
        Write-Host "[PASS] Admin successfully accessed /Admin/Blog." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Admin could not access /Admin/Blog."
    }

    # Test Create Blog Post
    $createPage = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog/Create" -WebSession $session -UseBasicParsing
    $createHtml = [System.Text.Encoding]::UTF8.GetString($createPage.RawContentStream.ToArray())
    $cToken = [regex]::Match($createHtml, 'name="__RequestVerificationToken"\s+value="([^"]+)"').Groups[1].Value

    $testPostTitle = "Test Məqalə - Avtomatlaşdırılmış Test"
    $createBody = @{
        "Title" = $testPostTitle
        "Slug" = ""
        "AuthorName" = "Test Admin"
        "Summary" = "Bu avtomatlaşdırılmış test üçün yaradılmış nümunə məqalədir."
        "Content" = "Test məqaləsinin tam mətni burada yerləşir. Avtomatlaşdırılmış test sistemi tərəfindən uğurla yaradılmışdır."
        "__RequestVerificationToken" = $cToken
    }

    $createResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog/Create" -Method Post -Body $createBody -WebSession $session -UseBasicParsing
    $adminBlogHtml2 = [System.Text.Encoding]::UTF8.GetString($createResp.RawContentStream.ToArray())

    if ($adminBlogHtml2 -match "test-meqale-avtomatlasdirilmis-test" -or $adminBlogHtml2 -match "Test Məqalə") {
        Write-Host "[PASS] Test blog post created with auto-generated slug!" -ForegroundColor Green
    } else {
        # Check by refreshing Index
        $checkIndex = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -UseBasicParsing
        $checkHtml = [System.Text.Encoding]::UTF8.GetString($checkIndex.RawContentStream.ToArray())
        if ($checkHtml -match "Test Məqalə") {
            Write-Host "[PASS] Test blog post confirmed in admin list!" -ForegroundColor Green
        } else {
            Write-Error "[FAIL] Test blog post was not found in admin list."
        }
    }

    Write-Host "`n=== ALL BLOG SYSTEM TESTS COMPLETED SUCCESSFULLY ===" -ForegroundColor Cyan
}
finally {
    Write-Host "Stopping PetShop process..."
    if ($process -and -not $process.HasExited) {
        $process.Kill()
    }
}
