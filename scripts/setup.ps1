$ErrorActionPreference = 'Stop'
if (Test-Path .env) { throw '.env already exists. Preserve your existing credentials.' }
function New-Secret { return 'Sc9!' + [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24)) }
@("SQL_PASSWORD=$(New-Secret)", "CUSTOMER_PASSWORD=$(New-Secret)", "ADMIN_PASSWORD=$(New-Secret)") | Set-Content .env
Write-Host 'Created .env. Read it locally for customer/admin passwords; never commit it.'
