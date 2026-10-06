param([string]$Image = 'hotdesk-dependency-check:20261006')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 10)
$network = "hotdesk-check-$suffix"
$database = "$network-db"
$api = "$network-api"
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(10)
$baseUrl = ''

function Invoke-Docker {
    $Arguments = $args
    $result = & docker.exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($Arguments[0])" }
    return $result
}

function Request {
    param([string]$Method, [string]$Path, [int]$Expected, $Body = $null, [string]$Token = '')
    $message = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($Method), "$baseUrl$Path")
    if ($null -ne $Body) {
        $message.Content = New-Object System.Net.Http.StringContent(($Body | ConvertTo-Json -Depth 10), [Text.Encoding]::UTF8, 'application/json')
    }
    if ($Token) { $message.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $Token) }
    $response = $client.SendAsync($message).GetAwaiter().GetResult()
    try {
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ([int]$response.StatusCode -ne $Expected) { throw "$Method $Path expected $Expected, got $([int]$response.StatusCode)" }
        Write-Host "$Method $Path -> $Expected"
        if ($content -and $response.Content.Headers.ContentType.MediaType -match 'json') { return ($content | ConvertFrom-Json) }
        return $content
    }
    finally { $response.Dispose(); $message.Dispose() }
}

function Check { param([bool]$Condition, [string]$Description) if (!$Condition) { throw $Description } }

try {
    Invoke-Docker network create $network | Out-Null
    $env:POSTGRES_PASSWORD = [Guid]::NewGuid().ToString('N')
    Invoke-Docker run -d --name $database --network $network --network-alias db --tmpfs /var/lib/postgresql -e POSTGRES_PASSWORD -e POSTGRES_DB=checks postgres:18 | Out-Null
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker.exe exec $database pg_isready -U postgres -d checks *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    Check $ready 'PostgreSQL did not become ready.'
    $env:ConnectionStrings__Database = "Host=db;Database=checks;Username=postgres;Password=$env:POSTGRES_PASSWORD"
    $env:Jwt__Key = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
    Invoke-Docker run -d --name $api --network $network -p 127.0.0.1::8080 -e ASPNETCORE_ENVIRONMENT=Development -e ConnectionStrings__Database -e Jwt__Key -e Jwt__Expires=1 $Image | Out-Null
    $binding = Invoke-Docker port $api 8080/tcp
    $baseUrl = "http://$binding"
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $response = $client.GetAsync("$baseUrl/swagger/v1/swagger.json").GetAwaiter().GetResult()
            $ready = $response.IsSuccessStatusCode
            $response.Dispose()
        } catch { }
        if ($ready) { break }
        Start-Sleep -Seconds 1
    }
    Check $ready 'API did not become ready.'

    $swagger = Request GET /swagger/v1/swagger.json 200
    $ui = Request GET /swagger/index.html 200
    Check ($ui -match 'swagger-ui') 'Swagger UI missing.'
    Check ($swagger.components.securitySchemes.Bearer.type -eq 'http') 'Bearer scheme missing.'
    Check ($swagger.security[0].PSObject.Properties.Name -contains 'Bearer') 'Bearer security reference missing.'
    Check ($null -ne $swagger.paths.'/api/reservations'.post) 'Reservation route missing.'
    Check ($swagger.components.schemas.BookDeskCommand.properties.startDate.format -eq 'date') 'Date schema changed.'

    $admin = Request POST /api/login 200 @{ email = 'a@c.pl'; password = 'Test123!' }
    $registration = @{ email = 'employee@example.test'; firstName = 'Test'; lastName = 'Employee'; password = 'SmokePassword123!' }
    Request POST /api/register 200 $registration | Out-Null
    $employee = Request POST /api/login 200 @{ email = $registration.email; password = $registration.password }
    Request POST /api/login 400 @{ email = $registration.email; password = 'wrong' } | Out-Null
    $invalid = Request POST /api/register 422 @{ email = 'invalid'; firstName = ''; lastName = ''; password = 'short' }
    Check ($invalid.status -eq 422 -and $invalid.errors.Count -gt 0) 'Validation contract changed.'

    $locationBody = @{ name = 'Smoke office'; address = @{ street = 'Main'; buildingNumber = '1'; city = 'Warsaw'; postalCode = '00-001' } }
    Request POST /api/locations 401 $locationBody | Out-Null
    Request POST /api/locations 403 $locationBody $employee | Out-Null
    Request POST /api/locations 401 $locationBody 'invalid-token' | Out-Null
    $location = Request POST /api/locations 201 $locationBody $admin
    Check ($location.address.city -eq 'Warsaw' -and $location.name -eq 'Smoke office') 'Location mapping changed.'
    $desksPath = "/api/locations/$($location.id)/desks"
    $desk = Request POST $desksPath 201 @{ name = 'Desk'; description = 'Smoke desk' } $admin
    $list = Request GET $desksPath 200 $null $employee
    Check ($list.items.Count -eq 1 -and $list.items[0].id -eq $desk.id) 'Desk list mapping changed.'
    $date = [DateTime]::UtcNow.AddDays(1).ToString('yyyy-MM-dd')
    $booking = Request POST /api/reservations 201 @{ deskId = $desk.id; startDate = $date; endDate = $date } $employee
    Check ($booking.startDate -eq $date -and $booking.endDate -eq $date -and $booking.status -eq 'Reserved') 'Reservation serialization changed.'
    $details = Request GET "$desksPath/$($desk.id)" 200 $null $employee
    Check ($details.reservation.id -eq $booking.id) 'Booked reservation missing from desk details.'
    Check ($null -eq $details.reservation.user) 'Employee response exposed reservation identity.'
    $adminDetails = Request GET "$desksPath/$($desk.id)" 200 $null $admin
    Check ($adminDetails.reservation.user.name -eq 'Test Employee') 'Admin reservation identity missing.'
    Write-Host 'All dependency smoke checks passed.'
}
finally {
    $client.Dispose()
    & docker.exe rm -f $api $database 2>$null | Out-Null
    & docker.exe network rm $network 2>$null | Out-Null
    Remove-Item Env:POSTGRES_PASSWORD, Env:ConnectionStrings__Database, Env:Jwt__Key -ErrorAction SilentlyContinue
}
