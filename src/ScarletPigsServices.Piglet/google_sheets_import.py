"""One-time reader for the legacy Piglet Google Sheets workbook."""

from __future__ import annotations

from dataclasses import dataclass
import json
import os
from typing import Any

import gspread


@dataclass(frozen=True, slots=True)
class LegacyOperation:
    date: str
    name: str
    author: str
    source: str


@dataclass(frozen=True, slots=True)
class LegacyGoogleSheetsData:
    date_amount: int
    schedule_messages: dict[str, Any]
    modlist_messages: dict[str, Any]
    questionnaire_message: dict[str, Any] | None
    questionnaire_info: list[list[str]]
    operations: list[LegacyOperation]


def read_legacy_google_sheets() -> LegacyGoogleSheetsData:
    """Read every piece of state that Piglet previously kept in Sheets."""

    workbook_name = _required_environment("GOOGLE_SHEET_NAME")
    credentials = {
        "type": _required_environment("TYPE"),
        "project_id": _required_environment("PROJECT_ID"),
        "private_key_id": _required_environment("PRIVATE_KEY_ID"),
        "private_key": _required_environment("PRIVATE_KEY").replace("\\n", "\n"),
        "client_email": _required_environment("CLIENT_EMAIL"),
        "client_id": _required_environment("CLIENT_ID"),
        "auth_uri": _required_environment("AUTH_URI"),
        "token_uri": _required_environment("TOKEN_URI"),
        "auth_provider_x509_cert_url": _required_environment(
            "AUTH_PROVIDER_X509_CERT_URL"
        ),
        "client_x509_cert_url": _required_environment("CLIENT_X509_CERT_URL"),
    }
    client = gspread.service_account_from_dict(credentials)
    worksheets = client.open(workbook_name).worksheets()
    if len(worksheets) < 3:
        raise RuntimeError(
            "The legacy Google workbook must contain schedule, archive, and DLC worksheets."
        )

    schedule_rows = worksheets[0].get_all_values()
    archive_rows = worksheets[1].get_all_values()
    questionnaire_rows = worksheets[2].get_all_values()

    return LegacyGoogleSheetsData(
        date_amount=int(_cell(schedule_rows, 2, 7) or "10"),
        schedule_messages=_json_object(
            _cell(schedule_rows, 3, 7), {"servers": []}
        ),
        modlist_messages=_json_object(
            _cell(schedule_rows, 4, 7), {"servers": []}
        ),
        questionnaire_message=_optional_json_object(
            _cell(schedule_rows, 5, 7)
        ),
        questionnaire_info=questionnaire_rows,
        operations=[
            *_operations(schedule_rows, "schedule"),
            *_operations(archive_rows, "archive"),
        ],
    )


def _required_environment(name: str) -> str:
    value = os.getenv(name)
    if not value:
        raise ValueError(
            f"{name} is required until the legacy Google Sheets import completes."
        )
    return value


def _cell(rows: list[list[str]], row: int, column: int) -> str:
    try:
        return rows[row - 1][column - 1]
    except IndexError:
        return ""


def _json_object(value: str, default: dict[str, Any]) -> dict[str, Any]:
    if not value:
        return default
    parsed = json.loads(value)
    if not isinstance(parsed, dict):
        raise ValueError("Expected a JSON object in the legacy settings cell.")
    return parsed


def _optional_json_object(value: str) -> dict[str, Any] | None:
    return None if not value else _json_object(value, {})


def _operations(rows: list[list[str]], source: str) -> list[LegacyOperation]:
    operations: list[LegacyOperation] = []
    for row in rows:
        date = _column(row, 0).strip()
        name = _column(row, 1).strip()
        author = _column(row, 2).strip()
        if not date or date.casefold() == "date" or not name:
            continue
        operations.append(
            LegacyOperation(
                date=date,
                name=name,
                author=author,
                source=source,
            )
        )
    return operations


def _column(row: list[str], index: int) -> str:
    return row[index] if index < len(row) else ""
