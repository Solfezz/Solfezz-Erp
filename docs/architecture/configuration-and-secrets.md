# Configuration and Secrets

## Principles

Solfezz ERP separates configuration from secrets.

Safe configuration may be committed to source control.

Secrets must never be committed.

## Configuration Sources

ASP.NET Core configuration may be supplied by:

1. appsettings.json
2. appsettings.{Environment}.json
3. User Secrets during local development
4. environment variables
5. deployment-specific secret/configuration providers

Later configuration sources override earlier sources.

## Local Development

Safe development settings may live in:

appsettings.Development.json

Secrets such as database passwords must use:

- ASP.NET Core User Secrets
- local environment variables
- other approved secret stores

## Database Connection

The application expects:

ConnectionStrings:ErpDatabase

The committed appsettings.json contains only an empty placeholder.

The real local development value is stored using User Secrets.

## Docker Secrets

The local SQL Server container receives its SA password through:

.env

The real .env file is ignored by Git.

.env.example documents required variables without containing real credentials.

## Rules

Never commit:

- passwords
- API keys
- access tokens
- private certificates
- production connection strings
- marketplace credentials

Do not log secrets.

Do not include secrets in exception messages.

Do not paste secrets into documentation or pull requests.

## Environment Variables

ASP.NET Core hierarchical configuration uses double underscores.

Example:

ConnectionStrings__ErpDatabase

maps to:

ConnectionStrings:ErpDatabase
