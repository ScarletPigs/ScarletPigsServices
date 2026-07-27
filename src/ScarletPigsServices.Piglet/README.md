# Piglet

Piglet stores its schedule, Discord message registrations, and DLC
questionnaire state in the Scarlet Pigs API. `scarletpigsapi.py` contains the
typed Python client for the API's `snake_case` contract.

## One-time Google Sheets import

At startup Piglet reads the persistent
`piglet.google_sheets_import` app-setting. If it is not marked complete, the bot
uses the configured Google service-account credentials to import:

- schedule, modlist, and questionnaire message registrations;
- questionnaire rows and reaction counts;
- current and archived operations; and
- the configured number of schedule dates.

The completion setting is written only after all imports succeed. Imported
events use stable external IDs, so an interrupted import can safely retry on
the next launch. Once the marker is complete, Piglet does not initialize or
contact Google Sheets during normal operation.


The invite link for the bot: https://discord.com/api/oauth2/authorize?client_id=1012077296515039324&permissions=8&scope=bot%20applications.commands

Legacy schedule sheet (import source): https://docs.google.com/spreadsheets/d/1BAWa3vwD20Q_92kWG_jZlMIkMXgQlF5piot4TYnmscE/edit?usp=sharing



BOT INSTRUCTIONS MESSAGE:

With the new bot mission makers can now add their ops to the schedule themselves. They can also edit the ops currently on the schedule.

The command to add an op to the schedule is /reserversunday and takes two arguments. One for the mission name and one for the author name. (Please remember to use tab to autocomplete the arguments after the command. Should show up as small boxes you can write in.)

The command to edit an op on the schedule is /editsunday and takes no arguments. It should give you some prompts automatically when you run it.

The command to easily and quickly add roles written about in your announcement as reactions is to right click your message, hover over apps, and then press "Add signups".

The command to get reactions and signups in a nice and easily digestible way is like above but instead pressing "Get signups". Then it'll generate an excel sheet for you after some seconds. This will soon also include the training tags of the people that signed up next to their names.

NOTE: For results from the DLC poll, consult the link below. It should dynamically update every hour with the new responses.
https://docs.google.com/spreadsheets/d/e/2PACX-1vQYrmXaRK5P-FatQKhgiy6SEmyTX2sqSBvBxKg5Oz-hTYZMgeh8fFqgRD__mdSn5gC-3LqVC3u02WFJ/pubchart?oid=653336303&format=interactive
