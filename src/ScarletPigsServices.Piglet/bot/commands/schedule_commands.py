from __future__ import annotations

from collections.abc import Awaitable, Callable
import asyncio
import os

import discord
from discord import Interaction, app_commands

from bot.services import schedule_service as schedule


def register_schedule_commands(
    tree: app_commands.CommandTree,
    bot: discord.Client,
    date_select_factory: Callable[[str, str], discord.ui.Select],
    op_edit_select_factory: Callable[[bool], discord.ui.Select],
    format_schedule_message: Callable[[], str],
    retrieve_file_from_github: Callable[[str], str | None],
    format_dlc_list: Callable[[list], str],
    check_dlc_message: Callable[[], Awaitable[None]],
):
    @tree.command(name="send", description="Send a message")
    @app_commands.checks.has_role("ServerOps")
    async def send(interaction: Interaction, message: str):
        channel = getattr(interaction, 'channel', None)
        if channel is None or not hasattr(channel, 'send'):
            await interaction.response.send_message(content="Could not resolve channel to send message.", ephemeral=True)
            return
        await interaction.response.send_message(content="Message sent.", ephemeral=True)
        await channel.send(message)

    @tree.command(name="reservesunday", description="Reserve a sunday")
    @app_commands.checks.has_role("Mission Maker")
    async def reservesunday(interaction: discord.Interaction, opname: str, authorname: str):
        await interaction.response.defer(ephemeral=True)
        view = discord.ui.View(timeout=180).add_item(date_select_factory(opname, authorname))
        await interaction.followup.send(content="Reserved an op. Now pick the date: ", view=view)

    @tree.command(name="editsunday", description="Edit a booked op")
    @app_commands.checks.has_role("Mission Maker")
    async def editsunday(interaction: discord.Interaction):
        await interaction.response.defer(ephemeral=True)
        view = discord.ui.View(timeout=180).add_item(op_edit_select_factory(False))
        await interaction.followup.send(content="Which op do you want to edit? ", view=view)

    @tree.command(name="deletesunday", description="Delete a booked op")
    @app_commands.checks.has_role("Mission Maker")
    async def deletesunday(interaction: discord.Interaction):
        await interaction.response.defer(ephemeral=True)
        view = discord.ui.View(timeout=180).add_item(op_edit_select_factory(True))
        await interaction.followup.send(content="Which op do you want to delete? ", view=view)

    @tree.command(name="createschedule", description="Create an op schedule in this channel")
    @app_commands.checks.has_role("Unit Organizer")
    async def createschedule(interaction: discord.Interaction):
        await interaction.response.defer(ephemeral=True)
        guild_id = interaction.guild_id
        channel = getattr(interaction, 'channel', None)
        if channel is None or not hasattr(channel, 'send'):
            await interaction.followup.send(content="Could not resolve channel to send schedule.", ephemeral=True)
            return
        schedule_messages = schedule.get_schedule_messages()
        guild_ids = [server['guild_id'] for server in schedule_messages['servers']]

        if guild_id in guild_ids:
            index = guild_ids.index(guild_id)
            old_channel = bot.get_channel(schedule_messages['servers'][index]['channel_id'])
            if old_channel is not None and isinstance(old_channel, discord.TextChannel):
                try:
                    old_msg = await old_channel.fetch_message(schedule_messages['servers'][index]['message_id'])
                    await old_msg.delete()
                except Exception:
                    print("Couldn't delete old message")

        new_msg = await channel.send(content=format_schedule_message())
        if guild_id is None or not hasattr(channel, 'id') or not hasattr(new_msg, 'id'):
            await interaction.followup.send(content="Could not resolve guild/channel/message id.", ephemeral=True)
            return
        schedule.set_schedule_message_id(guild_id, channel.id, new_msg.id)
        await interaction.followup.send(content="Op schedule created.")

    @tree.command(name="createmodlist", description="Create a modlist message in this channel")
    @app_commands.checks.has_role("Unit Organizer")
    async def createmodlist(interaction: discord.Interaction, repofilepath: str):
        await interaction.response.defer(ephemeral=True)
        channel = getattr(interaction, 'channel', None)
        guild_id = interaction.guild_id
        if channel is None or not hasattr(channel, 'send'):
            await interaction.followup.send(content="Could not resolve channel to send modlist.", ephemeral=True)
            return
        file_name = retrieve_file_from_github(repofilepath)
        if file_name is None:
            await interaction.followup.send(content="Couldn't find the file. Make sure the file exists and the path is correct. (An example path format would be Modlists/ScarletBannerKAT.html)", ephemeral=True)
            return
        msg = await channel.send(content=f"The modlist file: {file_name}", files=[discord.File(f"files/{file_name}")])
        os.remove(f"files/{file_name}")
        if guild_id is None or not hasattr(channel, 'id') or not hasattr(msg, 'id'):
            await interaction.followup.send(content="Could not resolve guild/channel/message id.", ephemeral=True)
            return
        schedule.add_modlist_message(guild_id, channel.id, msg.id, repofilepath)
        await interaction.followup.send(content="Modlist message created.", ephemeral=True)

    @tree.command(name="createquestionnaire", description="Create DLC questionnaire in channel. (WARNING: Will delete any previous questionnaire messages)")
    @app_commands.checks.has_role("Unit Organizer")
    async def createquestionnaire(interaction: discord.Interaction):
        await interaction.response.defer(ephemeral=True)
        guild_id = interaction.guild_id
        channel = getattr(interaction, 'channel', None)
        if channel is None or not hasattr(channel, 'send'):
            await interaction.followup.send(content="Could not resolve channel to send questionnaire.", ephemeral=True)
            return
        questionnaire_message = schedule.get_questionnaire_message()
        if questionnaire_message is not None:
            if ('guild_id' not in questionnaire_message or questionnaire_message['guild_id'] not in [guild.id for guild in bot.guilds]):
                await interaction.followup.send(content="I do not have access to the previous questionnaire message.", ephemeral=True)
                return
            old_channel = bot.get_channel(questionnaire_message['channel_id'])
            if old_channel is not None and hasattr(old_channel, 'fetch_message') and isinstance(old_channel, discord.TextChannel):
                try:
                    old_msg = await old_channel.fetch_message(questionnaire_message['message_id'])
                    await old_msg.delete()
                except Exception:
                    print("Couldn't delete old message")
        dlcs = schedule.get_questionnaire_info()
        msg_content = f"**The Scarlet Pigs DLC Questionnaire**\n\nPlease react to this message with the DLCs you have to allow the mission makers to better keep track of which DLCs they can make use of.\n\n*DLCs:*\n{format_dlc_list(dlcs)}\n\n\nResults: https://docs.google.com/spreadsheets/d/e/2PACX-1vQYrmXaRK5P-FatQKhgiy6SEmyTX2sqSBvBxKg5Oz-hTYZMgeh8fFqgRD__mdSn5gC-3LqVC3u02WFJ/pubchart?oid=653336303&format=interactive"
        new_msg = await channel.send(content=msg_content, embeds=[])
        await interaction.followup.send(content="DLC questionnaire created.", ephemeral=True)
        await asyncio.sleep(1)
        for dlc in dlcs:
            emoji = dlc[2]
            try:
                await new_msg.add_reaction(emoji)
            except Exception as error:
                print(f"Couldn't add reaction because {error}")
        if guild_id is None or not hasattr(channel, 'id') or not hasattr(new_msg, 'id'):
            await interaction.followup.send(content="Could not resolve guild/channel/message id.", ephemeral=True)
            return
        schedule.set_questionnaire_message(guild_id, channel.id, new_msg.id)
        await check_dlc_message()