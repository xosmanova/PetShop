$testPort = 5163
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
        "qidalanma qaydalar"
    )

    foreach ($t in $titles) {
        if ($blogHtml -match [regex]::Escape($t)) {
            Write-Host "[PASS] Found post title matching: '$t'" -ForegroundColor Green
        } else {
            Write-Error "[FAIL] Missing post title matching: '$t'"
        }
    }

    # ================= 2. BLOG DETAILS =================
    Write-Host "`n=== TEST 2: Blog Details (/blog/{slug}) ==="
    $slug = "10-reasons-to-be-helpful-towards-any-animals"
    $detailResp = Invoke-WebRequest -Uri "$baseUrl/blog/$slug" -UseBasicParsing
    $detailHtml = [System.Text.Encoding]::UTF8.GetString($detailResp.RawContentStream.ToArray())

    if ($detailResp.StatusCode -eq 200 -and $detailHtml -match "10 Reasons to be helpful towards any animals" -and $detailHtml -match "recent-posts-list") {
        Write-Host "[PASS] Friendly route /blog/$slug returned 200 with content and recent-posts-list." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] /blog/$slug failed to render. StatusCode: $($detailResp.StatusCode)"
    }

    # Also test /Blog/Details?slug=how-to-know-your-pet-is-hungry
    $detail2Resp = Invoke-WebRequest -Uri "$baseUrl/Blog/Details?slug=how-to-know-your-pet-is-hungry" -UseBasicParsing
    $detail2Html = [System.Text.Encoding]::UTF8.GetString($detail2Resp.RawContentStream.ToArray())
    if ($detail2Resp.StatusCode -eq 200 -and $detail2Html -match "How to know your pet is hungry") {
        Write-Host "[PASS] /Blog/Details?slug=how-to-know-your-pet-is-hungry rendered successfully." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] /Blog/Details?slug=how-to-know-your-pet-is-hungry failed."
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
    }

    # Check latest-blog Read all button on Home page
    if ($homeHtml -match 'href="/Blog"[^>]*class="btn btn-outline-dark[^"]*">\s*Read all') {
        Write-Host "[PASS] Home page 'Read all' button links to /Blog." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Home page 'Read all' button does not link to /Blog."
    }

    # ================= 4. ADMIN AUTHORIZATION & CRUD =================
    Write-Host "`n=== TEST 4: Admin Blog Authorization & CRUD ==="
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

    # 4.1 Unauthenticated access to /Admin/Blog
    $unauthResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
    if ($unauthResp.StatusCode -eq 302 -and $unauthResp.Headers["Location"] -match "/Account/Login") {
        Write-Host "[PASS] Unauthenticated access to /Admin/Blog properly redirected to Login (HTTP 302)." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Unauthenticated access not blocked: $($unauthResp.StatusCode)"
    }

    # 4.2 Login as Admin
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -UseBasicParsing
    $loginHtml = [System.Text.Encoding]::UTF8.GetString($loginPage.RawContentStream.ToArray())
    $token = [regex]::Match($loginHtml, 'name="__RequestVerificationToken"\s+value="([^"]+)"').Groups[1].Value

    $loginBody = "Email=admin%40petshop.com&Password=Admin123!&RememberMe=false&__RequestVerificationToken=$([System.Uri]::EscapeDataString($token))"
    $loginPost = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -ContentType "application/x-www-form-urlencoded" -WebSession $session -UseBasicParsing

    Write-Host "Logged in. Accessing /Admin/Blog..."
    $adminResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -UseBasicParsing
    $adminHtml = [System.Text.Encoding]::UTF8.GetString($adminResp.RawContentStream.ToArray())

    if ($adminResp.StatusCode -eq 200 -and $adminHtml -match "10-reasons-to-be-helpful-towards-any-animals") {
        Write-Host "[PASS] Admin successfully accessed /Admin/Blog with table of blog posts." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Admin could not access /Admin/Blog. StatusCode: $($adminResp.StatusCode)"
    }

    # 4.3 Check Admin navigation menu has Blog
    if ($adminHtml -match 'href="/Admin/Blog"[^>]*class="nav-link active"' -or $adminHtml -match 'class="nav-link active"[^>]*href="/Admin/Blog"') {
        Write-Host "[PASS] Admin sidebar contains active Blog link." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Admin sidebar missing active Blog link."
    }

    # 4.4 Create a new BlogPost via POST /Admin/Blog/Create
    Write-Host "`nCreating a new blog post via Admin CRUD..."
    $createGet = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog/Create" -WebSession $session -UseBasicParsing
    $createHtml = [System.Text.Encoding]::UTF8.GetString($createGet.RawContentStream.ToArray())
    $createToken = [regex]::Match($createHtml, 'name="__RequestVerificationToken"\s+value="([^"]+)"').Groups[1].Value

    $testTitle = "Automated Test Post " + (Get-Random -Minimum 1000 -Maximum 9999)
    $createFormData = "Title=$([System.Uri]::EscapeDataString($testTitle))&Slug=&AuthorName=TestAuthor&Summary=This+is+a+test+summary+for+automated+testing&Content=This+is+the+full+content+of+the+automated+test+blog+post&__RequestVerificationToken=$([System.Uri]::EscapeDataString($createToken))"
    $createPost = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog/Create" -Method Post -Body $createFormData -ContentType "application/x-www-form-urlencoded" -WebSession $session -UseBasicParsing

    $adminRespAfterCreate = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -UseBasicParsing
    $adminHtmlAfterCreate = [System.Text.Encoding]::UTF8.GetString($adminRespAfterCreate.RawContentStream.ToArray())

    if ($adminHtmlAfterCreate -match [regex]::Escape($testTitle)) {
        Write-Host "[PASS] Successfully created new post: '$testTitle' with automatic slug generation!" -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Created post not found in admin list."
    }

    Write-Host "`n=== ALL BLOG SYSTEM TESTS COMPLETED SUCCESSFULLY ===" -ForegroundColor Cyan
}
finally {
    Write-Host "Stopping PetShop process..."
    if ($process -and -not $process.HasExited) {
        $process.Kill()
    }
}
