# SPDX-License-Identifier: MIT
"""PR-fast acceptance tests for the public CI status CLI.

Only the external HTTP transport and wall clock are replaced. Checked-in JSON
fixtures supply GitHub's response shapes; no test performs a network request.
"""

from __future__ import annotations

import io
import json
import unittest
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import parse_qs, urlsplit

from tools import ci_status

FIXTURE = Path(__file__).parent / "fixtures" / "ci_status" / "responses.json"


@dataclass(frozen=True)
class FakeResponse:
    status: int
    body: bytes
    headers: dict[str, str]
    reason: str = ""


class FakeTransport:
    """Serve fixture responses at the real HTTP edge and retain request metadata."""

    def __init__(self, responses: dict[str, object]) -> None:
        self.responses = {
            key: list(value) if isinstance(value, list) else [value]
            for key, value in responses.items()
        }
        self.requests: list[tuple[str, dict[str, str], dict[str, list[str]]]] = []

    def __call__(
        self,
        url: str,
        headers: dict[str, str],
    ) -> FakeResponse:
        parsed = urlsplit(url)
        key = f"GET {parsed.path}"
        if key not in self.responses or not self.responses[key]:
            raise AssertionError(f"unexpected HTTP request: {key}")
        self.requests.append((url, dict(headers), parse_qs(parsed.query)))
        fixture = self.responses[key].pop(0)
        assert isinstance(fixture, dict)
        raw_body = fixture.get("raw_body")
        body = (
            raw_body.encode("utf-8")
            if isinstance(raw_body, str)
            else json.dumps(fixture["body"]).encode("utf-8")
        )
        return FakeResponse(
            status=int(fixture["status"]),
            body=body,
            headers=dict(fixture.get("headers", {})),
            reason=str(fixture.get("reason", "")),
        )


class FakeClock:
    def __init__(self) -> None:
        self.now = 0.0
        self.sleeps: list[float] = []

    def __call__(self) -> float:
        return self.now

    def sleep(self, seconds: float) -> None:
        self.sleeps.append(seconds)
        self.now += seconds


class CiStatusTests(unittest.TestCase):
    def setUp(self) -> None:
        self.fixtures = json.loads(FIXTURE.read_text(encoding="utf-8"))

    def invoke(
        self,
        arguments: list[str],
        transport: FakeTransport,
        *,
        environ: dict[str, str] | None = None,
        clock=None,
        sleep=None,
    ) -> tuple[int, str, str]:
        stdout = io.StringIO()
        stderr = io.StringIO()
        code = ci_status.main(
            arguments,
            transport=transport,
            environ={} if environ is None else environ,
            stdout=stdout,
            stderr=stderr,
            clock=clock,
            sleep=sleep,
        )
        return code, stdout.getvalue(), stderr.getvalue()

    def test_run_id_prints_run_and_job_fields(self) -> None:
        transport = FakeTransport(self.fixtures)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "101"],
            transport,
        )

        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("Run 101: CI", stdout)
        self.assertIn("Event: workflow_dispatch", stdout)
        self.assertIn("Status: completed", stdout)
        self.assertIn("Conclusion: success", stdout)
        self.assertIn("Head SHA: 1111111111111111111111111111111111111111", stdout)
        self.assertIn("Detect changes: completed / success", stdout)
        self.assertIn("Contracts: completed / success", stdout)

    def test_sha_selects_ci_runs_for_the_requested_head(self) -> None:
        transport = FakeTransport(self.fixtures)
        sha = "1111111111111111111111111111111111111111"
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--sha", sha],
            transport,
        )

        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("Run 101: CI", stdout)
        self.assertNotIn("Documentation", stdout)
        request_url, _, query = transport.requests[0]
        self.assertIn("/repos/acme/mocassin/actions/runs?", request_url)
        self.assertEqual(query["head_sha"], [sha])

    def test_abbreviated_sha_is_resolved_before_run_selection(self) -> None:
        transport = FakeTransport(self.fixtures)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--sha", "1111111"],
            transport,
        )

        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("Run 101: CI", stdout)
        self.assertEqual(
            transport.requests[0][0],
            "https://api.github.com/repos/acme/mocassin/commits/1111111",
        )
        self.assertEqual(
            transport.requests[1][2]["head_sha"],
            ["1111111111111111111111111111111111111111"],
        )

    def test_invalid_sha_exits_two_without_an_http_request(self) -> None:
        transport = FakeTransport(self.fixtures)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--sha", "not-a-sha"],
            transport,
        )
        self.assertEqual(code, 2)
        self.assertEqual(stdout, "")
        self.assertIn("full or abbreviated hexadecimal commit SHA", stderr)
        self.assertEqual(transport.requests, [])

    def test_json_output_contains_runs_and_jobs(self) -> None:
        transport = FakeTransport(self.fixtures)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "101", "--json"],
            transport,
        )

        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertEqual(
            json.loads(stdout),
            {
                "repository": "acme/mocassin",
                "runs": [
                    {
                        "id": 101,
                        "workflow_name": "CI",
                        "event": "workflow_dispatch",
                        "status": "completed",
                        "conclusion": "success",
                        "head_sha": "1111111111111111111111111111111111111111",
                        "jobs": [
                            {
                                "name": "Detect changes",
                                "status": "completed",
                                "conclusion": "success",
                            },
                            {
                                "name": "Contracts",
                                "status": "completed",
                                "conclusion": "success",
                            },
                        ],
                    }
                ],
                "timed_out": False,
            },
        )

    def test_failed_and_cancelled_runs_exit_one(self) -> None:
        for run_id, conclusion in ((102, "failure"), (104, "cancelled")):
            with self.subTest(run_id=run_id):
                transport = FakeTransport(self.fixtures)
                code, stdout, stderr = self.invoke(
                    ["--repo", "acme/mocassin", "--run-id", str(run_id)],
                    transport,
                )
                self.assertEqual(code, 1)
                self.assertEqual(stderr, "")
                self.assertIn(f"Conclusion: {conclusion}", stdout)

    def test_in_progress_run_exits_three_without_wait(self) -> None:
        transport = FakeTransport(self.fixtures)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "103"],
            transport,
        )
        self.assertEqual(code, 3)
        self.assertEqual(stderr, "")
        self.assertIn("Status: in_progress", stdout)
        self.assertIn("Conclusion: pending", stdout)
        self.assertIn("Detect changes: in_progress / pending", stdout)

    def test_wait_polls_until_the_run_completes(self) -> None:
        run_key = "GET /repos/acme/mocassin/actions/runs/103"
        jobs_key = f"{run_key}/jobs"
        responses = dict(self.fixtures)
        responses[run_key] = [self.fixtures[run_key], self.fixtures["completed-103"]]
        responses[jobs_key] = [
            self.fixtures[jobs_key],
            self.fixtures["GET /repos/acme/mocassin/actions/runs/101/jobs"],
        ]
        transport = FakeTransport(responses)
        clock = FakeClock()

        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "103", "--wait", "30"],
            transport,
            clock=clock,
            sleep=clock.sleep,
        )

        self.assertEqual(code, 0)
        self.assertIn("Conclusion: success", stdout)
        self.assertIn("Run 103: in_progress", stderr)
        self.assertIn("Waiting", stderr)
        self.assertEqual(clock.sleeps, [15.0])

    def test_wait_timeout_exits_one_and_marks_json(self) -> None:
        run_key = "GET /repos/acme/mocassin/actions/runs/103"
        jobs_key = f"{run_key}/jobs"
        responses = dict(self.fixtures)
        responses[run_key] = [self.fixtures[run_key], self.fixtures[run_key]]
        responses[jobs_key] = [self.fixtures[jobs_key], self.fixtures[jobs_key]]
        transport = FakeTransport(responses)
        clock = FakeClock()

        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "103", "--wait", "1", "--json"],
            transport,
            clock=clock,
            sleep=clock.sleep,
        )

        self.assertEqual(code, 1)
        self.assertTrue(json.loads(stdout)["timed_out"])
        self.assertIn("Timed out after 1 seconds", stderr)
        self.assertEqual(clock.sleeps, [1.0])

    def test_rate_limit_names_optional_gh_token_remedy(self) -> None:
        run_key = "GET /repos/acme/mocassin/actions/runs/101"
        for status in (403, 429):
            with self.subTest(status=status):
                responses = dict(self.fixtures)
                responses[run_key] = self.fixtures[f"rate-limit-{status}"]
                transport = FakeTransport(responses)
                code, stdout, stderr = self.invoke(
                    ["--repo", "acme/mocassin", "--run-id", "101"],
                    transport,
                )
                self.assertEqual(code, 2)
                self.assertEqual(stdout, "")
                self.assertIn(f"HTTP {status}", stderr)
                self.assertIn("GH_TOKEN", stderr)
                self.assertIn("rate limit", stderr)

    def test_gh_token_is_optional_and_never_printed(self) -> None:
        token = "test-only-token-value"
        run_key = "GET /repos/acme/mocassin/actions/runs/101"
        responses = dict(self.fixtures)
        response = dict(self.fixtures[run_key])
        body = dict(response["body"])
        body["name"] = token
        response["body"] = body
        responses[run_key] = response
        transport = FakeTransport(responses)

        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "101"],
            transport,
            environ={"GH_TOKEN": token},
        )

        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertNotIn(token, stdout)
        self.assertIn("[redacted]", stdout)
        self.assertEqual(transport.requests[0][1]["Authorization"], f"Bearer {token}")

        json_transport = FakeTransport(responses)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "101", "--json"],
            json_transport,
            environ={"GH_TOKEN": token},
        )
        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertNotIn(token, stdout)
        self.assertEqual(json.loads(stdout)["runs"][0]["workflow_name"], "[redacted]")

        unauthenticated = FakeTransport(self.fixtures)
        code, _, _ = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "101"],
            unauthenticated,
        )
        self.assertEqual(code, 0)
        self.assertNotIn("Authorization", unauthenticated.requests[0][1])

    def test_invalid_json_and_malformed_run_exit_two(self) -> None:
        run_key = "GET /repos/acme/mocassin/actions/runs/101"
        for fixture_name, message in (
            ("invalid-json", "invalid JSON"),
            ("malformed-run", "workflow name"),
        ):
            with self.subTest(fixture=fixture_name):
                responses = dict(self.fixtures)
                responses[run_key] = self.fixtures[fixture_name]
                code, stdout, stderr = self.invoke(
                    ["--repo", "acme/mocassin", "--run-id", "101"],
                    FakeTransport(responses),
                )
                self.assertEqual(code, 2)
                self.assertEqual(stdout, "")
                self.assertIn(message, stderr)

    def test_no_matching_ci_run_exits_two(self) -> None:
        responses = dict(self.fixtures)
        responses["GET /repos/acme/mocassin/actions/runs"] = self.fixtures["no-ci-runs"]
        code, stdout, stderr = self.invoke(
            [
                "--repo",
                "acme/mocassin",
                "--sha",
                "1111111111111111111111111111111111111111",
            ],
            FakeTransport(responses),
        )
        self.assertEqual(code, 2)
        self.assertEqual(stdout, "")
        self.assertIn("No CI workflow runs found", stderr)

    def test_repository_is_derived_from_the_real_origin_remote(self) -> None:
        responses = {
            key.replace("/repos/acme/mocassin", "/repos/seb-eis/mocassin-2"): value
            for key, value in self.fixtures.items()
        }
        transport = FakeTransport(responses)
        code, stdout, stderr = self.invoke(["--run-id", "101"], transport)
        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("Run 101: CI", stdout)
        self.assertIn(
            "/repos/seb-eis/mocassin-2/actions/runs/101", transport.requests[0][0]
        )

    def test_job_pagination_includes_every_job(self) -> None:
        jobs_key = "GET /repos/acme/mocassin/actions/runs/101/jobs"
        base_job = self.fixtures[jobs_key]["body"]["jobs"][0]
        first_page = [dict(base_job, name=f"Job {number}") for number in range(100)]
        last_page = [dict(base_job, name="Final job")]
        responses = dict(self.fixtures)
        responses[jobs_key] = [
            {"status": 200, "body": {"total_count": 101, "jobs": first_page}},
            {"status": 200, "body": {"total_count": 101, "jobs": last_page}},
        ]
        transport = FakeTransport(responses)
        code, stdout, stderr = self.invoke(
            ["--repo", "acme/mocassin", "--run-id", "101"],
            transport,
        )
        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("Job 99: completed / success", stdout)
        self.assertIn("Final job: completed / success", stdout)
        pages = [
            request[2]["page"]
            for request in transport.requests
            if request[0].startswith(
                "https://api.github.com/repos/acme/mocassin/actions/runs/101/jobs?"
            )
        ]
        self.assertEqual(pages, [["1"], ["2"]])

    def test_sha_run_pagination_skips_unrelated_workflows(self) -> None:
        runs_key = "GET /repos/acme/mocassin/actions/runs"
        unrelated = [
            {
                "id": 1000 + number,
                "name": "Documentation",
                "event": "workflow_dispatch",
                "status": "completed",
                "conclusion": "failure",
                "head_sha": "1111111111111111111111111111111111111111",
            }
            for number in range(100)
        ]
        ci_run = self.fixtures[runs_key]["body"]["workflow_runs"][0]
        responses = dict(self.fixtures)
        responses[runs_key] = [
            {"status": 200, "body": {"total_count": 101, "workflow_runs": unrelated}},
            {"status": 200, "body": {"total_count": 101, "workflow_runs": [ci_run]}},
        ]
        transport = FakeTransport(responses)
        code, stdout, stderr = self.invoke(
            [
                "--repo",
                "acme/mocassin",
                "--sha",
                "1111111111111111111111111111111111111111",
            ],
            transport,
        )
        self.assertEqual(code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("Run 101: CI", stdout)
        self.assertNotIn("Documentation", stdout)
        list_requests = [
            request
            for request in transport.requests
            if request[0].startswith(
                "https://api.github.com/repos/acme/mocassin/actions/runs?"
            )
        ]
        self.assertEqual(
            [request[2]["page"] for request in list_requests], [["1"], ["2"]]
        )


if __name__ == "__main__":
    unittest.main()
