# Piglet

Piglet stores its schedule, Discord message registrations, and DLC
questionnaire state in the Scarlet Pigs API. `scarletpigsapi.py` contains the
typed Python client for the API's `snake_case` contract. The API is Piglet's
sole persistent datastore.

The DLC questionnaire options are read from the
`piglet.dlc_questionnaire` app-setting. Configure that setting through the API
before using `/createquestionnaire`; the bot reports a configuration message
instead of creating an empty questionnaire when no options exist.


The invite link for the bot: https://discord.com/api/oauth2/authorize?client_id=1012077296515039324&permissions=8&scope=bot%20applications.commands

BOT INSTRUCTIONS MESSAGE:

With the new bot mission makers can now add their ops to the schedule themselves. They can also edit the ops currently on the schedule.

The command to add an op to the schedule is /reserversunday and takes two arguments. One for the mission name and one for the author name. (Please remember to use tab to autocomplete the arguments after the command. Should show up as small boxes you can write in.)

The command to edit an op on the schedule is /editsunday and takes no arguments. It should give you some prompts automatically when you run it.

The command to easily and quickly add roles written about in your announcement as reactions is to right click your message, hover over apps, and then press "Add signups".

The command to get reactions and signups in a nice and easily digestible way is like above but instead pressing "Get signups". Then it'll generate an excel sheet for you after some seconds. This will soon also include the training tags of the people that signed up next to their names.

The DLC questionnaire reaction counts are stored in the Scarlet Pigs API.
