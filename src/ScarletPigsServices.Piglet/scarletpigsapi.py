"""Typed client for the Scarlet Pigs API."""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
import os
from typing import Any, Mapping
from urllib.parse import quote
from uuid import UUID

import requests


JsonValue = (
    None
    | bool
    | int
    | float
    | str
    | list["JsonValue"]
    | dict[str, "JsonValue"]
)


class ApiError(RuntimeError):
    """Raised when the Scarlet Pigs API returns an unsuccessful response."""

    def __init__(self, method: str, url: str, status_code: int, detail: str):
        super().__init__(f"{method} {url} returned {status_code}: {detail}")
        self.status_code = status_code


@dataclass(frozen=True, slots=True)
class AppSetting:
    key: str
    value: JsonValue
    updated_at: datetime | None = None

    @classmethod
    def from_json(cls, data: Mapping[str, Any]) -> "AppSetting":
        return cls(
            key=str(data["key"]),
            value=data.get("value"),
            updated_at=_optional_datetime(data.get("updated_at")),
        )


@dataclass(frozen=True, slots=True)
class Event:
    id: UUID
    name: str
    starts_at: datetime
    type_key: str
    author: str = ""
    briefing: str | None = None
    duration_minutes: int = 0
    external_id: str | None = None
    status: str = ""
    metadata: dict[str, JsonValue] = field(default_factory=dict)

    @property
    def ends_at(self) -> datetime:
        return self.starts_at + timedelta(minutes=self.duration_minutes)

    @classmethod
    def from_json(cls, data: Mapping[str, Any]) -> "Event":
        metadata = data.get("metadata")
        return cls(
            id=UUID(str(data["id"])),
            name=str(data["name"]),
            starts_at=_required_datetime(data["starts_at"]),
            type_key=str(data["type_key"]),
            author=str(data.get("author") or ""),
            briefing=_optional_string(data.get("briefing")),
            duration_minutes=int(data.get("duration_minutes") or 0),
            external_id=_optional_string(data.get("external_id")),
            status=str(data.get("status") or ""),
            metadata=dict(metadata) if isinstance(metadata, Mapping) else {},
        )


def _optional_string(value: Any) -> str | None:
    return None if value is None else str(value)


def _required_datetime(value: Any) -> datetime:
    if not isinstance(value, str):
        raise TypeError(f"Expected an ISO datetime string, got {type(value).__name__}.")
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def _optional_datetime(value: Any) -> datetime | None:
    return None if value is None else _required_datetime(value)


def _api_base_url(endpoint: str) -> str:
    endpoint = endpoint.rstrip("/")
    return endpoint if endpoint.endswith("/api") else f"{endpoint}/api"


class ScarletPigsApiClient:
    """Synchronous, typed API client used by the Discord bot."""

    def __init__(
        self,
        endpoint: str,
        api_key: str,
        *,
        session: requests.Session | None = None,
        timeout_seconds: float = 20,
    ):
        if not endpoint or endpoint == "None":
            raise ValueError("SCARLETPIGS_API is not configured.")
        if not api_key:
            raise ValueError("SCARLETPIGS_API_KEY is not configured.")

        self.base_url = _api_base_url(endpoint)
        self.timeout_seconds = timeout_seconds
        self._session = session or requests.Session()
        self._session.headers.update(
            {"Accept": "application/json", "X-API-Key": api_key}
        )

    @classmethod
    def from_environment(cls) -> "ScarletPigsApiClient":
        return cls(
            os.getenv("SCARLETPIGS_API", ""),
            os.getenv("SCARLETPIGS_API_KEY", ""),
        )

    def _request(
        self,
        method: str,
        path: str,
        *,
        json_body: Mapping[str, Any] | None = None,
        params: Mapping[str, Any] | None = None,
        allow_not_found: bool = False,
    ) -> Any:
        url = f"{self.base_url}/{path.lstrip('/')}"
        response = self._session.request(
            method,
            url,
            json=json_body,
            params=params,
            timeout=self.timeout_seconds,
        )
        if allow_not_found and response.status_code == 404:
            return None
        if not response.ok:
            try:
                detail = response.json()
            except ValueError:
                detail = response.text
            raise ApiError(method, url, response.status_code, str(detail))
        if response.status_code == 204 or not response.content:
            return None
        return response.json()

    def get_setting(self, key: str) -> AppSetting | None:
        data = self._request(
            "GET", f"app-settings/{quote(key, safe='')}", allow_not_found=True
        )
        return None if data is None else AppSetting.from_json(data)

    def set_setting(self, key: str, value: JsonValue) -> AppSetting:
        now = datetime.now(timezone.utc).isoformat()
        existing = self.get_setting(key)
        if existing is None:
            data = self._request(
                "POST",
                "app-settings",
                json_body={"key": key, "value": value, "updated_at": now},
            )
        else:
            data = self._request(
                "PATCH",
                f"app-settings/{quote(key, safe='')}",
                json_body={"value": value, "updated_at": now},
            )
        return AppSetting.from_json(data)

    def get_events(self) -> list[Event]:
        events: list[Event] = []
        offset = 0
        page_size = 1000
        while True:
            page = self._request(
                "GET", "events", params={"offset": offset, "limit": page_size}
            )
            events.extend(Event.from_json(item) for item in page)
            if len(page) < page_size:
                return events
            offset += page_size

    def get_event(self, event_id: UUID | str) -> Event | None:
        data = self._request(
            "GET", f"events/{event_id}", allow_not_found=True
        )
        return None if data is None else Event.from_json(data)

    def get_event_at(self, instant: datetime, *, type_key: str | None = None) -> Event | None:
        instant = _as_aware(instant)
        for event in self.get_events():
            if type_key is not None and event.type_key != type_key:
                continue
            if event.starts_at <= instant <= event.ends_at:
                return event
        return None

    def find_event_by_external_id(self, external_id: str) -> Event | None:
        return next(
            (
                event
                for event in self.get_events()
                if event.external_id == external_id
            ),
            None,
        )

    def create_event(
        self,
        *,
        name: str,
        author: str,
        starts_at: datetime,
        type_key: str,
        duration_minutes: int = 180,
        briefing: str | None = None,
        external_id: str | None = None,
        status: str = "scheduled",
        metadata: Mapping[str, JsonValue] | None = None,
    ) -> Event:
        data = self._request(
            "POST",
            "events",
            json_body={
                "name": name,
                "author": author,
                "starts_at": _as_aware(starts_at).isoformat(),
                "type_key": type_key,
                "duration_minutes": duration_minutes,
                "briefing": briefing,
                "external_id": external_id,
                "status": status,
                "metadata": dict(metadata or {}),
            },
        )
        return Event.from_json(data)

    def update_event(
        self, event_id: UUID | str, changes: Mapping[str, Any]
    ) -> Event:
        payload = dict(changes)
        starts_at = payload.get("starts_at")
        if isinstance(starts_at, datetime):
            payload["starts_at"] = _as_aware(starts_at).isoformat()
        data = self._request(
            "PATCH", f"events/{event_id}", json_body=payload
        )
        return Event.from_json(data)

    def delete_event(self, event_id: UUID | str) -> None:
        self._request("DELETE", f"events/{event_id}")

    def ensure_event_type(
        self,
        type_key: str,
        *,
        capability_key: str = "manage_events",
    ) -> None:
        event_type = self._request(
            "GET",
            f"event-types/{quote(type_key, safe='')}",
            allow_not_found=True,
        )
        if event_type is not None:
            return

        capability = self._request(
            "GET",
            f"capabilities/{quote(capability_key, safe='')}",
            allow_not_found=True,
        )
        if capability is None:
            self._request(
                "POST",
                "capabilities",
                json_body={
                    "key": capability_key,
                    "label": "Manage events",
                    "description": "Create and maintain scheduled events.",
                    "sort_order": 0,
                },
            )

        self._request(
            "POST",
            "event-types",
            json_body={
                "key": type_key,
                "capability_key": capability_key,
                "label": "Operation",
                "color": "",
                "fixed_duration_minutes": 180,
                "fixed_start_minutes": 900,
                "force_unlimited_slots": True,
                "sort_order": 0,
            },
        )


def _as_aware(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.astimezone()
    return value


_default_client: ScarletPigsApiClient | None = None


def get_client() -> ScarletPigsApiClient:
    global _default_client
    if _default_client is None:
        _default_client = ScarletPigsApiClient.from_environment()
    return _default_client
