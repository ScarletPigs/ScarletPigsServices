from __future__ import annotations

from collections.abc import Awaitable, Callable
import datetime

import discord
from discord import ui

from bot.services import schedule_service as schedule
from bot.services import api_service

_schedule_loop: Callable[[], Awaitable[None]] | None = None
_try_api_call: Callable[..., object] | None = None


def configure_modals(
    schedule_loop: Callable[[], Awaitable[None]],
    try_api_call: Callable[..., object],
):
    global _schedule_loop, _try_api_call
    _schedule_loop = schedule_loop
    _try_api_call = try_api_call


class OpEditModal(discord.ui.Modal, title="Edit an op"):
    opname = ui.TextInput(label='OP Name', min_length=1, max_length=31)
    author = ui.TextInput(label='Author', min_length=1, max_length=15)

    def __init__(self, date, opnamevalue, authorvalue):
        self.opname.default = opnamevalue
        self.author.default = authorvalue
        self.date = date
        super().__init__()

    async def on_submit(self, interaction: discord.Interaction):
        await interaction.response.defer()
        schedule.update_op(self.date, self.opname.value, self.author.value)

        try:
            isodate = datetime.datetime.strptime(
                self.date, "%b %d (%y)").replace(hour=16, minute=0, second=0)
        except Exception:
            await interaction.followup.send(content="Invalid date format for event.", ephemeral=True)
            return

        event = _try_api_call(api_service.get_event_at_date, isodate) if _try_api_call is not None else None
        if event:
            event["name"] = self.opname.value
            event["description"] = f"Op made by {self.author.value}"
            if _try_api_call is not None:
                _try_api_call(api_service.edit_event, event)

        if _schedule_loop is not None:
            await _schedule_loop()
        embed = discord.Embed(title="Edited a Sunday", description=f"Op named {self.opname.value} made by {
                              self.author.value} is booked for {self.date}.", timestamp=datetime.datetime.utcnow(), color=discord.Colour.blue())
        await interaction.followup.send(content="Op edited (API fallback if needed)", embed=embed, ephemeral=True)


class BOTMessageEditModal(discord.ui.Modal, title="Edit BOT message"):
    edit_message_textfield = ui.TextInput(
        style=discord.TextStyle.paragraph, label='Message', min_length=1, max_length=2000)

    def __init__(self, message):
        self.message = message
        self.edit_message_textfield.default = message.content
        super().__init__()

    async def on_submit(self, interaction: discord.Interaction):
        await self.message.edit(content=self.edit_message_textfield.value)
        await interaction.response.send_message("Message edited", ephemeral=True)