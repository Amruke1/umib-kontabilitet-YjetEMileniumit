param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
$endpoint = "$($BaseUrl.TrimEnd('/'))/api/customers"
$createdId = $null

function Assert-Status($Response, [int]$Expected) {
    if ([int]$Response.StatusCode -ne $Expected) {
        throw "Expected HTTP $Expected, received $($Response.StatusCode): $($Response.Content)"
    }
}

try {
    $invalid = Invoke-WebRequest $endpoint -Method Post -ContentType 'application/json' -Body '{"name":"   "}' -SkipHttpErrorCheck
    Assert-Status $invalid 400
    $invalidEmail = Invoke-WebRequest $endpoint -Method Post -ContentType 'application/json' -Body '{"name":"Test","email":"invalid"}' -SkipHttpErrorCheck
    Assert-Status $invalidEmail 400

    $name = "ERP smoke test $([guid]::NewGuid().ToString('N'))"
    $body = @{ name = "  $name  "; email = '  test@example.com  '; phone = '  ' } | ConvertTo-Json
    $created = Invoke-WebRequest $endpoint -Method Post -ContentType 'application/json' -Body $body -SkipHttpErrorCheck
    Assert-Status $created 201
    $customer = $created.Content | ConvertFrom-Json
    $createdId = $customer.id
    if ($customer.name -ne $name -or $customer.email -ne 'test@example.com' -or $null -ne $customer.phone) { throw 'Normalization failed.' }
    if (-not $created.Headers.Location) { throw 'Creation must return a Location header.' }

    Assert-Status (Invoke-WebRequest "$endpoint/$createdId" -SkipHttpErrorCheck) 200
    $invalidUpdate = Invoke-WebRequest "$endpoint/$createdId" -Method Put -ContentType 'application/json' -Body '{"name":" "}' -SkipHttpErrorCheck
    Assert-Status $invalidUpdate 400
    $unchanged = Invoke-RestMethod "$endpoint/$createdId"
    if ($unchanged.name -ne $name) { throw 'Invalid update changed stored data.' }

    $updated = Invoke-RestMethod "$endpoint/$createdId" -Method Put -ContentType 'application/json' -Body (@{ name = "$name updated"; email = $null; phone = '123' } | ConvertTo-Json)
    if ($updated.name -ne "$name updated" -or $updated.phone -ne '123') { throw 'Update failed.' }
    $list = Invoke-RestMethod $endpoint
    if (-not ($list | Where-Object id -EQ $createdId)) { throw 'Created customer missing from list.' }

    Assert-Status (Invoke-WebRequest "$endpoint/$createdId" -Method Delete -SkipHttpErrorCheck) 204
    Assert-Status (Invoke-WebRequest "$endpoint/$createdId" -SkipHttpErrorCheck) 404
    Assert-Status (Invoke-WebRequest "$endpoint/$createdId" -Method Delete -SkipHttpErrorCheck) 404
    $createdId = $null
    Write-Output 'PASS: validation, normalization, create, read, update, list, delete and missing-record responses.'
} finally {
    if ($null -ne $createdId) {
        Invoke-WebRequest "$endpoint/$createdId" -Method Delete -SkipHttpErrorCheck | Out-Null
    }
}
