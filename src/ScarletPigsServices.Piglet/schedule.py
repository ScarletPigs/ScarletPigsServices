"""Piglet schedule storage backed by the Scarlet Pigs API."""

from __future__ import annotations

from copy import deepcopy
import datetime
import logging
import os
from typing import Any, cast
from zoneinfo import ZoneInfo

from scarletpigsapi import Event, JsonValue, ScarletPigsApiClient, get_client


DATE_AMOUNT_KEY = "piglet.schedule.date_amount"
SCHEDULE_MESSAGES_KEY = "piglet.discord.schedule_messages"
MODLIST_MESSAGES_KEY = "piglet.discord.modlist_messages"
QUESTIONNAIRE_MESSAGE_KEY = "piglet.discord.questionnaire_message"
QUESTIONNAIRE_INFO_KEY = "piglet.dlc_questionnaire"
GOOGLE_IMPORT_KEY = "piglet.google_sheets_import"
GOOGLE_IMPORT_VERSION = 1

EVENT_TYPE_KEY = os.getenv("PIGLET_EVENT_TYPE_KEY", "operation")
EVENT_TIME_ZONE = ZoneInfo(os.getenv("PIGLET_TIMEZONE", "Europe/Copenhagen"))
EVENT_START_HOUR = 15
EVENT_DURATION_MINUTES = 180

_client: ScarletPigsApiClient | None = None
_initialized = False


def initialize(client: ScarletPigsApiClient | None = None) -> None:
    """Prepare API storage and run the legacy import exactly once."""

    global _client, _initialized
    if client is not None:
        _client = client
    if _initialized:
        return

    api = _api()
    api.ensure_event_type(EVENT_TYPE_KEY)
    marker = api.get_setting(GOOGLE_IMPORT_KEY)
    if marker is not None and _is_completed_import(marker.value):
        _initialized = True
        logging.info("Legacy Google Sheets import was already completed.")
        return

    # Google dependencies and credentials are deliberately touched only here.
    from google_sheets_import import read_legacy_google_sheets

    logging.info("Importing legacy Piglet data from Google Sheets.")
    legacy = read_legacy_google_sheets()
    api.set_setting(DATE_AMOUNT_KEY, legacy.date_amount)
    api.set_setting(SCHEDULE_MESSAGES_KEY, legacy.schedule_messages)
    api.set_setting(MODLIST_MESSAGES_KEY, legacy.modlist_messages)
    api.set_setting(
        QUESTIONNAIRE_MESSAGE_KEY,
        legacy.questionnaire_message
        if legacy.questionnaire_message is not None
        else False,
    )
    api.set_setting(
        QUESTIONNAIRE_INFO_KEY,
        cast(JsonValue, legacy.questionnaire_info),
    )

    existing_events = api.get_events()
    for operation in legacy.operations:
        starts_at = _starts_at(operation.date)
        external_id = _legacy_external_id(starts_at.date())
        existing = next(
            (
                event
                for event in existing_events
                if event.external_id == external_id
                or (
                    event.type_key == EVENT_TYPE_KEY
                    and _event_date(event) == starts_at.date()
                )
            ),
            None,
        )
        if existing is not None:
            continue
        imported = api.create_event(
            name=operation.name,
            author=operation.author,
            starts_at=starts_at,
            type_key=EVENT_TYPE_KEY,
            duration_minutes=EVENT_DURATION_MINUTES,
            briefing=_briefing(operation.author),
            external_id=external_id,
            metadata={
                "import_source": "google_sheets",
                "legacy_sheet": operation.source,
                "legacy_date": operation.date,
            },
        )
        existing_events.append(imported)

    # This marker is intentionally the final write. A failed partial import is
    # safely retried, with stable external IDs preventing duplicate events.
    api.set_setting(
        GOOGLE_IMPORT_KEY,
        {
            "completed": True,
            "completed_at": datetime.datetime.now(
                datetime.timezone.utc
            ).isoformat(),
            "version": GOOGLE_IMPORT_VERSION,
        },
    )
    _initialized = True
    logging.info("Legacy Google Sheets import completed.")


def _api() -> ScarletPigsApiClient:
    global _client
    if _client is None:
        _client = get_client()
    return _client


def _is_completed_import(value: JsonValue) -> bool:
    if value is True:
        return True
    return isinstance(value, dict) and value.get("completed") is True


def _setting(key: str, default: JsonValue) -> JsonValue:
    setting = _api().get_setting(key)
    return deepcopy(default if setting is None else setting.value)


def _set_setting(key: str, value: JsonValue) -> None:
    _api().set_setting(key, value)


def get_schedule_messages() -> dict[str, Any]:
    value = _setting(SCHEDULE_MESSAGES_KEY, {"servers": []})
    return value if isinstance(value, dict) else {"servers": []}


def set_schedule_message_id(
    guild_id: int, channel_id: int, message_id: int
) -> None:
    serverdata = get_schedule_messages()
    servers = serverdata.setdefault("servers", [])
    guild_ids = [server["guild_id"] for server in servers]
    replacement = {
        "guild_id": guild_id,
        "channel_id": channel_id,
        "message_id": message_id,
    }
    if guild_id in guild_ids:
        servers[guild_ids.index(guild_id)] = replacement
    else:
        servers.append(replacement)
    _set_setting(SCHEDULE_MESSAGES_KEY, serverdata)


def remove_schedule_message(identifier: int) -> None:
    serverdata = get_schedule_messages()
    if _remove_message(serverdata, identifier):
        _set_setting(SCHEDULE_MESSAGES_KEY, serverdata)


def get_modlist_messages() -> dict[str, Any]:
    value = _setting(MODLIST_MESSAGES_KEY, {"servers": []})
    return value if isinstance(value, dict) else {"servers": []}


def add_modlist_message(
    guild_id: int, channel_id: int, message_id: int, file_path: str
) -> None:
    serverdata = get_modlist_messages()
    servers = serverdata.setdefault("servers", [])
    servers.append(
        {
            "guild_id": guild_id,
            "channel_id": channel_id,
            "message_id": message_id,
            "file_path": file_path,
        }
    )
    _set_setting(MODLIST_MESSAGES_KEY, serverdata)


def remove_modlist_message(identifier: int) -> None:
    serverdata = get_modlist_messages()
    if _remove_message(serverdata, identifier):
        _set_setting(MODLIST_MESSAGES_KEY, serverdata)


def _remove_message(serverdata: dict[str, Any], identifier: int) -> bool:
    servers = serverdata.get("servers")
    if not isinstance(servers, list):
        return False
    for index, server in enumerate(servers):
        if (
            isinstance(server, dict)
            and identifier
            in (
                server.get("guild_id"),
                server.get("channel_id"),
                server.get("message_id"),
            )
        ):
            servers.pop(index)
            return True
    return False


def get_questionnaire_message() -> dict[str, Any] | None:
    value = _setting(QUESTIONNAIRE_MESSAGE_KEY, None)
    return value if isinstance(value, dict) else None


def set_questionnaire_message(
    guild_id: int, channel_id: int, message_id: int
) -> None:
    _set_setting(
        QUESTIONNAIRE_MESSAGE_KEY,
        {
            "guild_id": guild_id,
            "channel_id": channel_id,
            "message_id": message_id,
        },
    )


def get_questionnaire_info() -> list[list[Any]]:
    value = _setting(QUESTIONNAIRE_INFO_KEY, [])
    if not isinstance(value, list):
        return []
    return [row for row in value if isinstance(row, list)]


def set_questionnaire_info(info: list[list[Any]]) -> None:
    _set_setting(QUESTIONNAIRE_INFO_KEY, cast(JsonValue, info))


def get_todays_date() -> datetime.date:
    now = datetime.datetime.now(EVENT_TIME_ZONE)
    today = now.date()
    if today.weekday() == 6 and now.hour >= 16:
        today += datetime.timedelta(days=1)
    return today


def get_next_sunday() -> datetime.date:
    today = get_todays_date()
    return today + datetime.timedelta(days=6 - today.weekday())


def get_next_n_sundays(n: int = 5) -> list[str]:
    next_sunday = get_next_sunday()
    return [
        (next_sunday + datetime.timedelta(days=index * 7)).strftime(
            "%b %d (%y)"
        )
        for index in range(n)
    ]


def get_schedule_dates() -> list[list[str]]:
    date_amount_value = _setting(DATE_AMOUNT_KEY, 10)
    date_amount = (
        int(date_amount_value)
        if isinstance(date_amount_value, (int, float, str))
        else 10
    )
    dates = get_next_n_sundays(max(1, date_amount))
    events_by_date = {
        _event_date(event): event
        for event in _api().get_events()
        if event.type_key == EVENT_TYPE_KEY
    }

    names: list[str] = []
    authors: list[str] = []
    for display_date in dates:
        event = events_by_date.get(_parse_date(display_date))
        names.append(event.name if event is not None else "")
        authors.append(event.author if event is not None else "")
    return [dates, names, authors]


def update_op(
    datex: str,
    opname: str | None = None,
    opauthor: str | None = None,
    author_discord_id: int | None = None,
) -> None:
    event = _event_on_date(_parse_date(datex))
    if event is None:
        if not opname:
            raise ValueError("An operation name is required for a new event.")
        author = opauthor or ""
        metadata: dict[str, JsonValue] = {"created_by": "piglet"}
        if author_discord_id is not None:
            metadata["author_discord_id"] = str(author_discord_id)
        _api().create_event(
            name=opname,
            author=author,
            starts_at=_starts_at(datex),
            type_key=EVENT_TYPE_KEY,
            duration_minutes=EVENT_DURATION_MINUTES,
            briefing=_briefing(author),
            external_id=f"piglet-schedule:{_parse_date(datex).isoformat()}",
            metadata=metadata,
        )
        return

    changes: dict[str, Any] = {}
    if opname is not None:
        changes["name"] = opname
    if opauthor is not None:
        changes["author"] = opauthor
        changes["briefing"] = _briefing(opauthor)
    if changes:
        _api().update_event(event.id, changes)


def delete_op(datex: str) -> None:
    event = _event_on_date(_parse_date(datex))
    if event is not None:
        _api().delete_event(event.id)


def get_op_data(
    date: str | None = None,
    op: str | None = None,
    author: str | None = None,
) -> list[str] | None:
    for entry in get_full_schedule():
        if (
            (date is not None and entry[0] == date)
            or (op is not None and entry[1] == op)
            or (author is not None and entry[2] == author)
        ):
            return list(entry)
    return None


def get_full_schedule() -> list[tuple[str, str, str]]:
    return list(zip(*get_schedule_dates()))


def get_free_dates() -> list[list[str]]:
    return [list(entry) for entry in get_full_schedule() if not entry[1]]


def get_booked_dates() -> list[list[str]]:
    return [list(entry) for entry in get_full_schedule() if entry[1]]


def _event_on_date(value: datetime.date) -> Event | None:
    return next(
        (
            event
            for event in _api().get_events()
            if event.type_key == EVENT_TYPE_KEY and _event_date(event) == value
        ),
        None,
    )


def _event_date(event: Event) -> datetime.date:
    return event.starts_at.astimezone(EVENT_TIME_ZONE).date()


def _parse_date(value: str) -> datetime.date:
    return datetime.datetime.strptime(value, "%b %d (%y)").date()


def _starts_at(value: str) -> datetime.datetime:
    parsed = _parse_date(value)
    return datetime.datetime.combine(
        parsed,
        datetime.time(hour=EVENT_START_HOUR),
        tzinfo=EVENT_TIME_ZONE,
    )


def _briefing(author: str) -> str:
    return f"Op made by {author}" if author else ""


def _legacy_external_id(value: datetime.date) -> str:
    return f"piglet-google-sheets:{value.isoformat()}"
