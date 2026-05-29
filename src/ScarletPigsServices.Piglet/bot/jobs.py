from __future__ import annotations

from collections.abc import Awaitable, Callable
import asyncio
import datetime

from a2squery import A2SQuery
import discord
from discord.ext import tasks

from bot.config import SERVER_IP, SERVER_PORT
from bot.services import schedule_service as schedule
from bot.runtime import BOT

_format_schedule_message: Callable[[], str] | None = None
_retrieve_file_from_github: Callable[[str], str | None] | None = None
_process_reaction: Callable[[discord.Message, discord.Reaction], Awaitable[int]] | None = None


def configure_jobs(
    format_schedule_message: Callable[[], str],
    retrieve_file_from_github: Callable[[str], str | None],
    process_reaction: Callable[[discord.Message, discord.Reaction], Awaitable[int]],
):
    global _format_schedule_message, _retrieve_file_from_github, _process_reaction
    _format_schedule_message = format_schedule_message
    _retrieve_file_from_github = retrieve_file_from_github
    _process_reaction = process_reaction


async def update_scheduled_messages(category: str, messages: dict):
    print(f'Updating {category} messages...')
    for server in messages['servers']:
        guild_id = server['guild_id']
        guild = BOT.get_guild(guild_id)
        if guild is None:
            continue
        channel_id = server['channel_id']
        channel = guild.get_channel(channel_id)
        if channel is None or not hasattr(channel, 'fetch_message'):
            continue
        message_id = server['message_id']
        try:
            if isinstance(channel, discord.TextChannel):
                msg = await channel.fetch_message(message_id)
        except Exception:
            print(f'The {category} message for {getattr(guild, "name", "?")} in channel {getattr(channel, "name", "?")} could not be found! Removing it from the database.')
            if category == "schedule":
                schedule.remove_schedule_message(message_id)
            elif category == "modlist":
                schedule.remove_modlist_message(message_id)
            continue
        if not hasattr(msg, 'author') or not hasattr(BOT, 'user') or msg.author is None or BOT.user is None or msg.author.id != BOT.user.id:
            continue
        print(f'Updating {category} for {getattr(guild, "name", "?")} in channel {getattr(channel, "name", "?")}')
        if category == "schedule" and _format_schedule_message is not None:
            await msg.edit(content=_format_schedule_message())
        elif category == "modlist" and _retrieve_file_from_github is not None:
            file_path = server['file_path']
            file_name = _retrieve_file_from_github(file_path)
            if file_name is None:
                continue
            await msg.edit(attachments=[discord.File(f"files/{file_name}")])
            os.remove(f"files/{file_name}")


async def debug_print_schedule_messages():
    print('Fetching stored schedule messages...')
    messages = schedule.get_schedule_messages()
    servers = messages.get('servers', [])
    if not servers:
        print('No schedule messages are configured.')
        return
    if _format_schedule_message is None:
        return

    expected_content = _format_schedule_message()
    for server in servers:
        guild_id = server.get('guild_id')
        channel_id = server.get('channel_id')
        message_id = server.get('message_id')
        print(f'--- Schedule message guild={guild_id} channel={channel_id} message={message_id} ---')

        guild = BOT.get_guild(guild_id)
        if guild is None:
            print('Guild is not available to the bot.')
            continue

        channel = guild.get_channel(channel_id)
        if channel is None or not isinstance(channel, discord.TextChannel):
            print('Channel is not available or does not support fetch_message.')
            continue

        try:
            msg = await channel.fetch_message(message_id)
        except Exception as error:
            print(f'Failed to fetch message: {error}')
            continue

        print('Current content:')
        print(msg.content)
        print('Expected content:')
        print(expected_content)
        print(f'Content matches expected: {msg.content == expected_content}')


async def queue_dlc_message_refresh(delay: float):
    existing_task = getattr(BOT, 'dlc_refresh_task', None)
    if existing_task is not None and not existing_task.done():
        existing_task.cancel()

    async def delayed_refresh():
        try:
            await asyncio.sleep(delay)
            await check_dlc_message()
        except asyncio.CancelledError:
            return

    BOT.dlc_refresh_task = asyncio.create_task(delayed_refresh())


async def handle_raw_reaction_payload(payload: discord.RawReactionActionEvent, debounce_seconds: float):
    questionnaire_message = schedule.get_questionnaire_message()
    if questionnaire_message is None:
        return
    if payload.message_id != questionnaire_message.get('message_id'):
        return
    if BOT.user is not None and payload.user_id == BOT.user.id:
        return
    await queue_dlc_message_refresh(debounce_seconds)


async def check_dlc_message():
    async with BOT.dlc_refresh_lock:
        print('Updating DLC graph...')
        questionnaire_message = schedule.get_questionnaire_message()
        if questionnaire_message is None or 'guild_id' not in questionnaire_message or 'channel_id' not in questionnaire_message or 'message_id' not in questionnaire_message:
            return
        questionnaire_info = schedule.get_questionnaire_info()
        guild = BOT.get_guild(questionnaire_message['guild_id'])
        if guild is None:
            return
        channel = guild.get_channel(questionnaire_message['channel_id'])
        if channel is None or not hasattr(channel, 'fetch_message'):
            return
        fetch_message_fn = getattr(channel, 'fetch_message', None)
        if not callable(fetch_message_fn):
            return
        try:
            import typing
            fetch_message_coro = typing.cast(
                "typing.Callable[[int], typing.Awaitable[discord.Message]]", fetch_message_fn)
            message = await fetch_message_coro(questionnaire_message['message_id'])
        except Exception:
            return

        counts_by_emoji = {}
        for reaction in message.reactions:
            emoji_name = reaction.emoji if not reaction.is_custom_emoji() else reaction.emoji.name  # type: ignore
            if _process_reaction is None:
                return
            counts_by_emoji[str(emoji_name)] = await _process_reaction(message, reaction)

        updated_questionnaire_info = [questionnaire_info[0]]
        for info in questionnaire_info[1:]:
            emoji_name = info[2]
            updated_questionnaire_info.append(
                [info[0], counts_by_emoji.get(str(emoji_name), 0), emoji_name])
        schedule.set_questionnaire_info(updated_questionnaire_info)


async def schedule_loop():
    await BOT.wait_until_ready()

    if not BOT.is_closed():
        try:
            asyncio.create_task(check_dlc_message())
        except Exception as e:
            print(e)

        try:
            asyncio.create_task(update_scheduled_messages(
                "schedule", schedule.get_schedule_messages()))
        except Exception as e:
            print(e)

        try:
            asyncio.create_task(update_scheduled_messages(
                "modlist", schedule.get_modlist_messages()))
        except Exception as e:
            print(e)


async def activity_loop():
    if not BOT.is_closed():
        print("Updating server status...")

        server_ip = SERVER_IP
        server_port = SERVER_PORT
        if not server_ip or not server_port:
            await BOT.change_presence(activity=discord.Activity(type=discord.ActivityType.watching, name="an offline server"))
            print("SERVER_IP or SERVER_PORT not set.")
            return
        try:
            port_int = int(server_port) + 1
        except Exception:
            await BOT.change_presence(activity=discord.Activity(type=discord.ActivityType.watching, name="an offline server"))
            print("SERVER_PORT is not a valid integer.")
            return
        try:
            with A2SQuery(server_ip, port_int, timeout=7) as a2s:
                if BOT.server_status == "offline":
                    BOT.server_start_time = int(datetime.datetime.now().replace(
                        tzinfo=datetime.timezone.utc).timestamp() * 1000)
                    BOT.server_status = "online"
                num_players = a2s.info().players
                mission = a2s.info().game
                plural_str = "s" if num_players != 1 else ""
                await BOT.change_presence(activity=discord.Activity(application_id=1035166922033082468, assets={"large_image": "pigs_patch", "large_text": "The Scarlet Pigs Server", "small_image": "pigs_patch", "small_text": "The Scarlet Pigs Server"}, type=discord.ActivityType.watching, name=f"{num_players} player" + plural_str + f" on {mission}", state="Running", timestamps={"start": BOT.server_start_time, "end": None}))
                print("Updated server status to online - Start time set to " + str(BOT.server_start_time))
        except TimeoutError:
            await BOT.change_presence(activity=discord.Activity(type=discord.ActivityType.watching, name="an offline server"))
            if BOT.server_status == "online":
                BOT.server_start_time = None
                BOT.server_status = "offline"
                print("Updated server status to offline")
        except Exception as e:
            if BOT.server_status == "online":
                BOT.server_start_time = None
                BOT.server_status = "offline"
            print(e)
            print("Something went wrong while updating the server status")
            await BOT.change_presence(activity=discord.Activity(type=discord.ActivityType.watching, name="an offline server"))

        print("Checked server status")


@tasks.loop(minutes=1)
async def loop_tasks():
    await BOT.wait_until_ready()
    i = loop_tasks.current_loop
    if i == 0:
        print("Started loop tasks")
    if (i % 2) == 0:
        await activity_loop()
    if (i % 60) == 0:
        await schedule_loop()
        await activity_loop()


async def startup_runtime_tasks():
    if not loop_tasks.is_running():
        loop_tasks.start()