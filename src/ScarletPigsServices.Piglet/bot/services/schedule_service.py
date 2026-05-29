import datetime
import json

from . import sheets_store


date_amount = int(sheets_store.get_cell_entry(2, 7))
schedule_message_info = sheets_store.get_cell_entry(3, 7)
modlist_message_info = sheets_store.get_cell_entry(4, 7)
questionnaire_message_info = sheets_store.get_cell_entry(5, 7)


def _refresh_cached_settings():
    global date_amount, schedule_message_info, modlist_message_info, questionnaire_message_info
    date_amount = int(sheets_store.get_cell_entry(2, 7))
    schedule_message_info = sheets_store.get_cell_entry(3, 7)
    modlist_message_info = sheets_store.get_cell_entry(4, 7)
    questionnaire_message_info = sheets_store.get_cell_entry(5, 7)


def update_local_sheet():
    sheets_store.update_local_sheet()
    _refresh_cached_settings()


def update_online_sheet():
    sheets_store.update_online_sheet()


def get_schedule_messages():
    if schedule_message_info == "" or schedule_message_info is None:
        return {"servers": []}
    return json.loads(schedule_message_info)


def set_schedule_message_id(guild_id: int, channel_id: int, message_id: int):
    serverdata = get_schedule_messages()
    guild_ids = [server["guild_id"] for server in serverdata["servers"]]

    if guild_id in guild_ids:
        index = guild_ids.index(guild_id)
        serverdata["servers"][index]["channel_id"] = channel_id
        serverdata["servers"][index]["message_id"] = message_id
    else:
        serverdata["servers"].append(
            {"guild_id": guild_id, "channel_id": channel_id, "message_id": message_id})

    sheets_store.set_cell_entry(3, 7, json.dumps(serverdata))
    update_online_sheet()
    update_local_sheet()


def remove_schedule_message(id: int):
    serverdata = get_schedule_messages()
    guild_ids = [server["guild_id"] for server in serverdata["servers"]]
    channel_ids = [server["channel_id"] for server in serverdata["servers"]]
    message_ids = [server["message_id"] for server in serverdata["servers"]]

    if id in guild_ids:
        index = guild_ids.index(id)
    elif id in channel_ids:
        index = channel_ids.index(id)
    elif id in message_ids:
        index = message_ids.index(id)
    else:
        return

    serverdata["servers"].pop(index)
    sheets_store.set_cell_entry(3, 7, json.dumps(serverdata))
    update_online_sheet()
    update_local_sheet()


def get_modlist_messages():
    if modlist_message_info == "" or modlist_message_info is None:
        return {"servers": []}
    return json.loads(modlist_message_info)


def add_modlist_message(guild_id: int, channel_id: int, message_id: int, file_path: str):
    serverdata = get_modlist_messages()
    serverdata["servers"].append(
        {"guild_id": guild_id, "channel_id": channel_id, "message_id": message_id, "file_path": file_path})

    sheets_store.set_cell_entry(4, 7, json.dumps(serverdata))
    update_online_sheet()
    update_local_sheet()


def remove_modlist_message(id: int):
    serverdata = get_modlist_messages()
    guild_ids = [server["guild_id"] for server in serverdata["servers"]]
    channel_ids = [server["channel_id"] for server in serverdata["servers"]]
    message_ids = [server["message_id"] for server in serverdata["servers"]]

    if id in guild_ids:
        index = guild_ids.index(id)
    elif id in channel_ids:
        index = channel_ids.index(id)
    elif id in message_ids:
        index = message_ids.index(id)
    else:
        return

    serverdata["servers"].pop(index)
    sheets_store.set_cell_entry(4, 7, json.dumps(serverdata))
    update_online_sheet()
    update_local_sheet()


def get_questionnaire_message():
    update_local_sheet()
    if questionnaire_message_info == "" or questionnaire_message_info is None:
        return None
    return json.loads(questionnaire_message_info)


def set_questionnaire_message(guild_id: int, channel_id: int, message_id: int):
    serverdata = {"guild_id": guild_id, "channel_id": channel_id, "message_id": message_id}
    sheets_store.set_cell_entry(5, 7, json.dumps(serverdata))
    update_online_sheet()
    update_local_sheet()


def get_questionnaire_info():
    return sheets_store.dlc_sheet.get_all_values()


def set_questionnaire_info(info):
    sheets_store.dlc_sheet.update(info)


def get_todays_date():
    today = datetime.date.today()
    if today.weekday() == 6 and datetime.datetime.now().hour >= 16:
        today = today + datetime.timedelta(days=1)
    return today


def get_next_sunday():
    today = get_todays_date()
    next_sunday = today + datetime.timedelta(days=(6 - today.weekday()))
    return next_sunday


def get_next_n_sundays(n=5):
    next_sunday = get_next_sunday()
    next_n_sundays = []
    for i in range(n):
        sunday_after = next_sunday + datetime.timedelta(days=i * 7)
        next_n_sundays.append(sunday_after.strftime("%b %d (%y)"))
    return next_n_sundays


def get_schedule_dates():
    update_local_sheet()
    dates = [row[0] for row in sheets_store.entire_sheet]
    names = [row[1] for row in sheets_store.entire_sheet]
    authors = [row[2] for row in sheets_store.entire_sheet]
    old_ops = [dates, names, authors]
    next_sundays = get_next_n_sundays(date_amount)
    ops = []

    previous_sundays = [date for date in old_ops[0] if date not in next_sundays]
    for old_sunday in previous_sundays:
        if old_sunday != "Date":
            index = old_ops[0].index(old_sunday)
            old_name = old_ops[1][index]
            old_author = old_ops[2][index]
            sheets_store.archive_sheet.append_row(values=[old_sunday, old_name, old_author])

    for i in range(1, 11):
        name = ''
        author = ''
        if next_sundays[i - 1] in old_ops[0]:
            index = old_ops[0].index(next_sundays[i - 1])
            if 0 <= index < len(old_ops[1]):
                name = old_ops[1][index]
            if 0 <= index < len(old_ops[2]):
                author = old_ops[2][index]
        ops.append([next_sundays[i - 1], name, author])

    for i in range(0, 10):
        sheets_store.set_cell_entry(i + 2, 1, ops[i][0])
        sheets_store.set_cell_entry(i + 2, 2, ops[i][1])
        sheets_store.set_cell_entry(i + 2, 3, ops[i][2])

    update_online_sheet()
    update_local_sheet()

    dates = []
    names = []
    authors = []
    for i in range(0, len(ops)):
        dates.append(ops[i][0])
        names.append(ops[i][1])
        authors.append(ops[i][2])

    return [dates, names, authors]


def update_op(datex, opname=None, opauthor=None):
    for i in range(1, len(sheets_store.entire_sheet)):
        if sheets_store.entire_sheet[i][0] == datex:
            if opname is not None:
                sheets_store.entire_sheet[i][1] = opname
            if opauthor is not None:
                sheets_store.entire_sheet[i][2] = opauthor
            break
    update_online_sheet()
    update_local_sheet()
    return None


def delete_op(datex):
    for i in range(1, len(sheets_store.entire_sheet)):
        if sheets_store.entire_sheet[i][0] == datex:
            sheets_store.entire_sheet[i] = [datex, "", ""]
            break
    update_online_sheet()
    update_local_sheet()
    return None


def get_op_data(date=None, op=None, author=None):
    datecolumn = [row[0] for row in sheets_store.entire_sheet]
    opcolumn = [row[1] for row in sheets_store.entire_sheet]
    authorcolumn = [row[2] for row in sheets_store.entire_sheet]

    if date is not None:
        for i in range(1, len(datecolumn)):
            if datecolumn[i] == date:
                return [datecolumn[i], opcolumn[i], authorcolumn[i]]
    elif op is not None:
        for i in range(1, len(opcolumn)):
            if opcolumn[i] == op:
                return [datecolumn[i], opcolumn[i], authorcolumn[i]]
    elif author is not None:
        for i in range(1, len(authorcolumn)):
            if authorcolumn[i] == author:
                return [datecolumn[i], opcolumn[i], authorcolumn[i]]
    return None


def get_full_schedule():
    full_schedule = get_schedule_dates()
    return list(zip(*full_schedule))


def get_free_dates():
    full_schedule = get_full_schedule()
    free_dates = []
    for entry in full_schedule:
        if entry[1] == "" or entry[1] is None:
            free_dates.append([entry[0], entry[1], entry[2]])
    return free_dates


def get_booked_dates():
    full_schedule = get_full_schedule()
    booked_dates = []
    for entry in full_schedule:
        if entry[1] != "" and entry[1] is not None:
            booked_dates.append([entry[0], entry[1], entry[2]])
    return booked_dates


print("Sheets updated and setup")
