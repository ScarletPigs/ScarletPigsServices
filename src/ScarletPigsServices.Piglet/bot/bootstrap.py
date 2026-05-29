import base64
import datetime
import logging
import os
import sys

import discord
from discord import app_commands
from github import Github

from bot.commands import register_message_commands, register_schedule_commands
from bot.config import CREATOR_ID, DISCORD_TOKEN, DLC_REFRESH_DEBOUNCE_SECONDS, GITHUB_TOKEN
from bot.jobs import check_dlc_message, configure_jobs, debug_print_schedule_messages, handle_raw_reaction_payload, schedule_loop, startup_runtime_tasks, update_scheduled_messages
from bot.runtime import BOT, TREE, run_bot
from bot.services.formatting import format_dlc_list, format_schedule_message
from bot.services.reactions import get_emojis_in_message, get_reactions_from_message, process_reaction
from bot.ui import BOTMessageEditModal, DateSelect, OpEditModal, OpEditSelect, configure_modals, configure_selects

gh = Github(login_or_token=GITHUB_TOKEN)
_configured = False


def retrieve_file_from_github(file_path: str):
    try:
        data = gh.get_repo(
            f"MacbainSP/Scarlet-Pigs-Server-Stuff").get_contents(file_path)
        if data is None:
            return None

        file_content = data.content  # type: ignore
        file_name = data.name  # type: ignore

        os.makedirs("files", exist_ok=True)
        with open(f"files/{file_name}", "wb") as f:
            f.write(base64.decodebytes(file_content.encode('utf-8')))
        return file_name
    except Exception as e:
        print(e)


async def error_response(interaction: discord.Interaction, message: str, expected: bool = True):
    try:
        await interaction.response.send_message(content=message, ephemeral=True)
    except Exception:
        await interaction.followup.send(content=message, ephemeral=True)
    finally:
        if not expected:
            try:
                creator_id_int = int(CREATOR_ID) if CREATOR_ID is not None else None
            except Exception:
                creator_id_int = None
            if creator_id_int:
                creator = await BOT.fetch_user(creator_id_int)
                await creator.send(f"[{datetime.datetime.now()}] - {interaction.user} tried to use a command. Something went wrong! \n({message})")
            raise Exception(message)


@TREE.error
async def on_app_command_error(interaction: discord.Interaction, error: app_commands.AppCommandError):
    if isinstance(error, app_commands.CommandOnCooldown):
        await error_response(interaction, f'This command is on cooldown. Try again in {round(error.retry_after)} seconds.')
    elif isinstance(error, app_commands.MissingRole):
        await error_response(interaction, "You do not have the required role for this command")
    else:
        await error_response(interaction, str(error), False)


def try_api_call(api_func, *args, **kwargs):
    try:
        return api_func(*args, **kwargs)
    except Exception as e:
        logging.error(f"API call failed: {e}")
        return None


def configure_bot():
    global _configured
    if _configured:
        return

    configure_modals(schedule_loop, try_api_call)
    configure_selects(update_scheduled_messages, OpEditModal)
    configure_jobs(format_schedule_message, retrieve_file_from_github, process_reaction)

    register_schedule_commands(
        TREE,
        BOT,
        DateSelect,
        OpEditSelect,
        format_schedule_message,
        retrieve_file_from_github,
        format_dlc_list,
        check_dlc_message,
    )
    register_message_commands(
        TREE,
        get_emojis_in_message,
        get_reactions_from_message,
        BOTMessageEditModal,
    )

    BOT.startup_callback = startup_runtime_tasks
    BOT.debug_handler = debug_print_schedule_messages
    BOT.raw_reaction_callback = lambda payload: handle_raw_reaction_payload(payload, DLC_REFRESH_DEBOUNCE_SECONDS)
    _configured = True


def run_main(argv: list[str] | None = None):
    configure_bot()
    args = argv if argv is not None else sys.argv[1:]
    discordtoken = DISCORD_TOKEN
    if not discordtoken:
        print("No discord token found. Please set the DISCORD_TOKEN environment variable.")
        raise SystemExit(1)

    if len(args) > 0 and args[0] == "print-schedule-messages":
        run_bot(discordtoken, debug_action="print_schedule_messages")
        return

    if len(args) > 0:
        print('Usage: python main.py [print-schedule-messages]')
        raise SystemExit(1)

    run_bot(discordtoken)


configure_bot()