#!/usr/bin/env bash
set -euo pipefail
if [[ -f .env ]]; then echo '.env already exists; preserving credentials.'; exit 1; fi
umask 077
for name in SQL_PASSWORD CUSTOMER_PASSWORD ADMIN_PASSWORD; do
 printf '%s=Sc9!%s\n' "$name" "$(openssl rand -hex 24)" >> .env
done
echo 'Created .env. Read it locally for customer/admin passwords; never commit it.'
