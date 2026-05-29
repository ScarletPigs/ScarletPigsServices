from __future__ import annotations

from collections.abc import Awaitable, Callable
import asyncio

import discord
from discord import app_commands


class SPiglet(discord.Client):
    def __init__(self):
        self.server_start_time: int | None = None
        self.dlc_refresh_task: asyncio.Task[None] | None = None
        self.dlc_refresh_lock = asyncio.Lock()
        self.debug_action: str | None = None
        self.synced = False
        self.server_status = "offline"
        self.startup_callback: Callable[[], Awaitable[None]] | None = None
        self.debug_handler: Callable[[], Awaitable[None]] | None = None
        self.raw_reaction_callback: Callable[[discord.RawReactionActionEvent], Awaitable[None]] | None = None
        super().__init__(intents=discord.Intents.default())

    async def on_ready(self):
        await self.wait_until_ready()
        if self.debug_action == "print_schedule_messages" and self.debug_handler is not None:
            await self.debug_handler()
            await self.close()
            return
        if not self.synced:
            print("Syncing commands...")
            for com in TREE.get_commands():
                print(f"Syncing {com.name}")
            await TREE.sync()
            self.synced = True
        if self.startup_callback is not None:
            await self.startup_callback()

    async def on_command_error(self, ctx, error):
        await ctx.reply(str(error), ephemeral=True)

    async def on_raw_reaction_add(self, payload: discord.RawReactionActionEvent):
        if self.raw_reaction_callback is not None:
            await self.raw_reaction_callback(payload)

    async def on_raw_reaction_remove(self, payload: discord.RawReactionActionEvent):
        if self.raw_reaction_callback is not None:
            await self.raw_reaction_callback(payload)


BOT = SPiglet()
TREE = app_commands.CommandTree(BOT)


def run_bot(token: str, debug_action: str | None = None):
    BOT.debug_action = debug_action
    BOT.run(token=token)