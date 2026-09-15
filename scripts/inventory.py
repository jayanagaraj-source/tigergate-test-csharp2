#!/usr/bin/env python3
"""Enumerate every package declaration in the repo with its file and line number.

Prints the same table the README embeds, so a TigerGate scan result can be
diffed against ground truth.
"""
import glob
import re
from collections import defaultdict

PKGREF = re.compile(r'PackageReference Include="([^"]+)" Version="([^"]+)"')
PKGCFG = re.compile(r'package id="([^"]+)" version="([^"]+)"')


def collect():
    rows = []
    projects = sorted(
        glob.glob("src/**/*.csproj", recursive=True)
        + glob.glob("tests/**/*.csproj", recursive=True)
    )
    for path in projects:
        with open(path, encoding="utf-8") as handle:
            for line_no, line in enumerate(handle, 1):
                match = PKGREF.search(line)
                if match:
                    rows.append((match.group(1), match.group(2), path, line_no))

    for path in sorted(glob.glob("src/**/packages.config", recursive=True)):
        with open(path, encoding="utf-8") as handle:
            for line_no, line in enumerate(handle, 1):
                match = PKGCFG.search(line)
                if match:
                    rows.append((match.group(1), match.group(2), path, line_no))

    rows.sort(key=lambda row: (row[0].lower(), row[2]))
    return rows


def main():
    rows = collect()

    print("| # | Package | Version | Declared in | Line |")
    print("|---|---------|---------|-------------|------|")
    for index, (pkg, version, path, line_no) in enumerate(rows, 1):
        print(f"| {index} | `{pkg}` | `{version}` | `{path}` | {line_no} |")

    by_name = defaultdict(list)
    for pkg, version, path, line_no in rows:
        by_name[pkg].append((version, path, line_no))

    dupes = {k: v for k, v in by_name.items() if len(v) > 1}

    print()
    print(f"Total package declarations: {len(rows)}")
    print(f"Distinct package names: {len(by_name)}")
    print(f"Packages declared in more than one manifest: {len(dupes)}")
    print()

    for name in sorted(dupes, key=str.lower):
        versions = sorted({entry[0] for entry in dupes[name]})
        print(f"{name}: {len(dupes[name])} declarations, versions {', '.join(versions)}")
        for version, path, line_no in dupes[name]:
            print(f"    {path}:{line_no}  {version}")


if __name__ == "__main__":
    main()
