import requests
import os
import datetime as dt
from typing import Any, Dict, List, Optional

# Define the URL
API_URL: str = os.getenv('SCARLETPIGS_API') or ''


def get_events() -> List[Dict[str, Any]]:
    response = requests.get(API_URL + '/events', timeout=20)
    response.raise_for_status()
    return response.json()


def get_events_between(from_date: dt.datetime, to_date: dt.datetime) -> List[Dict[str, Any]]:
    params = {
        "fromDate": from_date.isoformat(),
        "toDate": to_date.isoformat(),
    }
    response = requests.get(API_URL + '/events/fromto',
                            params=params, timeout=20)
    response.raise_for_status()
    return response.json()


def get_event_at_date(target: dt.datetime) -> Optional[Dict[str, Any]]:
    response = requests.get(API_URL + '/events', timeout=20)
    response.raise_for_status()
    list_of_events = response.json()
    for event in list_of_events:
        try:
            start_s = event.get('startTime') or event.get('StartTime')
            end_s = event.get('endTime') or event.get('EndTime')
            if not start_s or not end_s:
                continue
            start = dt.datetime.fromisoformat(start_s.replace('Z', '+00:00'))
            end = dt.datetime.fromisoformat(end_s.replace('Z', '+00:00'))
            if start <= target <= end:
                return event
        except Exception:
            continue
    return None


def create_event(name: str, description: str, author: str, authorid: int, starttime: dt.datetime, endtime: dt.datetime):
    # Shorten description if it's too long
    if len(description) > 150:
        description = description[:147] + "..."
    # Make the request
    event = {
        "name": name,
        "creatorDiscordUsername": f"{authorid}",
        "author": author,
        "description": description,
        "startTime": starttime.isoformat(),
        "endTime": endtime.isoformat(),
    }
    response = requests.post(API_URL + '/events', json=event, timeout=20)
    response.raise_for_status()
    return response.json()


def get_event(event_id: int) -> Dict[str, Any]:
    response = requests.get(API_URL + '/events/' + str(event_id), timeout=20)
    response.raise_for_status()
    return response.json()


# def edit_event(id: int, name: str | None, description: str | None, starttime: datetime.datetime | None, endtime: datetime.datetime | None):
#     edited_event = {
#         "id": id,
#         "name": name,
#         "description": description,
#         "startTime": starttime.isoformat() if starttime else None,
#         "endTime": endtime.isoformat() if endtime else None
#     }
#     response = requests.put(API_URL + '/Events/', json=edited_event)


def edit_event(edited_event: Dict[str, Any]) -> None:
    response = requests.put(API_URL + '/events', json=edited_event, timeout=20)
    response.raise_for_status()


def delete_event(event_id: int) -> bool:
    response = requests.delete(
        API_URL + '/events/' + str(event_id), timeout=20)
    return response.ok
