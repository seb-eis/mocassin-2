#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Read public GitHub Actions CI status without discovering git credentials.

The only optional credential is GH_TOKEN from the process environment. It is
used in the request header, never included in reports, and redacted from errors.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import subprocess
import sys
import time
from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass
from typing import Protocol
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, urlsplit
from urllib.request import Request, urlopen

DEFAULT_API_BASE = "https://api.github.com"
DEFAULT_WAIT_SECONDS = 900
POLL_INTERVAL_SECONDS = 15
REQUEST_TIMEOUT_SECONDS = 30
REPOSITORY_RE = re.compile(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+")
FULL_SHA_RE = re.compile(r"[0-9a-fA-F]{40}")
ABBREVIATED_SHA_RE = re.compile(r"[0-9a-fA-F]{4,39}")


class StatusError(Exception):
    """An expected usage, transport, HTTP, or response error."""

    def __init__(self, message: str, exit_code: int = 2) -> None:
        super().__init__(message)
        self.exit_code = exit_code


@dataclass(frozen=True)
class HttpResponse:
    status: int
    body: bytes
    headers: Mapping[str, str]
    reason: str = ""


class Transport(Protocol):
    def __call__(self, url: str, headers: Mapping[str, str]) -> HttpResponse: ...


@dataclass(frozen=True)
class Job:
    name: str
    status: str
    conclusion: str | None


@dataclass(frozen=True)
class Run:
    id: int
    workflow_name: str
    event: str
    status: str
    conclusion: str | None
    head_sha: str
    jobs: tuple[Job, ...]


def _text(value: object, field: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise StatusError(f"GitHub response is missing a valid '{field}' field")
    return value


def _integer(value: object, field: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < 0:
        raise StatusError(f"GitHub response is missing a valid '{field}' field")
    return value


def _conclusion(value: object) -> str | None:
    if value is None:
        return None
    return _text(value, "conclusion")


def parse_job(value: object) -> Job:
    if not isinstance(value, dict):
        raise StatusError("GitHub returned an invalid job object")
    return Job(
        name=_text(value.get("name"), "job name"),
        status=_text(value.get("status"), "job status"),
        conclusion=_conclusion(value.get("conclusion")),
    )


def parse_run(value: object, jobs: Sequence[Job]) -> Run:
    if not isinstance(value, dict):
        raise StatusError("GitHub returned an invalid workflow run object")
    run_id = _integer(value.get("id"), "run id")
    if run_id == 0:
        raise StatusError("GitHub returned an invalid run id")
    return Run(
        id=run_id,
        workflow_name=_text(value.get("name"), "workflow name"),
        event=_text(value.get("event"), "event"),
        status=_text(value.get("status"), "status"),
        conclusion=_conclusion(value.get("conclusion")),
        head_sha=_text(value.get("head_sha"), "head_sha"),
        jobs=tuple(jobs),
    )


def urllib_transport(url: str, headers: Mapping[str, str]) -> HttpResponse:
    request = Request(url, headers=dict(headers), method="GET")
    try:
        with urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:
            return HttpResponse(
                status=response.status,
                body=response.read(),
                headers=dict(response.headers.items()),
                reason=getattr(response, "reason", ""),
            )
    except HTTPError as error:
        response = HttpResponse(
            status=error.code,
            body=b"",
            headers=dict(error.headers.items()) if error.headers else {},
            reason=str(error.reason),
        )
        error.close()
        return response
    except (URLError, OSError) as error:
        reason = error.reason if isinstance(error, URLError) else error
        raise StatusError(f"GitHub API request failed: {reason}") from error


class GitHubClient:
    def __init__(
        self,
        repository: str,
        api_base: str,
        token: str,
        transport: Transport,
    ) -> None:
        self.repository = repository
        self.api_base = api_base.rstrip("/")
        self.headers = {
            "Accept": "application/vnd.github+json",
            "User-Agent": "mocassin-ci-status",
            "X-GitHub-Api-Version": "2022-11-28",
        }
        if token:
            self.headers["Authorization"] = f"Bearer {token}"
        self.transport = transport

    def get_json(
        self, path: str, query: Mapping[str, object] | None = None
    ) -> dict[str, object]:
        url = f"{self.api_base}{path}"
        if query:
            url = f"{url}?{urlencode(query)}"
        try:
            response = self.transport(url, self.headers)
        except StatusError:
            raise
        except (OSError, ValueError) as error:
            raise StatusError(f"GitHub API request failed: {error}") from error

        if response.status in (403, 429):
            raise StatusError(
                f"GitHub API rate limit or access limit reached (HTTP {response.status}). "
                "For public repositories, retry later or set GH_TOKEN for a higher rate limit."
            )
        if not 200 <= response.status < 300:
            reason = f": {response.reason}" if response.reason else ""
            raise StatusError(
                f"GitHub API request failed (HTTP {response.status}){reason}"
            )
        try:
            payload = json.loads(response.body.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError) as error:
            raise StatusError("GitHub API returned invalid JSON") from error
        if not isinstance(payload, dict):
            raise StatusError("GitHub API returned an unexpected JSON value")
        return payload

    def _jobs_page(self, run_id: int, page: int) -> dict[str, object]:
        return self.get_json(
            f"/repos/{self.repository}/actions/runs/{run_id}/jobs",
            {"per_page": 100, "page": page},
        )

    def jobs(self, run_id: int) -> tuple[Job, ...]:
        jobs: list[Job] = []
        page = 1
        while True:
            payload = self._jobs_page(run_id, page)
            values = payload.get("jobs")
            if not isinstance(values, list):
                raise StatusError("GitHub response is missing a valid 'jobs' list")
            total = _integer(payload.get("total_count"), "total_count")
            jobs.extend(parse_job(value) for value in values)
            if len(values) < 100 or len(jobs) >= total:
                return tuple(jobs)
            page += 1

    def run(self, run_id: int) -> Run:
        payload = self.get_json(f"/repos/{self.repository}/actions/runs/{run_id}")
        return parse_run(payload, self.jobs(run_id))

    def resolve_head_sha(self, sha: str) -> str:
        if FULL_SHA_RE.fullmatch(sha):
            return sha.lower()
        if ABBREVIATED_SHA_RE.fullmatch(sha):
            payload = self.get_json(f"/repos/{self.repository}/commits/{sha}")
            resolved = _text(payload.get("sha"), "commit sha")
            if not FULL_SHA_RE.fullmatch(resolved):
                raise StatusError("GitHub returned an invalid resolved commit SHA")
            return resolved.lower()
        raise StatusError("--sha must be a full or abbreviated hexadecimal commit SHA")

    def ci_run_ids(self, sha: str) -> tuple[int, ...]:
        run_ids: list[int] = []
        seen = 0
        page = 1
        while True:
            payload = self.get_json(
                f"/repos/{self.repository}/actions/runs",
                {"head_sha": sha, "per_page": 100, "page": page},
            )
            values = payload.get("workflow_runs")
            if not isinstance(values, list):
                raise StatusError(
                    "GitHub response is missing a valid 'workflow_runs' list"
                )
            total = _integer(payload.get("total_count"), "total_count")
            seen += len(values)
            for value in values:
                run = parse_run(value, ())
                if run.workflow_name.casefold() == "ci":
                    run_ids.append(run.id)
            if len(values) < 100 or seen >= total:
                break
            page += 1
        if not run_ids:
            raise StatusError(f"No CI workflow runs found for head SHA {sha}")
        return tuple(run_ids)


def validate_sha(value: str) -> str:
    sha = value.strip()
    if not sha or any(character.isspace() for character in sha):
        raise StatusError("--sha must be a non-empty commit SHA without whitespace")
    return sha


def validate_repository(value: str) -> str:
    repository = (
        value.strip()
        .removeprefix("https://github.com/")
        .removeprefix("http://github.com/")
    )
    repository = repository.strip("/").removesuffix(".git")
    if not REPOSITORY_RE.fullmatch(repository):
        raise StatusError("Repository must use the form owner/name")
    return repository


def repository_from_remote(remote: str) -> str:
    remote = remote.strip()
    if remote.startswith("git@github.com:"):
        return validate_repository(remote.removeprefix("git@github.com:"))
    parsed = urlsplit(remote)
    if parsed.scheme in ("https", "ssh", "git") and parsed.hostname == "github.com":
        return validate_repository(parsed.path)
    raise StatusError("origin is not a GitHub repository; use --repo owner/name")


def resolve_repository(override: str | None) -> str:
    if override is not None:
        return validate_repository(override)
    try:
        result = subprocess.run(
            ["git", "remote", "get-url", "origin"],
            check=False,
            capture_output=True,
            text=True,
        )
    except OSError as error:
        raise StatusError(
            f"Could not read the origin remote ({error}); use --repo owner/name"
        ) from error
    if result.returncode != 0:
        raise StatusError("Could not read the origin remote; use --repo owner/name")
    return repository_from_remote(result.stdout)


def validate_api_base(value: str) -> str:
    parsed = urlsplit(value)
    if (
        parsed.scheme != "https"
        or not parsed.hostname
        or parsed.username
        or parsed.password
        or parsed.query
        or parsed.fragment
    ):
        raise StatusError(
            "--api-base must be an HTTPS URL without credentials, query, or fragment"
        )
    return value.rstrip("/")


def positive_run_id(value: str) -> int:
    try:
        run_id = int(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("run id must be an integer") from error
    if run_id <= 0:
        raise argparse.ArgumentTypeError("run id must be positive")
    return run_id


def wait_seconds(value: str) -> float:
    try:
        seconds = float(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("wait seconds must be a number") from error
    if not math.isfinite(seconds) or seconds < 0:
        raise argparse.ArgumentTypeError("wait seconds must be finite and non-negative")
    return seconds


class ArgumentParser(argparse.ArgumentParser):
    def error(self, message: str) -> None:
        raise StatusError(message)


def build_parser() -> argparse.ArgumentParser:
    parser = ArgumentParser(
        description="Read public GitHub Actions CI run and job status.",
        epilog=(
            "Exit codes: 0 all selected runs succeeded; 1 a run failed/cancelled or wait timed out; "
            "2 usage, HTTP, or parse error; 3 a run is still in progress without --wait. "
            "GH_TOKEN is optional and is read only from the environment."
        ),
    )
    parser.add_argument(
        "--repo", metavar="OWNER/NAME", help="repository (default: derive from origin)"
    )
    parser.add_argument(
        "--api-base",
        default=DEFAULT_API_BASE,
        help=f"API base URL (default: {DEFAULT_API_BASE})",
    )
    selector = parser.add_mutually_exclusive_group(required=True)
    selector.add_argument(
        "--sha",
        metavar="SHA",
        help="select CI workflow runs for this full or abbreviated head SHA",
    )
    selector.add_argument(
        "--run-id", type=positive_run_id, metavar="ID", help="fetch one workflow run"
    )
    parser.add_argument(
        "--json",
        action="store_true",
        dest="json_output",
        help="emit machine-readable JSON",
    )
    parser.add_argument(
        "--wait",
        nargs="?",
        const=DEFAULT_WAIT_SECONDS,
        type=wait_seconds,
        metavar="SECONDS",
        help=(
            f"poll every {POLL_INTERVAL_SECONDS} seconds until complete or timeout "
            f"(default: {DEFAULT_WAIT_SECONDS} seconds)"
        ),
    )
    return parser


def redact(value: object, token: str) -> object:
    if not token:
        return value
    if isinstance(value, str):
        return value.replace(token, "[redacted]")
    if isinstance(value, dict):
        return {key: redact(item, token) for key, item in value.items()}
    if isinstance(value, (list, tuple)):
        return [redact(item, token) for item in value]
    return value


def emit(text: str, stream, token: str) -> None:
    stream.write(redact(text, token) + "\n")


def render_text(runs: Sequence[Run]) -> str:
    lines: list[str] = []
    for run in runs:
        lines.extend(
            (
                f"Run {run.id}: {run.workflow_name}",
                f"  Event: {run.event}",
                f"  Status: {run.status}",
                f"  Conclusion: {run.conclusion or 'pending'}",
                f"  Head SHA: {run.head_sha}",
                "  Jobs:",
            )
        )
        lines.extend(
            f"    {job.name}: {job.status} / {job.conclusion or 'pending'}"
            for job in run.jobs
        )
    return "\n".join(lines)


def run_document(run: Run) -> dict[str, object]:
    return {
        "id": run.id,
        "workflow_name": run.workflow_name,
        "event": run.event,
        "status": run.status,
        "conclusion": run.conclusion,
        "head_sha": run.head_sha,
        "jobs": [
            {"name": job.name, "status": job.status, "conclusion": job.conclusion}
            for job in run.jobs
        ],
    }


def render_json(
    repository: str,
    runs: Sequence[Run],
    *,
    timed_out: bool = False,
    token: str = "",
) -> str:
    document = redact(
        {
            "repository": repository,
            "runs": [run_document(run) for run in runs],
            "timed_out": timed_out,
        },
        token,
    )
    return json.dumps(document, indent=2, sort_keys=True)


def exit_code(runs: Sequence[Run]) -> int:
    if any(run.status == "completed" and run.conclusion != "success" for run in runs):
        return 1
    if any(run.status != "completed" for run in runs):
        return 3
    return 0


def wait_for_completion(
    fetch_runs: Callable[[], list[Run]],
    timeout: float,
    *,
    emit_progress: Callable[[str], None],
    monotonic: Callable[[], float],
    sleep: Callable[[float], None],
) -> tuple[list[Run], bool]:
    runs = fetch_runs()
    deadline = monotonic() + timeout
    while exit_code(runs) == 3:
        remaining = deadline - monotonic()
        if remaining <= 0:
            return runs, True
        delay = min(POLL_INTERVAL_SECONDS, remaining)
        summary = ", ".join(
            f"Run {run.id}: {run.status}"
            + (f" ({run.conclusion})" if run.conclusion else "")
            for run in runs
            if run.status != "completed"
        )
        emit_progress(summary)
        emit_progress(f"Waiting; next check in {delay:g} seconds")
        sleep(delay)
        runs = fetch_runs()
    return runs, False


def main(
    argv: Sequence[str] | None = None,
    *,
    transport: Transport | None = None,
    environ: Mapping[str, str] | None = None,
    stdout=None,
    stderr=None,
    clock: Callable[[], float] | None = None,
    sleep: Callable[[float], None] | None = None,
) -> int:
    environment = os.environ if environ is None else environ
    token = environment.get("GH_TOKEN", "").strip()
    output = sys.stdout if stdout is None else stdout
    errors = sys.stderr if stderr is None else stderr
    try:
        args = build_parser().parse_args(argv)
        repository = resolve_repository(args.repo)
        api_base = validate_api_base(args.api_base)
        client = GitHubClient(
            repository,
            api_base,
            token,
            urllib_transport if transport is None else transport,
        )
        sha = (
            client.resolve_head_sha(validate_sha(args.sha))
            if args.sha is not None
            else None
        )

        def fetch_runs() -> list[Run]:
            run_ids = client.ci_run_ids(sha) if sha is not None else (args.run_id,)
            return [client.run(run_id) for run_id in run_ids]

        timed_out = False
        if args.wait is None:
            runs = fetch_runs()
        else:
            runs, timed_out = wait_for_completion(
                fetch_runs,
                args.wait,
                emit_progress=lambda message: emit(message, errors, token),
                monotonic=time.monotonic if clock is None else clock,
                sleep=time.sleep if sleep is None else sleep,
            )
            if timed_out:
                emit(
                    f"Timed out after {args.wait:g} seconds while waiting for CI",
                    errors,
                    token,
                )
        if args.json_output:
            report = render_json(repository, runs, timed_out=timed_out, token=token)
        else:
            report = render_text(runs)
        emit(report, output, token)
        return 1 if timed_out else exit_code(runs)
    except StatusError as error:
        emit(f"error: {error}", errors, token)
        return error.exit_code


if __name__ == "__main__":
    raise SystemExit(main())
