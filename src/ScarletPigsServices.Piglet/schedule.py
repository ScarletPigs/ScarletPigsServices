import os
import datetime
import json
from typing import Any, Dict, List, Optional, Tuple

from dotenv import load_dotenv
import utils
import scarletpigsapi

log = utils.log_handler

# Load environment variables (for defaults like API URL and counts)
load_dotenv()

# -----------------------------
# Local JSON state persistence
# -----------------------------

STATE_DIR = os.path.join(os.path.dirname(__file__), "files")
STATE_PATH = os.path.join(STATE_DIR, "piglet_state.json")


def _ensure_state_dir():
    os.makedirs(STATE_DIR, exist_ok=True)


def _load_state() -> Dict[str, Any]:
    _ensure_state_dir()
    if os.path.exists(STATE_PATH):
        try:
            with open(STATE_PATH, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception:
            pass
    # default state
    return {
        "schedule_dates_count": int(os.getenv("SCHEDULE_DATES_COUNT", "10")),
        "schedule_messages": {"servers": []},
        "modlist_messages": {"servers": []},
        "questionnaire_message": None,
        # Header row then [Name, Count, Emoji]
        "questionnaire_info": [["DLC", "Count", "Emoji"]],
    }


def _save_state(state: Dict[str, Any]) -> None:
    _ensure_state_dir()
    with open(STATE_PATH, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)


def _get_date_amount() -> int:
    state = _load_state()
    try:
        return int(state.get("schedule_dates_count", 10))
    except Exception:
        return 10


# ---------------------
# Scheduled msg mapping
# ---------------------

def get_schedule_messages() -> Dict[str, List[Dict[str, int]]]:
    state = _load_state()
    return state.get("schedule_messages", {"servers": []})

# Save schedule message


def set_schedule_message_id(guild_id: int, channel_id: int, message_id: int):
    state = _load_state()
    serverdata = state.get("schedule_messages", {"servers": []})
    guild_ids = [server.get("guild_id")
                 for server in serverdata.get("servers", [])]

    if guild_id in guild_ids:
        index = guild_ids.index(guild_id)
        serverdata["servers"][index]["channel_id"] = channel_id
        serverdata["servers"][index]["message_id"] = message_id
    else:
        serverdata.setdefault("servers", []).append(
            {"guild_id": guild_id, "channel_id": channel_id, "message_id": message_id}
        )

    state["schedule_messages"] = serverdata
    _save_state(state)

# Remove a schedule message


def remove_schedule_message(id: int):
    state = _load_state()
    serverdata = state.get("schedule_messages", {"servers": []})
    servers = serverdata.get("servers", [])
    guild_ids = [server.get("guild_id") for server in servers]
    channel_ids = [server.get("channel_id") for server in servers]
    message_ids = [server.get("message_id") for server in servers]

    index = -1
    if id in guild_ids:
        index = guild_ids.index(id)
    elif id in channel_ids:
        index = channel_ids.index(id)
    elif id in message_ids:
        index = message_ids.index(id)
    else:
        return

    servers.pop(index)
    state["schedule_messages"] = {"servers": servers}
    _save_state(state)


def get_modlist_messages() -> Dict[str, List[Dict[str, Any]]]:
    state = _load_state()
    return state.get("modlist_messages", {"servers": []})

# Save modlist message


def add_modlist_message(guild_id: int, channel_id: int, message_id: int, file_path: str):
    state = _load_state()
    serverdata = state.get("modlist_messages", {"servers": []})
    serverdata.setdefault("servers", []).append(
        {"guild_id": guild_id, "channel_id": channel_id,
            "message_id": message_id, "file_path": file_path}
    )
    state["modlist_messages"] = serverdata
    _save_state(state)

# Remove a modlist message


def remove_modlist_message(id: int):
    state = _load_state()
    serverdata = state.get("modlist_messages", {"servers": []})
    servers = serverdata.get("servers", [])
    guild_ids = [server.get("guild_id") for server in servers]
    channel_ids = [server.get("channel_id") for server in servers]
    message_ids = [server.get("message_id") for server in servers]

    index = -1
    if id in guild_ids:
        index = guild_ids.index(id)
    elif id in channel_ids:
        index = channel_ids.index(id)
    elif id in message_ids:
        index = message_ids.index(id)
    else:
        return

    servers.pop(index)
    state["modlist_messages"] = {"servers": servers}
    _save_state(state)


def get_questionnaire_message() -> Optional[Dict[str, int]]:
    state = _load_state()
    return state.get("questionnaire_message")

# Save questionnaire message


def set_questionnaire_message(guild_id: int, channel_id: int, message_id: int):
    state = _load_state()
    state["questionnaire_message"] = {
        "guild_id": guild_id,
        "channel_id": channel_id,
        "message_id": message_id,
    }
    _save_state(state)


def get_questionnaire_info() -> List[List[Any]]:
    state = _load_state()
    return state.get("questionnaire_info", [["DLC", "Count", "Emoji"]])


def set_questionnaire_info(info: List[List[Any]]):
    state = _load_state()
    state["questionnaire_info"] = info
    _save_state(state)

# -----------------------------
# Server status config in state
# -----------------------------


def get_server_connection() -> Tuple[Optional[str], Optional[int]]:
    state = _load_state()
    ip = state.get("server_ip")
    port = state.get("server_port")
    try:
        port_int = int(port) if port is not None else None
    except Exception:
        port_int = None
    return ip, port_int


def set_server_connection(ip: Optional[str], port: Optional[int]) -> None:
    state = _load_state()
    state["server_ip"] = ip
    state["server_port"] = port
    _save_state(state)


def get_todays_date() -> datetime.date:
    today = datetime.date.today()
    if today.weekday() == 6 and datetime.datetime.now().hour >= 16:
        today = today + datetime.timedelta(days=1)
    return today

# Get the date of the next sunday


def get_next_sunday() -> datetime.date:
    today = get_todays_date()
    next_sunday = today + datetime.timedelta(days=(6 - today.weekday()))
    return next_sunday

# Get a list over the next n amount of Sundays


def get_next_n_sundays(n: int = 5) -> List[str]:
    next_sunday = get_next_sunday()
    next_n_sundays: List[str] = []
    for i in range(n):
        sunday_after = next_sunday + datetime.timedelta(days=i * 7)
        next_n_sundays.append(sunday_after.strftime("%b %d (%y)"))
    return next_n_sundays

# Return schedule dates


def _parse_event_author(event: Dict[str, Any]) -> str:
    # Prefer explicit author if present; fallback: look in description
    author = event.get("author") or ""
    if not author:
        desc = (event.get("description") or "").lower()
        # naive parse "op made by <name>"
        key = "op made by "
        if key in desc:
            try:
                author = event.get("description", "")[
                    len("Op made by "):].strip()
            except Exception:
                author = ""
    return author or ""


def get_schedule_dates() -> List[List[str]]:
    """Return three lists: dates, names, authors for the next N Sundays using API events."""
    next_sundays = get_next_n_sundays(_get_date_amount())

    try:
        # Build range from first to last wanted Sunday in UTC day span
        first_dt = datetime.datetime.strptime(next_sundays[0], "%b %d (%y)").replace(
            hour=0, minute=0, second=0, microsecond=0)
        last_dt = datetime.datetime.strptime(
            next_sundays[-1], "%b %d (%y)").replace(hour=23, minute=59, second=59, microsecond=0)
        events = scarletpigsapi.get_events_between(first_dt, last_dt) or []
    except Exception:
        try:
            events = scarletpigsapi.get_events() or []
        except Exception:
            events = []

    # Index events by yyyy-mm-dd for quick lookup
    events_by_date: Dict[str, Dict[str, Any]] = {}
    for ev in events:
        try:
            start_iso = ev.get("startTime") or ev.get("StartTime")
            if not start_iso:
                continue
            start_dt = datetime.datetime.fromisoformat(
                start_iso.replace("Z", "+00:00"))
            key = start_dt.strftime("%Y-%m-%d")
            events_by_date[key] = ev
        except Exception:
            continue

    ops: List[Tuple[str, str, str]] = []
    for ds in next_sundays:
        # parse ds back to date to key lookup
        try:
            dt = datetime.datetime.strptime(ds, "%b %d (%y)")
        except Exception:
            dt = None  # type: ignore
        name = ""
        author = ""
        if dt is not None:
            key = dt.strftime("%Y-%m-%d")
            ev = events_by_date.get(key)
            if ev:
                name = ev.get("name") or ev.get("Name") or ""
                author = _parse_event_author(ev)
        ops.append((ds, name, author))

    # Return the new schedule in a more easily usable format
    dates: List[str] = []
    names: List[str] = []
    authors: List[str] = []
    for d, n, a in ops:
        dates.append(d)
        names.append(n)
        authors.append(a)

    return [dates, names, authors]

# Updates an op entry in the schedule
# Use
#   update_op("Nov 06 (22)", opname = "OP Name") to update only opname
# or
#   update_op("Nov 06 (22)", opauthor = "OP Author") to update only author


def update_op(datex: str, opname: Optional[str] = None, opauthor: Optional[str] = None):
    # No-op: Event creation/update is handled via scarletpigsapi in the bot flows.
    # Kept for backward compatibility.
    return None


def delete_op(datex: str):
    # No-op: Event deletion is handled via scarletpigsapi in the bot flows.
    return None

# Get data on specific op


def get_op_data(date: Optional[str] = None, op: Optional[str] = None, author: Optional[str] = None):
    schedule = get_full_schedule()
    # schedule entries are [date, name, author]
    if date is not None:
        for entry in schedule:
            if entry[0] == date:
                return entry
    elif op is not None:
        for entry in schedule:
            if entry[1] == op:
                return entry
    elif author is not None:
        for entry in schedule:
            if entry[2] == author:
                return entry
    return None

# Returns a list of all op entries in the sheet


def get_full_schedule() -> List[List[str]]:
    full_schedule = get_schedule_dates()
    entries: List[List[str]] = list(map(list, zip(*full_schedule)))
    return entries

# Get the dates without an op


def get_free_dates() -> List[List[str]]:
    full_schedule = get_full_schedule()
    free_dates: List[List[str]] = []
    for entry in full_schedule:
        if entry[1] == "" or entry[1] is None:
            free_dates.append([entry[0], entry[1], entry[2]])
    return free_dates

# Get the dates with an op


def get_booked_dates() -> List[List[str]]:
    full_schedule = get_full_schedule()
    booked_dates: List[List[str]] = []
    for entry in full_schedule:
        if entry[1] != "" and entry[1] is not None:
            booked_dates.append([entry[0], entry[1], entry[2]])
    return booked_dates


print("Schedule system initialized (API + local state)")
