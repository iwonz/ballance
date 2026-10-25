#!/usr/bin/env python3
"""Check repository context; OpenSpec itself validates requirement syntax."""

import argparse
import ast
import io
import re
import subprocess
import tokenize
from pathlib import Path
from urllib.parse import unquote, urlsplit


ROOT = Path(__file__).resolve().parent.parent
NON_ENGLISH = re.compile(r"[\u0400-\u052f\u3400-\u9fff]")
C_SOURCE = {".cs", ".c", ".cpp", ".h", ".hpp", ".shader", ".cginc", ".rc"}
TOKENS = re.compile(
    r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    r'|(?P<comment>//[^\r\n]*|/\*[\s\S]*?\*/)'
)
SKILLS = (
    "explore", "propose", "new-change", "continue-change", "ff-change",
    "apply-change", "update-change", "verify-change", "sync-specs", "archive-change",
)


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args]).decode("utf-8")


def repository_files(root):
    return sorted({Path(p) for p in git(root, "ls-files", "-z", "--cached", "--others",
                                      "--exclude-standard").split("\0") if p})


def vendor(path):
    return str(path).startswith("Assets/Plugins/") or "Virtools-SDK" in path.parts


def maintenance_fragments(path, text):
    """Separate prose from localized values and stable asset identifiers."""
    if path.suffix in C_SOURCE:
        for match in TOKENS.finditer(text):
            if match.group("comment"):
                yield match[0]
            elif path.suffix == ".cs" and not vendor(path):
                # Localization assertions, one legacy level ID, and an original legal notice.
                if str(path) == "Assets/System/Scripts/Editor/LocalizationChecks.cs":
                    continue
                if (str(path) == "Assets/Game/LevelBuilder/Scripts/LevelBuilderInit.cs"
                        and match[0] == '"\u6d4b\u8bd5\u5173\u5361"'):
                    continue
                if (str(path) == "Assets/Game/LevelEditor/Scripts/Editor/Modding/WindowAbout.cs"
                        and match[0] == '"Copyright © 2020 mengyu \u7248\u6743\u6240\u6709\uff0cMIT \u5f00\u6e90\u534f\u8bae"'):
                    continue
                yield match[0]
        yield from re.findall(r"^\s*#(?:end)?region[^\r\n]*", text, re.MULTILINE)
    elif path.suffix == ".filters" or (path.name.startswith("template_")
                                       and path.suffix in {".xml", ".json"}):
        yield text
    elif path.suffix in {".xml", ".csproj", ".vcxproj"}:
        yield from re.findall(r"<!--[\s\S]*?-->", text)
    elif path.suffix == ".py":
        yield from (t.string for t in tokenize.generate_tokens(io.StringIO(text).readline)
                    if t.type == tokenize.COMMENT)
        for node in ast.walk(ast.parse(text)):
            if isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef)):
                doc = ast.get_docstring(node)
                if doc:
                    yield doc
    elif path.suffix in {".md", ".txt", ".sh", ".yml", ".yaml", ".bat", ".cmd"}:
        if vendor(path) or "ThirdPartAllLincnse" in path.parts:
            return
        # Asset names in inline code/link destinations are identifiers, not prose.
        text = re.sub(r"`[^`\n]*`", "", text)
        text = re.sub(r"\]\([^\n]*?\)", "]()", text)
        yield text


def language_errors(path, text):
    return [f"{path}: non-English maintenance text: {line.strip()[:100]}"
            for fragment in maintenance_fragments(path, text)
            for line in fragment.splitlines() if NON_ENGLISH.search(line)]


def anchors(text):
    return {re.sub(r"[^\w\- ]", "", heading.lower()).replace(" ", "-")
            for heading in re.findall(r"^#{1,6}\s+(.+?)\s*#*\s*$", text, re.MULTILINE)}


def link_errors(root, path, text):
    errors = []
    # Code fences contain command examples, not rendered links.
    text = re.sub(r"^```[^\n]*\n[\s\S]*?^```[^\n]*$", "", text, flags=re.MULTILINE)
    for target in re.findall(r"\[[^\]\n]*\]\(([^\n]*?)\)", text):
        target = target.strip("<>")
        parts = urlsplit(target)
        if parts.scheme or parts.netloc:
            continue
        name = unquote(parts.path)
        destination = ((root / name.lstrip("/")) if name.startswith("/")
                       else (root / path).parent / name) if name else root / path
        destination = destination.resolve()
        if not destination.is_relative_to(root.resolve()) or not destination.exists():
            errors.append(f"{path}: missing local link {target}")
        elif parts.fragment and destination.suffix == ".md":
            if unquote(parts.fragment) not in anchors(destination.read_text(encoding="utf-8-sig")):
                errors.append(f"{path}: missing heading in {target}")
    return errors


def record_errors(directory):
    errors = []
    for name in (".openspec.yaml", "proposal.md", "design.md", "tasks.md", "verification.md"):
        if not (directory / name).is_file() or not (directory / name).read_text().strip():
            errors.append(f"{directory.name}: missing or empty {name}")
    tasks = (directory / "tasks.md").read_text() if (directory / "tasks.md").exists() else ""
    boxes = re.findall(r"^\s*- \[([ xX])\]", tasks, re.MULTILINE)
    if not boxes or any(box == " " for box in boxes):
        errors.append(f"{directory.name}: archived tasks must all be complete")
    verification = ((directory / "verification.md").read_text()
                    if (directory / "verification.md").exists() else "")
    for heading in ("Checks", "Results", "Limitations"):
        section = re.search(rf"^## {heading}[ \t]*\n([\s\S]*?)(?=^## |\Z)",
                            verification, re.MULTILINE)
        if not section or not section[1].strip():
            errors.append(f"{directory.name}: verification needs a nonempty {heading} section")
    metadata = ((directory / ".openspec.yaml").read_text()
                if (directory / ".openspec.yaml").exists() else "")
    if not list((directory / "specs").rglob("spec.md")) and not re.search(
            r"^skip_specs:\s*true\s*(?:#.*)?$", metadata, re.MULTILINE):
        errors.append(f"{directory.name}: needs delta specs or explicit skip_specs: true")
    return errors


def delivery_errors(root, base):
    base = git(root, "rev-parse", "--verify", f"{base}^{{commit}}").strip()
    changed = git(root, "diff", "--name-only", "-z", base).split("\0")
    untracked = git(root, "ls-files", "--others", "--exclude-standard", "-z").split("\0")
    if not any(changed + untracked):
        return []
    added = git(root, "diff", "--name-only", "--diff-filter=A", "--no-renames", "-z", base).split("\0")
    records = {Path(p).parent for p in added + untracked
               if re.fullmatch(r"openspec/changes/archive/[^/]+/proposal\.md", p)}
    if not records:
        return ["delivery: repository edits require a newly archived OpenSpec change in this diff"]
    return [error for record in sorted(records) for error in record_errors(root / record)]


def check(root, base=None):
    errors = []
    files = repository_files(root)
    for path in files:
        absolute = root / path
        if not absolute.is_file() or path.suffix not in C_SOURCE | {
                ".md", ".txt", ".py", ".xml", ".json", ".filters", ".csproj", ".vcxproj",
                ".sh", ".yml", ".yaml", ".bat", ".cmd"}:
            continue
        try:
            text = absolute.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            if vendor(path):
                # Original SDK headers and font licenses retain their legacy encoding.
                text = absolute.read_text(encoding="cp1252")
            else:
                errors.append(f"{path}: maintenance text must be UTF-8")
                continue
        errors.extend(language_errors(path, text))
        if path.suffix == ".md" and not vendor(path) and "archive" not in path.parts:
            errors.extend(link_errors(root, path, text))

    required = ["AGENTS.md", "openspec/config.yaml", "openspec/README.md",
                "openspec/specs/AGENTS.md", "docs/architecture.md", "docs/validation.md",
                "scripts/setup-openspec.sh"]
    required += [f".agents/skills/openspec-{skill}/SKILL.md" for skill in SKILLS]
    errors.extend(f"missing context file: {path}" for path in required if not (root / path).is_file())
    setup = root / "scripts/setup-openspec.sh"
    if setup.exists():
        version = re.search(r"^version=([\d.]+)$", setup.read_text(), re.MULTILINE)
        for name in ("openspec/README.md", ".github/workflows/macos-apple-silicon.yml"):
            if not version or not (root / name).exists() or (
                    f"@fission-ai/openspec@{version[1]}" not in (root / name).read_text()):
                errors.append(f"{name}: OpenSpec version must match setup-openspec.sh")
    specs = sorted((root / "openspec/specs").rglob("spec.md"))
    if not specs:
        errors.append("no current capability specifications")
    architecture = root / "docs/architecture.md"
    if architecture.exists():
        source_map = architecture.read_text()
        for spec in specs:
            capability = spec.parent.relative_to(root / "openspec/specs").as_posix()
            anchor = capability.replace("/", "-")
            if (anchor not in anchors(source_map)
                    or f"../openspec/specs/{capability}/spec.md" not in source_map
                    or f"/docs/architecture.md#{anchor}" not in spec.read_text()):
                errors.append(f"{capability}: spec and source map must link to each other")
    for record in sorted((root / "openspec/changes/archive").glob("*")):
        if record.is_dir():
            errors.extend(record_errors(record))
    if base:
        errors.extend(delivery_errors(root, base))
    return errors, len(specs)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", help="base commit for a completed delivery")
    args = parser.parse_args()
    try:
        errors, count = check(ROOT, args.base)
    except (OSError, ValueError, SyntaxError, tokenize.TokenError, subprocess.CalledProcessError) as exc:
        parser.exit(1, f"Context check failed: {exc}\n")
    if errors:
        parser.exit(1, "\n".join(errors) + "\n")
    print(f"Agent context OK: {count} capabilities; language, links, tooling, and change records checked.")


if __name__ == "__main__":
    main()
