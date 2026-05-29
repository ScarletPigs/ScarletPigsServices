import datetime
import json

import requests
from bot.config import SCARLETPIGS_API_URL

API_URL: str = SCARLETPIGS_API_URL


def get_events():
    response = requests.get(API_URL + '/events', timeout=20)
    list_of_events = json.loads(response.text)
    return list_of_events


def get_event_at_date(datetime_value: datetime.datetime):
    response = requests.get(API_URL + '/events', timeout=20)
    list_of_events = json.loads(response.text)
    for event in list_of_events:
        if (datetime.datetime.fromisoformat(event['startTime']) <= datetime_value and datetime.datetime.fromisoformat(event['endTime']) >= datetime_value):
            return event
    return None


def create_event(name: str, description: str, author: str, authorid: int, starttime: datetime.datetime, endtime: datetime.datetime):
    if len(description) > 150:
        description = description[:147] + "..."
    event = {
        "name": name,
        "CreatorDiscordUsername": f"{authorid}",
        "author": author,
        "Description": description,
        "startTime": starttime.isoformat(),
        "endTime": endtime.isoformat()
    }
    response = requests.post(API_URL + '/events', json=event, timeout=20)
    print(response.status_code)
    return response.json()


def get_event(event_id: int):
    response = requests.get(API_URL + '/events/' + str(event_id), timeout=20)
    return response.json()


def edit_event(edited_event: dict):
    requests.put(API_URL + '/events/', json=edited_event, timeout=20)


def delete_event(event_id: int):
    response = requests.delete(API_URL + '/events/' + str(event_id), timeout=20)
    return response.ok