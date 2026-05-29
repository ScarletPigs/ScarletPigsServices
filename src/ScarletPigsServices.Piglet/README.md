Scarlet Piglet Discord bot.

This service is hosted from the ScarletPigsServices repo at `src/ScarletPigsServices.Piglet` and is started by Aspire through the AppHost. The bot manages Sunday scheduling, signup reaction helpers, modlist posts, and the DLC questionnaire. It uses Google Sheets as the current backing store.

Invite link:
https://discord.com/api/oauth2/authorize?client_id=1012077296515039324&permissions=8&scope=bot%20applications.commands

Schedule sheet:
https://docs.google.com/spreadsheets/d/1BAWa3vwD20Q_92kWG_jZlMIkMXgQlF5piot4TYnmscE/edit?usp=sharing

## Running

For normal local development in this repo, start the AppHost and launch the `piglet` resource so Aspire can provide the required environment variables.

Required Piglet configuration passed through Aspire:

```text
DISCORD_TOKEN
CREATOR_ID
GITHUB_TOKEN
SCARLETPIGS_API
GOOGLE_SHEET_NAME
TYPE
PROJECT_ID
PRIVATE_KEY_ID
PRIVATE_KEY
CLIENT_EMAIL
CLIENT_ID
AUTH_URI
TOKEN_URI
AUTH_PROVIDER_X509_CERT_URL
CLIENT_X509_CERT_URL
```

Standalone runs from the service folder are still supported if the same variables are present in the environment.

Start the bot:

```powershell
.\.venv\Scripts\python main.py
```

Debug stored schedule messages:

```powershell
.\.venv\Scripts\python main.py print-schedule-messages
```

## Structure

```text
main.py                     # Real entrypoint
bot/
	bootstrap.py             # Bot wiring and registration
	config.py                # Environment and shared settings
	runtime.py               # Discord client, BOT, TREE, run_bot
	jobs.py                  # Recurring tasks and debug schedule fetch
	commands/
		schedule_commands.py   # Slash commands for schedule/admin actions
		message_commands.py    # Context-menu utilities
	ui/
		selects.py             # Discord select components
		modals.py              # Discord modal components
	services/
		schedule_service.py    # Schedule/questionnaire domain + sheet operations
		sheets_store.py        # Google Sheets storage layer
		sheets_config.py       # Google Sheets config values
		formatting.py          # Message formatting helpers
		reactions.py           # Reaction parsing/count helpers
		api_service.py         # Scarlet Pigs API client
```

## Bot Usage

Mission makers can reserve and edit Sunday ops with `/reservesunday`, `/editsunday`, and `/deletesunday`.

The bot can add signup reactions from an announcement via the `Add signups` context menu and export reaction signups via the `Get signups` context menu.

For DLC poll results, use the published Google chart:
https://docs.google.com/spreadsheets/d/e/2PACX-1vQYrmXaRK5P-FatQKhgiy6SEmyTX2sqSBvBxKg5Oz-hTYZMgeh8fFqgRD__mdSn5gC-3LqVC3u02WFJ/pubchart?oid=653336303&format=interactive
