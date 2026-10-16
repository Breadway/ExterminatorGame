#!/usr/bin/env python3
"""Check project C# syntax and Unity asset metadata without a Unity installation.

Requires tree-sitter and tree-sitter-c-sharp. This does not compile C# or run tests.
Each file is parsed in its own subprocess: the tree-sitter bindings segfault when one process
parses a few dozen large files in a row.
"""
import json
import subprocess
from pathlib import Path
import sys

try:
    import tree_sitter  # noqa: F401
    import tree_sitter_c_sharp  # noqa: F401
except ImportError:
    sys.exit("Install validation dependencies: pip install tree-sitter tree-sitter-c-sharp")

CHILD = """
import sys
from pathlib import Path
from tree_sitter import Language, Parser
import tree_sitter_c_sharp
parser = Parser(Language(tree_sitter_c_sharp.language()))
pending = [parser.parse(Path(sys.argv[1]).read_bytes()).root_node]
bad = []
while pending:
    node = pending.pop()
    if node.type == "ERROR" or node.is_missing:
        bad.append(node.start_point.row + 1)
    pending.extend(node.children)
print(" ".join(map(str, bad)))
"""


def main():
    root = Path(__file__).resolve().parents[1] / "Assets" / "_Project"
    errors = []
    sources = sorted(root.rglob("*.cs"))
    for source in sources:
        result = subprocess.run([sys.executable, "-c", CHILD, str(source)], capture_output=True, text=True)
        if result.returncode != 0:
            errors.append(f"{source}: parser crashed ({result.returncode})")
        elif result.stdout.strip():
            for line in result.stdout.split():
                errors.append(f"{source}:{line}: syntax error")
        if not Path(str(source) + ".meta").exists():
            errors.append(f"{source}: missing Unity metadata")

    for definition in root.rglob("*.asmdef"):
        try:
            json.loads(definition.read_text())
        except ValueError as exc:
            errors.append(f"{definition}: {exc}")

    for error in errors:
        print(error, file=sys.stderr)
    print(f"Checked {len(sources)} C# files: {len(errors)} errors")
    print("Syntax checks only. Unity compilation and Edit/Play Mode tests still required.")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
