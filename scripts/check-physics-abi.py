#!/usr/bin/env python3
"""Check the scalar/pointer ABI used by the C# delegates and native physics table."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent


def abi_type(value):
    value = value.strip()
    if "*" in value or value == "IntPtr" or value.startswith("ref ") or value.endswith("Callback"):
        return "pointer"
    return {"sBool": "int", "sUint": "int", "uint": "int", "unsigned int": "int"}.get(value, value)


def parameters(value):
    return [abi_type(re.sub(r"\s+\w+$", "", part.strip()))
            for part in value.split(",") if part.strip()]


def check():
    managed = (ROOT / "Assets/BallancePhysics/Api/ApiDefine.cs").read_text()
    native = (ROOT / "BallancePhysics/ballance_physics.cpp").read_text()
    delegates = re.findall(r"public delegate (\w+) fn_(\w+)\(([^)]*)\)", managed)
    assert len(delegates) > 80, "Physics delegates were not found"
    for result, name, args in delegates:
        function = re.search(r"\n([\w*]+) " + name + r"\(([^;{}]*?)\)\s*\{", native)
        assert function, f"Missing native implementation: {name}"
        expected = (abi_type(result), parameters(args))
        actual = (abi_type(function[1]), parameters(function[2]))
        assert actual == expected, f"{name}: C# {expected}, C++ {actual}"
    print(f"Physics ABI checked: {len(delegates)} delegates")


if __name__ == "__main__":
    check()
