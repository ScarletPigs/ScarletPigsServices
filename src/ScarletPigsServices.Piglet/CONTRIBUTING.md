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
| `requests` | 2.32+ | Scarlet Pigs API client |
| `tzdata` | 2025.2+ | Schedule timezone data |
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
| `SCARLETPIGS_API_KEY` | Shared API key sent in the `X-API-Key` header |

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
SCARLETPIGS_API_KEY=your-shared-api-key
```

## Running the bot

```bash
python main.py
```
