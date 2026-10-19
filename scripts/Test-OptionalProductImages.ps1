param([string]$BaseUrl = 'https://localhost:7280', [int]$ProductId = 15)
$ErrorActionPreference = 'Stop'
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
function Token($page) {
    [Net.WebUtility]::HtmlDecode([regex]::Match($page.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value)
}
$login = Invoke-WebRequest "$BaseUrl/Account/Login" -WebSession $session -SkipCertificateCheck
$null = Invoke-WebRequest "$BaseUrl/Account/Login" -Method Post -Body @{Email='admin@phonestore.local';Password='Admin@123!';__RequestVerificationToken=(Token $login)} -WebSession $session -SkipCertificateCheck
$edit = Invoke-WebRequest "$BaseUrl/Admin/Products/Edit/$ProductId" -WebSession $session -SkipCertificateCheck
$inputTag = [regex]::Match($edit.Content, '<input[^>]*name="Uploads"[^>]*>').Value
if (!$inputTag -or $inputTag.Contains('data-val-required') -or $inputTag -match '\srequired(?:\s|=|>)') { throw 'Upload input must not be required by client validation.' }
Write-Output 'PASS: image input is optional in generated HTML.'
function SelectedId([string]$name) {
    $select = [regex]::Match($edit.Content, '<select[^>]*name="' + $name + '"[^>]*>(.*?)</select>', 'Singleline').Groups[1].Value
    $option = [regex]::Match($select, '<option[^>]*selected[^>]*>').Value
    [regex]::Match($option, 'value="([^"]+)"').Groups[1].Value
}
# An intentionally stale row version reaches the save guard but prevents all
# database mutations. The clean demonstration database remains unchanged.
$form = @{
    ProductId="$ProductId"; ProductName='Optional upload validation'; Price='100000';
    StockQuantity='1'; BrandId=(SelectedId 'BrandId'); CategoryId=(SelectedId 'CategoryId');
    RowVersion='stale-test-version'; __RequestVerificationToken=(Token $edit)
}
$response = Invoke-WebRequest "$BaseUrl/Admin/Products/Edit" -Method Post -Form $form -WebSession $session -SkipCertificateCheck
$html = [Net.WebUtility]::HtmlDecode($response.Content)
if (!$html.Contains('Sản phẩm hoặc tồn kho vừa thay đổi.')) { throw 'Empty upload did not pass server validation to the concurrency guard.' }
if ($html.Contains('Ảnh sản phẩm (tối đa 8 ảnh, 5 MB/ảnh) field is required')) { throw 'Unexpected image-required validation.' }
Write-Output 'PASS: multipart edit without an image passes server validation; stale version prevents database changes.'
$after = Invoke-WebRequest "$BaseUrl/Admin/Products/Edit/$ProductId" -WebSession $session -SkipCertificateCheck
$beforeImages = @([regex]::Matches($edit.Content, 'src="(/images/products/[^"]+)"') | ForEach-Object { $_.Groups[1].Value })
$afterImages = @([regex]::Matches($after.Content, 'src="(/images/products/[^"]+)"') | ForEach-Object { $_.Groups[1].Value })
if (($beforeImages -join '|') -ne ($afterImages -join '|')) { throw 'Existing images changed.' }
Write-Output 'PASS: existing images preserved; no test accounts, products or orders created.'
