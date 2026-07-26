# Contributing

## Requirements

### Python version

This project requires **Python 3.13.3** (see `runtime.txt`).

### Installing dependencies

Install all required packages using pip:

```bash
pip install -r requirements.txt
```

The dependencies are:

| Package | Version | Purpose |
|---|---|---|
| `discord.py` | 2.5.2 | Discord bot framework |
| `gspread` | 6.2.1 | Google Sheets integration |
| `oauth2client` | 4.1.3 | Google API authentication |
| `python-dotenv` | 1.1.0 | Loading environment variables from `.env` |
| `xlsxwriter` | 3.2.3 | Generating Excel files for signups |
| `a2squery` | 0.0.2 | Querying game server info |
| `PyGithub` | 2.6.1 | GitHub API integration |
| `emoji` | 2.14.1 | Emoji support |

## Environment variables

Create a `.env` file in the project root (it is already in `.gitignore`) and populate it with the following values.

### Discord

| Variable | Description |
|---|---|
| `DISCORD_TOKEN` | Bot token from the [Discord Developer Portal](https://discord.com/developers/applications) |
| `CREATOR_ID` | Discord user ID of the bot creator/owner |

### GitHub

| Variable | Description |
|---|---|
| `GITHUB_TOKEN` | Personal access token for the GitHub API (used by PyGithub) |

### Game server

| Variable | Description |
|---|---|
| `SERVER_IP` | IP address of the game server to query |
| `SERVER_PORT` | Port of the game server to query |

### Scarlet Pigs API

| Variable | Description |
|---|---|
| `SCARLETPIGS_API` | Base URL of the Scarlet Pigs REST API (e.g. `http://localhost:5000`) |

### Google Sheets (service account)

These values come from a Google Cloud service account JSON key file. Create a service account in the [Google Cloud Console](https://console.cloud.google.com/), grant it access to the relevant spreadsheet, and copy the fields from the downloaded JSON key into your `.env`:

| Variable | Description |
|---|---|
| `GOOGLE_SHEET_NAME` | Name of the Google Spreadsheet to use |
| `TYPE` | Service account type (always `service_account`) |
| `PROJECT_ID` | GCP project ID |
| `PRIVATE_KEY_ID` | ID of the private key |
| `PRIVATE_KEY` | Private key string (newlines stored as `\n` in the `.env` file) |
| `CLIENT_EMAIL` | Service account email address |
| `CLIENT_ID` | Service account client ID |
| `AUTH_URI` | OAuth2 auth URI (usually `https://accounts.google.com/o/oauth2/auth`) |
| `TOKEN_URI` | OAuth2 token URI (usually `https://oauth2.googleapis.com/token`) |
| `AUTH_PROVIDER_X509_CERT_URL` | Auth provider cert URL |
| `CLIENT_X509_CERT_URL` | Client cert URL |

### Example `.env` file

```dotenv
# Discord
DISCORD_TOKEN=your-discord-bot-token
CREATOR_ID=123456789012345678

# GitHub
GITHUB_TOKEN=ghp_yourtokenhere

# Game server
SERVER_IP=127.0.0.1
SERVER_PORT=2302

# Scarlet Pigs API
SCARLETPIGS_API=http://localhost:5000

# Google Sheets service account
GOOGLE_SHEET_NAME=My Spreadsheet
TYPE=service_account
PROJECT_ID=my-gcp-project
PRIVATE_KEY_ID=abc123
PRIVATE_KEY=-----BEGIN RSA PRIVATE KEY-----\nMIIE...\n-----END RSA PRIVATE KEY-----\n
CLIENT_EMAIL=my-bot@my-gcp-project.iam.gserviceaccount.com
CLIENT_ID=123456789
AUTH_URI=https://accounts.google.com/o/oauth2/auth
TOKEN_URI=https://oauth2.googleapis.com/token
AUTH_PROVIDER_X509_CERT_URL=https://www.googleapis.com/oauth2/v1/certs
CLIENT_X509_CERT_URL=https://www.googleapis.com/robot/v1/metadata/x509/my-bot%40my-gcp-project.iam.gserviceaccount.com
```

## Running the bot

```bash
python main.py
```
