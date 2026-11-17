param(
    [string]$BaseUrl = 'http://localhost:5080',
    [PSCredential]$Credential,
    [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession
)
$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')
$endpoint = "$($BaseUrl.TrimEnd('/'))/api/customers"
$createdId = $null

if (([uri]$base).Scheme -ne 'https' -and ([uri]$base).Host -notin 'localhost', '127.0.0.1') {
    throw 'Use HTTPS for an online API.'
}
if (-not $Credential -and -not $WebSession) {
    throw 'Pass -Credential (Get-Credential) for an Administrator/Salesperson account, or an authenticated -WebSession.'
}
if (-not $WebSession) { $WebSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new() }

function Invoke-TestRequest {
    param([string]$Uri, [string]$Method = 'Get', [string]$Body)
    $parameters = @{ Uri=$Uri; Method=$Method; WebSession=$WebSession; SkipHttpErrorCheck=$true; MaximumRedirection=0 }
    if ($Body) { $parameters.ContentType='application/json'; $parameters.Body=$Body }
    if ($Method -notin 'Get', 'Head', 'Options') {
        $csrf = Invoke-RestMethod "$base/api/auth/csrf" -WebSession $WebSession
        $parameters.Headers = @{ 'X-CSRF-TOKEN'=$csrf.token }
    }
    Invoke-WebRequest @parameters
}

function Assert-Status($Response, [int]$Expected) {
    if ([int]$Response.StatusCode -ne $Expected) {
        throw "Expected HTTP $Expected, received $($Response.StatusCode): $($Response.Content)"
    }
}

if ($Credential) {
    $loginBody = @{ userName=$Credential.UserName; password=$Credential.GetNetworkCredential().Password } | ConvertTo-Json
    $login = Invoke-TestRequest "$base/api/auth/login" -Method Post -Body $loginBody
    $loginBody = $null
    Assert-Status $login 200
}
$account = Invoke-RestMethod "$base/api/auth/me" -WebSession $WebSession
if ($account.role -notin 'Administrator', 'Salesperson') { throw 'Customer write tests require Administrator or Salesperson.' }

try {
    $invalid = Invoke-TestRequest $endpoint -Method Post -Body '{"name":"   "}'
    Assert-Status $invalid 400
    $invalidEmail = Invoke-TestRequest $endpoint -Method Post -Body '{"name":"Test","email":"invalid"}'
    Assert-Status $invalidEmail 400

    $name = "ERP smoke test $([guid]::NewGuid().ToString('N'))"
    $body = @{ name = "  $name  "; email = '  test@example.com  '; phone = '  ' } | ConvertTo-Json
    $created = Invoke-TestRequest $endpoint -Method Post -Body $body
    Assert-Status $created 201
    $customer = $created.Content | ConvertFrom-Json
    $createdId = $customer.id
    if ($customer.name -ne $name -or $customer.email -ne 'test@example.com' -or $null -ne $customer.phone) { throw 'Normalization failed.' }
    if (-not $created.Headers.Location) { throw 'Creation must return a Location header.' }
    $location = [uri]::new([uri]$endpoint, [string]$created.Headers.Location[0])
    if ($location.Scheme -ne ([uri]$endpoint).Scheme -or $location.Authority -ne ([uri]$endpoint).Authority) {
        throw 'Creation Location must retain the API origin and HTTPS scheme.'
    }

    Assert-Status (Invoke-TestRequest "$endpoint/$createdId") 200
    $invalidUpdate = Invoke-TestRequest "$endpoint/$createdId" -Method Put -Body '{"name":" "}'
    Assert-Status $invalidUpdate 400
    $unchanged = (Invoke-TestRequest "$endpoint/$createdId").Content | ConvertFrom-Json
    if ($unchanged.name -ne $name) { throw 'Invalid update changed stored data.' }

    $updated = (Invoke-TestRequest "$endpoint/$createdId" -Method Put -Body (@{ name = "$name updated"; email = $null; phone = '123' } | ConvertTo-Json)).Content | ConvertFrom-Json
    if ($updated.name -ne "$name updated" -or $updated.phone -ne '123') { throw 'Update failed.' }
    $list = (Invoke-TestRequest $endpoint).Content | ConvertFrom-Json
    if (-not ($list | Where-Object id -EQ $createdId)) { throw 'Created customer missing from list.' }

    Assert-Status (Invoke-TestRequest "$endpoint/$createdId" -Method Delete) 204
    Assert-Status (Invoke-TestRequest "$endpoint/$createdId") 404
    Assert-Status (Invoke-TestRequest "$endpoint/$createdId" -Method Delete) 404
    $createdId = $null
    Write-Output 'PASS: validation, normalization, create, read, update, list, delete and missing-record responses.'
} finally {
    if ($null -ne $createdId) {
        Invoke-TestRequest "$endpoint/$createdId" -Method Delete | Out-Null
    }
}
