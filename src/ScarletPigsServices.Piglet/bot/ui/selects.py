from __future__ import annotations

from collections.abc import Awaitable, Callable
import datetime

import discord

from bot.services import schedule_service as schedule
from bot.services import api_service

_update_scheduled_messages: Callable[[str, dict], Awaitable[None]] | None = None
_op_edit_modal_factory: Callable[[str, str, str], discord.ui.Modal] | None = None


def configure_selects(
    update_scheduled_messages: Callable[[str, dict], Awaitable[None]],
    op_edit_modal_factory: Callable[[str, str, str], discord.ui.Modal],
):
    global _update_scheduled_messages, _op_edit_modal_factory
    _update_scheduled_messages = update_scheduled_messages
    _op_edit_modal_factory = op_edit_modal_factory


class DateSelect(discord.ui.Select):
    def __init__(self, opname, opauthor):
        self.opname = opname
        self.opauthor = opauthor
        next_sundays = schedule.get_free_dates()
        options = []
        for sunday in next_sundays:
            date = sunday[0]
            options.append(discord.SelectOption(label=date, value=date))
        super().__init__(placeholder="Choose the date",
                         min_values=1, max_values=1, options=options)

    async def callback(self, interaction: discord.Interaction):
        opdate = self.values[0]
        content = f"Reserved {self.opname} by {self.opauthor} for {opdate}"
        schedule.update_op(opdate, self.opname, self.opauthor)
        embed = discord.Embed(title="Reserved a Sunday", description=content,
                              timestamp=datetime.datetime.utcnow(), color=discord.Colour.blue())
        if interaction.guild_id is not None:
            try:
                starttime = datetime.datetime.strptime(
                    self.values[0], "%b %d (%y)").replace(hour=16, minute=0, second=0)
                endtime = datetime.datetime.strptime(
                    self.values[0], "%b %d (%y)").replace(hour=18, minute=0, second=0)
                description = f"Op made by {self.opauthor}"
                authorid = interaction.user.id
                api_service.create_event(
                    self.opname, description, self.opauthor, authorid, starttime, endtime)
            except Exception as e:
                print(e)
        if _update_scheduled_messages is not None:
            await _update_scheduled_messages("schedule", schedule.get_schedule_messages())
        await interaction.edit_original_response(content=content, embed=embed, view=None)


class OpEditSelect(discord.ui.Select):
    def __init__(self, isDelete: bool = False):
        self.isDelete = isDelete
        next_booked_ops = schedule.get_booked_dates()
        options = []
        for booked_op in next_booked_ops:
            opname = booked_op[1]
            opdate = booked_op[0]
            options.append(discord.SelectOption(label=opname, value=opdate))
        super().__init__(placeholder="Choose the op",
                         min_values=1, max_values=1, options=options)

    async def callback(self, interaction: discord.Interaction):
        op = schedule.get_op_data(date=self.values[0])
        if op is None or len(op) < 3:
            await interaction.response.send_message(content="Could not find op data.", ephemeral=True)
            return
        if self.isDelete:
            op_date = op[0]
            try:
                dt = datetime.datetime.strptime(
                    op_date, "%b %d (%y)").replace(hour=16, minute=0, second=0)
            except Exception:
                await interaction.response.send_message(content="Invalid op date format.", ephemeral=True)
                return
            event = api_service.get_event_at_date(dt)
            if not event or "id" not in event:
                await interaction.response.send_message(content="Could not find event to delete.", ephemeral=True)
                return
            event_id = event["id"]
            api_service.delete_event(event_id)
            schedule.delete_op(op_date)
            await interaction.response.send_message(content=f"Op {op[1]} deleted", ephemeral=True)
        elif _op_edit_modal_factory is not None:
            await interaction.response.send_modal(_op_edit_modal_factory(op[0], op[1], op[2]))