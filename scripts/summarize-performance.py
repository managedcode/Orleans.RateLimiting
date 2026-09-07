"""Summarize every paired ABBA measurement; never discard unfavorable samples."""

import collections
import json
import pathlib
import statistics
import sys

BASELINE = "baseline"
UPDATED = "updated"
VERSIONS = (BASELINE, UPDATED)
EXPECTED_SAMPLES = 6
PERCENT = 100
OPERATION_RATE = "OperationsPerSecond"
TAIL_LATENCY = "P99Milliseconds"
ALLOCATION = "ProcessAllocatedBytesPerOperation"
METRICS = (OPERATION_RATE, TAIL_LATENCY, ALLOCATION)
CASE_FIELDS = ("Algorithm", "Scenario", "Cancellable", "TokenCanBeCanceled")


def read_measurements(directory):
    groups = {version: collections.defaultdict(list) for version in VERSIONS}
    for path in sorted(directory.glob("*.json")):
        version = next(version for version in VERSIONS if f"-{version}-" in path.name)
        report = json.loads(path.read_text())
        if not report.get("WarmWorkload") or not report.get("Topology"):
            raise ValueError(f"Missing warmed topology evidence: {path}")
        for row in report["Results"]:
            groups[version][tuple(row[field] for field in CASE_FIELDS)].append(row)
    if not groups[BASELINE] or groups[BASELINE].keys() != groups[UPDATED].keys():
        raise ValueError("Baseline and updated cases must match and be nonempty")
    return groups


def summarize(groups):
    print("\nMedians of all six repetitions per case (two processes per revision).")
    print("Throughput includes successful acquisition and disposal; p99 is the median of per-repetition p99 values.")
    print("Raw JSON retains each repetition, allocations, GC counts, timestamps and verified topology.\n")
    print("| Scenario | Token overload | Cancellable token | Baseline ops/s | Updated ops/s | Change | p99 ms baseline → updated | B/op baseline → updated |")
    print("| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |")
    for case in sorted(groups[BASELINE]):
        values = {}
        for version in VERSIONS:
            rows = groups[version][case]
            if len(rows) != EXPECTED_SAMPLES:
                raise ValueError(f"Expected {EXPECTED_SAMPLES} samples for {version} {case}")
            values[version] = {metric: statistics.median(row[metric] for row in rows) for metric in METRICS}
        before, after = values[BASELINE], values[UPDATED]
        change = (after[OPERATION_RATE] - before[OPERATION_RATE]) / before[OPERATION_RATE] * PERCENT
        _, scenario, overload, token = case
        print(f"| {scenario} | {overload} | {token} | {before[OPERATION_RATE]:,.0f} | {after[OPERATION_RATE]:,.0f} | {change:+.1f}% | {before[TAIL_LATENCY]:.3f} → {after[TAIL_LATENCY]:.3f} | {before[ALLOCATION]:,.0f} → {after[ALLOCATION]:,.0f} |")


if __name__ == "__main__":
    summarize(read_measurements(pathlib.Path(sys.argv[-1])))
