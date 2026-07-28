from __future__ import annotations

import asyncio
from pathlib import Path
from types import SimpleNamespace
import sys
import time
import unittest
from unittest.mock import AsyncMock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import discordbot


class DiscordBotAsyncTests(unittest.IsolatedAsyncioTestCase):
    async def test_schedule_api_work_does_not_block_discord(self) -> None:
        started_at = asyncio.get_running_loop().time()
        task = asyncio.create_task(
            discordbot.run_schedule(time.sleep, 0.25)
        )

        await asyncio.sleep(0.01)

        self.assertLess(
            asyncio.get_running_loop().time() - started_at,
            0.15,
        )
        await task

    async def test_edit_command_handles_an_empty_api_schedule(self) -> None:
        interaction = SimpleNamespace(
            response=SimpleNamespace(defer=AsyncMock()),
            followup=SimpleNamespace(send=AsyncMock()),
        )

        with patch.object(
            discordbot,
            "run_schedule",
            AsyncMock(return_value=[]),
        ):
            await discordbot.editsunday.callback(interaction)

        interaction.response.defer.assert_awaited_once_with(ephemeral=True)
        interaction.followup.send.assert_awaited_once_with(
            content="There are no booked operations to edit.",
            ephemeral=True,
        )


if __name__ == "__main__":
    unittest.main()
