param(
    [Parameter(Mandatory=$true)][string]$LibraryUrl,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "published")
)
$ErrorActionPreference = "Stop"
$uri = $null
if (-not [Uri]::TryCreate($LibraryUrl, [UriKind]::Absolute, [ref]$uri) -or
    ($uri.Scheme -ne "https" -and -not ($uri.Scheme -eq "http" -and $uri.IsLoopback))) {
    throw "LibraryUrl must use HTTPS, except local HTTP for testing."
}
$destination = [IO.Path]::GetFullPath($OutputDirectory)
$published = Join-Path $destination "client"
$archive = Join-Path $destination "LibraryDesktop-Windows-x64.zip"
New-Item -ItemType Directory -Force -Path $destination | Out-Null
if (Test-Path $published) { Remove-Item -Recurse -Force $published }
dotnet publish (Join-Path $PSScriptRoot "LibraryDesktopClient.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $published
if ($LASTEXITCODE -ne 0) { throw "Windows client publish failed." }
@{ LibraryUrl = $uri.AbsoluteUri } | ConvertTo-Json | Set-Content -Encoding utf8 -Path (Join-Path $published "library-client.json")
if (Test-Path $archive) { Remove-Item -Force $archive }
Compress-Archive -Path (Join-Path $published '*') -DestinationPath $archive -CompressionLevel Optimal
Write-Host "Staff download package: $archive"
Write-Host "Upload it to App_Data/Downloads on the hosted library, not to wwwroot."
