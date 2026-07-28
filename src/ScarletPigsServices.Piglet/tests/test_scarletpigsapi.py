from __future__ import annotations

from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from scarletpigsapi import ScarletPigsApiClient


class FakeResponse:
    def __init__(self, status_code: int, payload: object = None) -> None:
        self.status_code = status_code
        self.ok = 200 <= status_code < 300
        self._payload = payload
        self.content = b"" if payload is None else b"json"
        self.text = "" if payload is None else str(payload)

    def json(self) -> object:
        return self._payload


class FakeSession:
    def __init__(self, responses: list[FakeResponse]) -> None:
        self.headers: dict[str, str] = {}
        self.responses = responses
        self.requests: list[tuple[str, str, dict[str, object]]] = []

    def request(self, method: str, url: str, **kwargs: object) -> FakeResponse:
        self.requests.append((method, url, kwargs))
        return self.responses.pop(0)


class ApiClientTests(unittest.TestCase):
    def test_normalizes_api_url_and_creates_typed_setting(self) -> None:
        session = FakeSession(
            [
                FakeResponse(404),
                FakeResponse(
                    201,
                    {
                        "key": "piglet.test",
                        "value": {"enabled": True},
                        "updated_at": "2026-07-28T10:00:00Z",
                    },
                ),
            ]
        )
        client = ScarletPigsApiClient(
            "http://localhost:5000/",
            "secret",
            session=session,  # type: ignore[arg-type]
        )

        setting = client.set_setting("piglet.test", {"enabled": True})

        self.assertEqual(setting.key, "piglet.test")
        self.assertEqual(setting.value, {"enabled": True})
        self.assertEqual(
            session.requests[0][1],
            "http://localhost:5000/api/app-settings/piglet.test",
        )
        self.assertEqual(
            session.requests[1][1],
            "http://localhost:5000/api/app-settings",
        )
        self.assertEqual(session.headers["X-API-Key"], "secret")


if __name__ == "__main__":
    unittest.main()
