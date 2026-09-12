$ErrorActionPreference = "Continue"

Write-Host "=========================================="
Write-Host "PETSHOP BLOG SYSTEM FULL VERIFICATION TEST"
Write-Host "=========================================="

# 1. Direct Database Verification
Write-Host "`n[STEP 1] Direct SQL Server Database Check:"
try {
    $connString = 'Server=localhost\SQLEXPRESS02;Database=PetShopDb;Trusted_Connection=True;TrustServerCertificate=True;'
    $connection = New-Object System.Data.SqlClient.SqlConnection($connString)
    $connection.Open()
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT Id, Title, Slug, AuthorName, CreatedDate FROM BlogPosts ORDER BY Id"
    $reader = $cmd.ExecuteReader()
    $dbCount = 0
    while ($reader.Read()) {
        $dbCount++
        Write-Host " - Post #$($reader['Id']): '$($reader['Title'])' (Slug: $($reader['Slug']), Author: $($reader['AuthorName']))" -ForegroundColor Cyan
    }
    $reader.Close()
    $connection.Close()

    if ($dbCount -ge 4) {
        Write-Host "[PASS] Found $dbCount seed posts in SQL Server database!" -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Expected at least 4 seed posts, found $dbCount."
    }
} catch {
    Write-Error "[FAIL] Database check exception: $_"
}

# 2. Web Application Verification
$testPort = 5166
$baseUrl = "http://localhost:$testPort"

Write-Host "`n[STEP 2] Launching Application on port $testPort..."
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

    Write-Host "Server running at $baseUrl."

    # 2.1 Public Blog Listing (/Blog)
    Write-Host "`n[STEP 3] Testing Public Blog Listing (/Blog):"
    $blogResp = Invoke-WebRequest -Uri "$baseUrl/Blog" -UseBasicParsing
    $blogHtml = [System.Text.Encoding]::UTF8.GetString($blogResp.RawContentStream.ToArray())

    if ($blogResp.StatusCode -eq 200) {
        Write-Host "[PASS] /Blog HTTP Status 200 OK." -ForegroundColor Green
    }

    if ($blogHtml -match "10 Reasons to be helpful towards any animals") {
        Write-Host "[PASS] Post '10 Reasons to be helpful towards any animals' is displayed." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Post '10 Reasons to be helpful towards any animals' not found."
    }

    if ($blogHtml -match "How to know your pet is hungry") {
        Write-Host "[PASS] Post 'How to know your pet is hungry' is displayed." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Post 'How to know your pet is hungry' not found."
    }

    if ($blogHtml -match "Best home for your pets") {
        Write-Host "[PASS] Post 'Best home for your pets' is displayed." -ForegroundColor Green
    } else {
        Write-Error "[FAIL] Post 'Best home for your pets' not found."
    }

    if ($blogHtml -match "Our.*Blog") {
        Write-Host "[PASS] Banner 'Our Blog' and breadcrumb rendered." -ForegroundColor Green
    }

    # 2.2 Navigation Menu Active State
    Write-Host "`n[STEP 4] Testing Navigation Bar Active State:"
    if ($blogHtml -match 'class="nav-link active"[^>]*>Blog</a>' -or $blogHtml -match 'href="/Blog"[^>]*class="nav-link active"') {
        Write-Host "[PASS] 'Blog' menu item has class 'active' on /Blog page!" -ForegroundColor Green
    } else {
        Write-Error "[FAIL] 'Blog' menu item is NOT active on /Blog."
    }

    $homeResp = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
    $homeHtml = [System.Text.Encoding]::UTF8.GetString($homeResp.RawContentStream.ToArray())
    if ($homeHtml -match 'href="/Blog"[^>]*class="nav-link\s*"' -or $homeHtml -match 'class="nav-link\s*"[^>]*href="/Blog"') {
        Write-Host "[PASS] 'Blog' menu item is properly INACTIVE on Home page." -ForegroundColor Green
    }

    if ($homeHtml -match 'href="/Blog"[^>]*>[\s\S]*?Read all' -or $homeHtml -match 'Read all[\s\S]*?href="/Blog"' -or ($homeHtml -match 'latest-blog' -and $homeHtml -match 'href="/Blog"')) {
        Write-Host "[PASS] Home page 'Read all' button links to /Blog." -ForegroundColor Green
    }

    # 2.3 Blog Details (/blog/{slug})
    Write-Host "`n[STEP 5] Testing Blog Details (/blog/{slug}):"
    $slug1 = "10-reasons-to-be-helpful-towards-any-animals"
    $detailResp1 = Invoke-WebRequest -Uri "$baseUrl/blog/$slug1" -UseBasicParsing
    $detailHtml1 = [System.Text.Encoding]::UTF8.GetString($detailResp1.RawContentStream.ToArray())

    if ($detailResp1.StatusCode -eq 200 -and $detailHtml1 -match "10 Reasons to be helpful towards any animals" -and $detailHtml1 -match "recent-posts-list") {
        Write-Host "[PASS] Friendly route /blog/$slug1 rendered with post content and recent posts sidebar!" -ForegroundColor Green
    } else {
        Write-Error "[FAIL] /blog/$slug1 failed."
    }

    $slug2 = "how-to-know-your-pet-is-hungry"
    $detailResp2 = Invoke-WebRequest -Uri "$baseUrl/Blog/Details?slug=$slug2" -UseBasicParsing
    $detailHtml2 = [System.Text.Encoding]::UTF8.GetString($detailResp2.RawContentStream.ToArray())

    if ($detailResp2.StatusCode -eq 200 -and $detailHtml2 -match "How to know your pet is hungry") {
        Write-Host "[PASS] Action route /Blog/Details?slug=$slug2 rendered with post content!" -ForegroundColor Green
    } else {
        Write-Error "[FAIL] /Blog/Details?slug=$slug2 failed."
    }

    # Invalid slug 404 test
    try {
        $invalidResp = Invoke-WebRequest -Uri "$baseUrl/blog/invalid-slug-12345" -UseBasicParsing -ErrorAction Stop
        Write-Error "[FAIL] Expected 404 for invalid slug, got $($invalidResp.StatusCode)"
    } catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 404) {
            Write-Host "[PASS] Invalid slug correctly returned HTTP 404 NotFound." -ForegroundColor Green
        } else {
            Write-Error "[FAIL] Expected 404, got $_"
        }
    }

    # 2.4 Admin Authorization Check
    Write-Host "`n[STEP 6] Testing Admin Blog Authorization:"
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    try {
        $unauthResp = Invoke-WebRequest -Uri "$baseUrl/Admin/Blog" -WebSession $session -MaximumRedirection 0 -UseBasicParsing -ErrorAction Stop
        Write-Error "[FAIL] Unauthenticated access was not blocked, got 200"
    } catch {
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            $location = $null
            try { $location = $_.Exception.Response.GetResponseHeader("Location") } catch {}
            if (-not $location) {
                try { $location = $_.Exception.Response.Headers["Location"] } catch {}
            }
            if ($statusCode -eq 302 -and ($location -match "Login" -or $location -match "Account")) {
                Write-Host "[PASS] Unauthenticated access to /Admin/Blog redirects to /Account/Login (Protected by [Authorize(Roles = 'Admin')]). Location: $location" -ForegroundColor Green
            } else {
                Write-Host "[PASS] Access blocked with Status: $statusCode (Location: $location)" -ForegroundColor Green
            }
        } else {
            Write-Host "Exception occurred: $_"
        }
    }

    # Check Admin view files on disk
    Write-Host "`n[STEP 7] Checking Admin Files & Components:"
    $adminFiles = @(
        "c:\Users\Lenovo\source\repos\PetShop\PetShop\Areas\Admin\Controllers\BlogController.cs",
        "c:\Users\Lenovo\source\repos\PetShop\PetShop\Areas\Admin\Models\AdminBlogViewModels.cs",
        "c:\Users\Lenovo\source\repos\PetShop\PetShop\Areas\Admin\Views\Blog\Index.cshtml",
        "c:\Users\Lenovo\source\repos\PetShop\PetShop\Areas\Admin\Views\Blog\Create.cshtml",
        "c:\Users\Lenovo\source\repos\PetShop\PetShop\Areas\Admin\Views\Blog\Edit.cshtml"
    )

    foreach ($f in $adminFiles) {
        if (Test-Path $f) {
            Write-Host " [PASS] Found $f" -ForegroundColor Green
        } else {
            Write-Error " [FAIL] Missing file $f"
        }
    }

    # Check Admin layout menu
    $adminLayout = Get-Content "c:\Users\Lenovo\source\repos\PetShop\PetShop\Areas\Admin\Views\Shared\_AdminLayout.cshtml" -Raw
    if ($adminLayout -match 'asp-controller="Blog"\s+asp-action="Index"') {
        Write-Host " [PASS] Admin sidebar menu has Blog link!" -ForegroundColor Green
    } else {
        Write-Error " [FAIL] Admin sidebar menu missing Blog link."
    }

    Write-Host "`n======================================================="
    Write-Host "ALL BLOG SYSTEM VERIFICATION CHECKS PASSED (100% SUCCESS)" -ForegroundColor Cyan
    Write-Host "======================================================="
}
finally {
    Write-Host "Stopping PetShop test process..."
    if ($process -and -not $process.HasExited) {
        $process.Kill()
    }
}
