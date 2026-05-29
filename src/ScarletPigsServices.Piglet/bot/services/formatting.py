from bot.services import schedule_service as schedule


def format_schedule_message_entry(entry: str, entry_type: int):
    lmargin = 1
    match entry_type:
        case 0:
            entry = entry[:-4]
            length = 12
            lmargin = 3
        case 1:
            length = 33
        case 2:
            length = 13

    if entry == "":
        entry = "Free"

    entry = entry.rjust(len(entry) + lmargin)

    if len(entry) > length:  # type: ignore
        diff = len(entry) - (len(entry) - length)  # type: ignore
        diff = diff + 3
        entry = entry[:diff]
        entry += "..."

    if len(entry) < length:  # type: ignore
        entry = entry.ljust(length)  # type: ignore

    return entry


def format_schedule_message():
    formatted_schedule = ""
    this_schedule = schedule.get_full_schedule()
    for booking in this_schedule:
        date = format_schedule_message_entry(booking[0], 0)
        op = format_schedule_message_entry(booking[1], 1)
        author = format_schedule_message_entry(booking[2], 2)
        formatted_schedule += f'{date}|{author}|{op}\n'
        if this_schedule.index(booking) != len(this_schedule) - 1:
            formatted_schedule += "\n"
    return f"```{formatted_schedule}```"


def format_dlc_list(dlclist):
    string = ""
    for i in range(1, len(dlclist)):
        dlc = dlclist[i]
        string += f"{dlc[2]} - {dlc[0]}\n"
    return string