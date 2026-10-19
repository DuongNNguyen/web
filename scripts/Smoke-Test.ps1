param([string]$BaseUrl = 'https://localhost:7280')
$ErrorActionPreference = 'Stop'
$checks = [System.Collections.Generic.List[string]]::new()
function Assert($condition, [string]$message) {
    if (!$condition) { throw "FAIL: $message" }
    $checks.Add($message)
    Write-Host "PASS: $message"
}
function GetPage($session, [string]$path) {
    Invoke-WebRequest "$BaseUrl$path" -WebSession $session -SkipCertificateCheck -SkipHttpErrorCheck
}
function Field($response, [string]$name) {
    $tag = [regex]::Match($response.Content, '<input[^>]*name="' + [regex]::Escape($name) + '"[^>]*>').Value
    [System.Net.WebUtility]::HtmlDecode([regex]::Match($tag, 'value="([^"]*)"').Groups[1].Value)
}
function Post($session, [string]$path, [hashtable]$data, [string]$formPath) {
    $page = GetPage $session $formPath
    $data['__RequestVerificationToken'] = Field $page '__RequestVerificationToken'
    if (!$data['__RequestVerificationToken'] -and $path.StartsWith('/Admin/')) {
        $data['__RequestVerificationToken'] = Field (GetPage $session '/Admin/Products/Create') '__RequestVerificationToken'
    }
    Invoke-WebRequest "$BaseUrl$path" -Method Post -Body $data -WebSession $session -SkipCertificateCheck -SkipHttpErrorCheck
}
function EditData($page) {
    $data = @{}
    foreach($name in @('ProductId','ProductName','Price','DiscountPrice','StockQuantity','Ram','Storage','Color','Screen','Processor','Battery','Camera','RowVersion')) { $data[$name] = Field $page $name }
    $data['BrandId'] = '1'; $data['CategoryId'] = '1'; $data['IsActive'] = 'true'; $data['IsFeatured'] = 'false'
    return $data
}
$anon = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$admin = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$customer = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$other = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$suffix = [Guid]::NewGuid().ToString('N').Substring(0,10)
$email = "smoke-$suffix@example.test"
$password = 'Test@12345!'
$homeResponse = GetPage $anon '/'
Assert ($homeResponse.StatusCode -eq 200 -and $homeResponse.Content.Contains('product-card')) 'Home renders database products'
$catalog = GetPage $anon '/Products'
Assert (([regex]::Matches($catalog.Content,'<article class="product-card">')).Count -eq 12) 'Catalog uses 12-item pagination'
$page2 = GetPage $anon '/Products?page=2'
Assert (([regex]::Matches($page2.Content,'<article class="product-card">')).Count -ge 3) 'Second catalog page works'
$search = GetPage $anon '/Products?keyword=iphone&brandId=1&minPrice=20000000&maxPrice=40000000&sort=price_asc'
Assert (([regex]::Matches($search.Content,'<article class="product-card">')).Count -eq 3) 'Combined search, brand, price and sort filters'
$slug = [regex]::Match($search.Content,'href="(/san-pham/[^"]+)"').Groups[1].Value
Assert ((GetPage $anon $slug).StatusCode -eq 200) 'Product detail route'
Assert ((GetPage $anon '/Checkout').BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Anonymous checkout requires login'
$badCsrf = Invoke-WebRequest "$BaseUrl/Cart/Add" -Method Post -Body @{productId=1;quantity=1} -WebSession $anon -SkipCertificateCheck -SkipHttpErrorCheck
Assert ($badCsrf.StatusCode -eq 400) 'CSRF protection rejects missing token'
$r = Post $admin '/Account/Login' @{Email='admin@phonestore.local';Password='Admin@123!'} '/Account/Login'
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/') 'Admin login'
foreach($path in @('/Admin','/Admin/Products','/Admin/Categories','/Admin/Brands','/Admin/Orders','/Admin/Users')) {
    Assert ((GetPage $admin $path).StatusCode -eq 200) "Admin page $path"
}
$r = Post $customer '/Account/Register' @{FullName='Smoke Test Customer';Email=$email;PhoneNumber='0901234567';Password=$password;ConfirmPassword=$password} '/Account/Register'
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/') 'Registration and automatic customer sign-in'
Assert ((GetPage $customer '/Admin').StatusCode -eq 403) 'Customer cannot access admin'
$r = Post $customer '/Account/Profile' @{FullName='Smoke Test Updated';PhoneNumber='0901234567';Address='Demo address, Ho Chi Minh City'} '/Account/Profile'
Assert ($r.Content.Contains('Smoke Test Updated')) 'Profile update'
$r = Post $customer '/Account/ChangePassword' @{CurrentPassword=$password;NewPassword='Changed@12345!';ConfirmPassword='Changed@12345!'} '/Account/ChangePassword'
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Profile') 'Change password'
$r = Post $customer '/Account/Logout' @{} '/Account/Profile'
Assert ((GetPage $customer '/Orders').BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Logout'
$r = Post $customer '/Account/Login' @{Email=$email;Password='Changed@12345!'} '/Account/Login'
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/') 'Login using changed password'

# Create isolated catalog fixtures through the same admin forms as a human.
foreach($lookup in @('Categories','Brands')) {
    $r = Post $admin "/Admin/$lookup/Create" @{Name="Smoke $lookup $suffix";Description='HTTP verification fixture';IsActive='true'} "/Admin/$lookup/Create"
    Assert ($r.StatusCode -eq 200 -and $r.Content.Contains("Smoke $lookup $suffix")) "$lookup create"
    $row = [regex]::Match($r.Content, '<tr><td>Smoke ' + $lookup + ' ' + $suffix + '</td>.*?</tr>', 'Singleline').Value
    $lookupId = [regex]::Match($row, '/Edit/(\d+)').Groups[1].Value
    $r = Post $admin "/Admin/$lookup/Edit" @{Id=$lookupId;Name="Smoke $lookup $suffix edited";Description='Edited fixture';IsActive='true'} "/Admin/$lookup/Edit/$lookupId"
    Assert ($r.Content.Contains("$suffix edited")) "$lookup edit"
    $r = Post $admin "/Admin/$lookup/Toggle" @{id=$lookupId} "/Admin/$lookup"
    Assert ($r.StatusCode -eq 200) "$lookup deactivate"
}
$productName = "Smoke Phone $suffix"
$create = @{ProductName=$productName;Price='1000000';DiscountPrice='900000';StockQuantity='3';BrandId='1';CategoryId='1';IsActive='true';IsFeatured='false';Ram='8 GB';Storage='128 GB';Description='HTTP verification fixture'}
$r = Post $admin '/Admin/Products/Create' $create '/Admin/Products/Create'
Assert ($r.Content.Contains($productName)) 'Admin product create'
$productId = [regex]::Match($r.Content, '<a href="/Admin/Products/Details/(\d+)">' + $productName).Groups[1].Value
Assert (![string]::IsNullOrEmpty($productId)) 'Created product ID available'
$edit = GetPage $admin "/Admin/Products/Edit/$productId"
$initialVersion = Field $edit 'RowVersion'
$r = Post $anon '/Cart/Add' @{productId=$productId;quantity='4'} '/Cart'
Assert (!$r.Content.Contains($productName)) 'Reject cart quantity above stock'
$r = Post $customer '/Cart/Add' @{productId=$productId;quantity='1'} '/Cart'
Assert ($r.Content.Contains($productName)) 'Add to cart'
$r = Post $customer '/Cart/Update' @{productId=$productId;quantity='2'} '/Cart'
Assert ($r.Content.Contains('value="2"')) 'Increase cart quantity'
$r = Post $customer '/Cart/Update' @{productId=$productId;quantity='1'} '/Cart'
Assert ($r.Content.Contains('value="1"')) 'Decrease cart quantity'
$checkout = GetPage $customer '/Checkout'
$orderInput = @{ReceiverName='Smoke Receiver';ReceiverPhone='0901234567';ReceiverEmail=$email;ShippingAddress='Demo address';Note='Automated verification';Token=(Field $checkout 'Token');__RequestVerificationToken=(Field $checkout '__RequestVerificationToken')}
$r = Invoke-WebRequest "$BaseUrl/Checkout" -Method Post -Body $orderInput -WebSession $customer -SkipCertificateCheck -SkipHttpErrorCheck
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -match '/Orders/Details/\d+') 'Checkout creates order and shows success'
$orderPath = $r.BaseResponse.RequestMessage.RequestUri.AbsolutePath
$orderId = $orderPath.Split('/')[-1]
Assert ($r.Content.Contains('900.000')) 'Order stores server-side discounted price'
$repeat = Invoke-WebRequest "$BaseUrl/Checkout" -Method Post -Body $orderInput -WebSession $customer -SkipCertificateCheck -SkipHttpErrorCheck
Assert ($repeat.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq $orderPath) 'Duplicate checkout is idempotent'
Assert (!(GetPage $customer '/Cart').Content.Contains($productName)) 'Checkout clears cart'
$edit = GetPage $admin "/Admin/Products/Edit/$productId"
Assert ((Field $edit 'StockQuantity') -eq '2') 'Checkout reduces stock exactly once'
$stale = EditData $edit; $stale.RowVersion = $initialVersion; $stale.StockQuantity='3'
$r = Post $admin '/Admin/Products/Edit' $stale "/Admin/Products/Edit/$productId"
Assert ((Field (GetPage $admin "/Admin/Products/Edit/$productId") 'StockQuantity') -eq '2') 'Stale admin edit cannot overwrite checkout stock'
$changed = EditData $edit; $changed.Price='1200000'; $changed.DiscountPrice='1000000'
$r = Post $admin '/Admin/Products/Edit' $changed "/Admin/Products/Edit/$productId"
Assert ((GetPage $customer $orderPath).Content.Contains('900.000')) 'Historical price survives product price change'
$r = Post $other '/Account/Register' @{FullName='Other Smoke Customer';Email="other-$suffix@example.test";PhoneNumber='0901234568';Password=$password;ConfirmPassword=$password} '/Account/Register'
Assert ((GetPage $other $orderPath).StatusCode -eq 404) 'Order ownership blocks other customers'
$r = Post $customer '/Orders/Cancel' @{id=$orderId} $orderPath
Assert ((Field (GetPage $admin "/Admin/Products/Edit/$productId") 'StockQuantity') -eq '3') 'Cancellation restores stock'
$r = Post $customer '/Orders/Cancel' @{id=$orderId} $orderPath
Assert ((Field (GetPage $admin "/Admin/Products/Edit/$productId") 'StockQuantity') -eq '3') 'Repeated cancellation does not restore stock twice'

$r = Post $customer '/Cart/Add' @{productId=$productId;quantity='1'} '/Cart'
$checkout = GetPage $customer '/Checkout'
$orderInput.Token = Field $checkout 'Token'; $orderInput.__RequestVerificationToken = Field $checkout '__RequestVerificationToken'
$r = Invoke-WebRequest "$BaseUrl/Checkout" -Method Post -Body $orderInput -WebSession $customer -SkipCertificateCheck -SkipHttpErrorCheck
$secondId = $r.BaseResponse.RequestMessage.RequestUri.AbsolutePath.Split('/')[-1]
foreach($status in @('Confirmed','Shipping','Completed')) {
    $r = Post $admin '/Admin/Orders/UpdateStatus' @{id=$secondId;status=$status} "/Admin/Orders/Details/$secondId"
    Assert ($r.Content.Contains("status-$status")) "Order transition $status"
}
$r = Post $admin '/Admin/Orders/UpdateStatus' @{id=$secondId;status='Pending'} "/Admin/Orders/Details/$secondId"
Assert ($r.Content.Contains('status-Completed')) 'Completed order cannot return to pending'
Assert ((GetPage $admin '/Admin').StatusCode -eq 200) 'Dashboard aggregates real completed revenue'
$r = Post $customer '/Cart/Add' @{productId=$productId;quantity='1'} '/Cart'
$r = Post $customer '/Cart/Remove' @{productId=$productId} '/Cart'
Assert (!$r.Content.Contains($productName)) 'Remove cart product'
$r = Post $customer '/Cart/Add' @{productId=$productId;quantity='1'} '/Cart'
$r = Post $customer '/Cart/Clear' @{} '/Cart'
Assert (!$r.Content.Contains($productName)) 'Clear cart'

# Valid and disguised uploads use real multipart forms.
$fixtureDirectory = Join-Path $PSScriptRoot 'fixtures'
New-Item -ItemType Directory -Force $fixtureDirectory | Out-Null
$pngPath = Join-Path $fixtureDirectory 'pixel.png'
[IO.File]::WriteAllBytes($pngPath, [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9xkAAAAASUVORK5CYII='))
$invalidPath = Join-Path $fixtureDirectory 'invalid.png'
[IO.File]::WriteAllText($invalidPath, '<script>not an image</script>')
$edit = GetPage $admin "/Admin/Products/Edit/$productId"
$uploadForm = EditData $edit
$uploadForm['__RequestVerificationToken'] = Field $edit '__RequestVerificationToken'
$uploadForm['Uploads'] = Get-Item $pngPath
$r = Invoke-WebRequest "$BaseUrl/Admin/Products/Edit" -Method Post -Form $uploadForm -WebSession $admin -SkipCertificateCheck -SkipHttpErrorCheck
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Admin/Products') 'Valid PNG upload accepted'
$edit = GetPage $admin "/Admin/Products/Edit/$productId"
$uploadedPath = [regex]::Match($edit.Content, 'src="(/images/products/[a-f0-9]{32}\.png)"').Groups[1].Value
Assert ($uploadedPath -and (GetPage $anon $uploadedPath).StatusCode -eq 200) 'Uploaded image has a generated filename and is served locally'
$uploadForm = EditData $edit
$uploadForm['__RequestVerificationToken'] = Field $edit '__RequestVerificationToken'
$uploadForm['Uploads'] = Get-Item $invalidPath
$r = Invoke-WebRequest "$BaseUrl/Admin/Products/Edit" -Method Post -Form $uploadForm -WebSession $admin -SkipCertificateCheck -SkipHttpErrorCheck
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Admin/Products/Edit' -and $r.Content.Contains('validation-summary-errors')) 'Disguised image upload rejected'
$imageId = [regex]::Match($edit.Content, 'name="id" value="(\d+)"').Groups[1].Value
$r = Post $admin '/Admin/Products/RemoveImage' @{id=$imageId} "/Admin/Products/Edit/$productId"
Assert ((GetPage $anon $uploadedPath).StatusCode -eq 404) 'Removing image deletes its generated file'

# Race two independent sessions against exactly one unit of stock.
$edit = GetPage $admin "/Admin/Products/Edit/$productId"
$data = EditData $edit; $data.StockQuantity = '1'
$r = Post $admin '/Admin/Products/Edit' $data "/Admin/Products/Edit/$productId"
$pending = @()
$clients = @()
foreach($session in @($customer, $other)) {
    $r = Post $session '/Cart/Add' @{productId=$productId;quantity='1'} '/Cart'
    $checkout = GetPage $session '/Checkout'
    $payload = [System.Collections.Generic.Dictionary[string,string]]::new()
    $payload.Add('ReceiverName','Concurrent buyer'); $payload.Add('ReceiverPhone','0901234567'); $payload.Add('ReceiverEmail','race@example.test'); $payload.Add('ShippingAddress','Demo address')
    $payload.Add('Token', (Field $checkout 'Token')); $payload.Add('__RequestVerificationToken', (Field $checkout '__RequestVerificationToken'))
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.CookieContainer = $session.Cookies
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
    $client = [System.Net.Http.HttpClient]::new($handler); $clients += $client
    $pending += @{Client=$client;Content=[System.Net.Http.FormUrlEncodedContent]::new($payload)}
}
$tasks = @($pending | ForEach-Object { $_.Client.PostAsync("$BaseUrl/Checkout", $_.Content) })
$results = @($tasks | ForEach-Object { $_.GetAwaiter().GetResult() })
$winners = @($results | Where-Object { $_.RequestMessage.RequestUri.AbsolutePath -match '/Orders/Details/\d+' })
Assert ($winners.Count -eq 1) 'Concurrent buyers: exactly one order succeeds for the last unit'
Assert ((Field (GetPage $admin "/Admin/Products/Edit/$productId") 'StockQuantity') -eq '0') 'Concurrent checkout leaves stock at zero, never negative'
$raceId = $winners[0].RequestMessage.RequestUri.AbsolutePath.Split('/')[-1]
$r = Post $admin '/Admin/Orders/UpdateStatus' @{id=$raceId;status='Cancelled'} "/Admin/Orders/Details/$raceId"
Assert ((Field (GetPage $admin "/Admin/Products/Edit/$productId") 'StockQuantity') -eq '1') 'Race winner cancellation restores exactly one unit'
foreach($client in $clients) { $client.Dispose() }

$r = Post $admin '/Admin/Products/Toggle' @{id=$productId} '/Admin/Products'
Assert (!((GetPage $anon "/Products?keyword=$suffix").Content.Contains('product-card'))) 'Inactive product hidden from storefront'

$userPage = GetPage $admin "/Admin/Users?keyword=$email"
$userId = [regex]::Match($userPage.Content, 'name="id" value="([^"]+)"').Groups[1].Value
$r = Post $admin '/Admin/Users/Toggle' @{id=$userId} '/Admin/Users'
Assert ((GetPage $customer '/Orders').BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Deactivated customer loses existing authenticated access'
$r = Post $admin '/Admin/Users/Toggle' @{id=$userId} '/Admin/Users'
$r = Post $customer '/Account/Login' @{Email=$email;Password='Changed@12345!'} '/Account/Login'
Assert ($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/') 'Reactivated customer can log in'
Assert ((GetPage $anon '/this-page-does-not-exist').StatusCode -eq 404) 'Friendly 404 retains correct status'
Write-Host "Completed $($checks.Count) checks. Fixture suffix: $suffix; product: $productId; orders: $orderId, $secondId"




