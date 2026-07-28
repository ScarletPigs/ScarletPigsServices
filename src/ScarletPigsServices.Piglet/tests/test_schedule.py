from __future__ import annotations

from dataclasses import replace
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
from uuid import uuid4

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import schedule
from scarletpigsapi import AppSetting, Event


class FakeApiClient:
    def __init__(self) -> None:
        self.settings: dict[str, object] = {}
        self.setting_writes: list[str] = []
        self.events: list[Event] = []

    def get_setting(self, key: str) -> AppSetting | None:
        if key not in self.settings:
            return None
        return AppSetting(key=key, value=self.settings[key])  # type: ignore[arg-type]

    def set_setting(self, key: str, value: object) -> AppSetting:
        self.settings[key] = value
        self.setting_writes.append(key)
        return AppSetting(key=key, value=value)  # type: ignore[arg-type]

    def get_events(self) -> list[Event]:
        return list(self.events)

    def create_event(self, **values: object) -> Event:
        event = Event(
            id=uuid4(),
            name=str(values["name"]),
            author=str(values["author"]),
            starts_at=values["starts_at"],  # type: ignore[arg-type]
            type_key=str(values["type_key"]),
            duration_minutes=int(values["duration_minutes"]),
            briefing=values.get("briefing"),  # type: ignore[arg-type]
            external_id=values.get("external_id"),  # type: ignore[arg-type]
            metadata=dict(values.get("metadata") or {}),  # type: ignore[arg-type]
        )
        self.events.append(event)
        return event

    def update_event(self, event_id: object, changes: dict[str, object]) -> Event:
        index = next(
            index
            for index, event in enumerate(self.events)
            if event.id == event_id
        )
        event = self.events[index]
        updated = replace(
            event,
            name=str(changes.get("name", event.name)),
            author=str(changes.get("author", event.author)),
            briefing=changes.get("briefing", event.briefing),  # type: ignore[arg-type]
        )
        self.events[index] = updated
        return updated

    def delete_event(self, event_id: object) -> None:
        self.events = [event for event in self.events if event.id != event_id]


class ScheduleTests(unittest.TestCase):
    def setUp(self) -> None:
        schedule._client = None

    def test_schedule_crud_uses_api_events(self) -> None:
        api = FakeApiClient()
        schedule._client = api  # type: ignore[assignment]
        date = "Aug 02 (26)"

        schedule.update_op(date, "First name", "Alice", author_discord_id=42)
        self.assertEqual(len(api.events), 1)
        self.assertEqual(
            api.events[0].metadata["author_discord_id"],
            "42",
        )

        schedule.update_op(date, "Changed name", "Bob")
        self.assertEqual(len(api.events), 1)
        self.assertEqual(api.events[0].name, "Changed name")
        self.assertEqual(api.events[0].author, "Bob")

        api.settings[schedule.DATE_AMOUNT_KEY] = 1
        with patch.object(
            schedule, "get_next_n_sundays", return_value=[date]
        ):
            self.assertEqual(
                schedule.get_full_schedule(),
                [(date, "Changed name", "Bob")],
            )

        schedule.delete_op(date)
        self.assertEqual(api.events, [])


if __name__ == "__main__":
    unittest.main()
