from __future__ import annotations

from collections.abc import Awaitable, Callable
import io

import discord
from discord import app_commands
import emoji as emoji_lib
import xlsxwriter


def register_message_commands(
    tree: app_commands.CommandTree,
    get_emojis_in_message: Callable[[str], list],
    get_reactions_from_message: Callable[[discord.Message], Awaitable[list | None]],
    bot_message_edit_modal_factory: Callable[[discord.Message], discord.ui.Modal],
):
    @tree.context_menu(name="Add signups")
    @app_commands.checks.has_role("Mission Maker")
    @app_commands.checks.cooldown(rate=1, per=120)
    async def add_signups(interaction: discord.Interaction, message: discord.Message):
        await interaction.response.defer(ephemeral=True)
        emojis = get_emojis_in_message(message.content)

        if len(emojis) == 0:
            await interaction.followup.send(content="Message has no emojis...")
            return

        for emoji in emojis:
            if type(emoji) == int:
                if isinstance(interaction.guild, discord.Guild):
                    emoji = await interaction.guild.fetch_emoji(emoji)
                    await message.add_reaction(emoji)
            else:
                try:
                    emoji = emoji_lib.emojize(emoji)
                    await message.add_reaction(emoji)
                except Exception:
                    print("Couldn't convert emoji to unicode")

        await interaction.followup.send(content="Reactions added to message.", ephemeral=True)

    @tree.context_menu(name="Get signups")
    @app_commands.checks.has_role("Mission Maker")
    @app_commands.checks.cooldown(rate=1, per=120)
    async def get_signups(interaction: discord.Interaction, message: discord.Message):
        await interaction.response.defer(ephemeral=True)
        all_rows = await get_reactions_from_message(message)

        if all_rows is None:
            await interaction.followup.send(content="Message has no reactions...")
            return

        stream = io.BytesIO()
        workbook = xlsxwriter.Workbook(stream)
        sheet = workbook.add_worksheet()
        workbook.set_custom_property("Encoding", "utf-8-sig")

        for i, row in enumerate(all_rows):
            sheet.write_row(i, 0, row)
        workbook.close()
        stream.seek(0)

        await interaction.followup.send(content="Signups exported to Excel sheet.", files=[discord.File(stream, "signups.xlsx")])
        stream.close()

    @tree.context_menu(name="Copy message")
    @app_commands.checks.has_role("ServerOps")
    async def copy_message(interaction: discord.Interaction, message: discord.Message):
        await interaction.response.defer(ephemeral=True)
        message_content = message.content
        message_attachments = message.attachments
        message_embeds = message.embeds
        channel = getattr(interaction, 'channel', None)
        if channel is None or not hasattr(channel, 'send'):
            await interaction.followup.send(content="Could not resolve channel to send copied message.", ephemeral=True)
            return
        files = []
        for att in message_attachments:
            if isinstance(att, discord.File):
                files.append(att)
        await interaction.followup.send(content="Message replaced.", ephemeral=True)
        await channel.send(content=message_content, files=files, embeds=message_embeds)

    @tree.context_menu(name="Edit message")
    @app_commands.checks.has_role("ServerOps")
    async def edit_message(interaction: discord.Interaction, message: discord.Message):
        await interaction.response.send_modal(bot_message_edit_modal_factory(message))