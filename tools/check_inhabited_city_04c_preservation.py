#!/usr/bin/env python3
"""Check preserved 04B assets/settings/progression and narrowly reviewed 04C hosts.

The comparison starts after the isolated, documented metadata repair. These are
source and payload checks, not native save loading or gameplay acceptance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BASE = "91c790b"
RUNTIME = "Assets/NeonSyndicate/Scripts/Runtime/"
HOSTS = {
    "AI/NpcController.cs", "AI/PedManager.cs", "AI/PoliceManager.cs", "Audio/AudioManager.cs", "Characters/PlayerCamera.cs",
    "Core/Game.cs", "Core/GameRoot.cs", "Core/WorldSystems.cs", "Gameplay/CraftingSystem.cs",
    "Gameplay/QuestBook.cs", "Gameplay/Quests/QuestBook.Givers.cs", "Gameplay/Quests/QuestCatalog.cs",
    "Gameplay/Quests/QuestData.cs", "Gameplay/Quests/QuestRunner.cs", "Gameplay/Quests/QuestSteps.cs",
    "Gameplay/Quests/QuestValidator.cs", "Gameplay/SaveSystem.cs", "Gameplay/WorldServices.cs",
    "QA/LivingTerritories04BQa.cs", "QA/QaTools.cs", "UI/MenuSystem.cs", "World/Atmosphere.cs", "World/CityBuilder.cs", "World/CityDressing.cs", "World/InnerWorld.cs"
}


def git(*args): return subprocess.check_output(["git", *args], cwd=ROOT)


def main():
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument("--report", type=Path, required=True); args = parser.parse_args()
    checks = []
    def check(name, passed, detail=None): checks.append({"name": name, "passed": bool(passed), "detail": detail})
    base = git("rev-parse", BASE).decode().strip(); paths = git("ls-tree", "-r", "--name-only", base).decode().splitlines()
    changed = []; inspected = 0; payload_bytes = 0
    for name in paths:
        if name.startswith("Assets/_Recovery/"): continue
        suffix = Path(name).suffix
        protected = name.startswith(("Packages/", "ProjectSettings/")) or (name.startswith("Assets/") and suffix in
            (".meta", ".obj", ".bytes", ".png", ".wav", ".shader", ".mat", ".prefab", ".unity", ".asmdef", ".asmref"))
        protected |= name.startswith(("Assets/NeonSyndicate/Resources/Quests/", "Assets/NeonSyndicate/Resources/Events/"))
        protected |= name.startswith(tuple(RUNTIME + folder for folder in ("Gameplay/Strongholds/", "Gameplay/Discovery/", "Gameplay/Travel", "Gameplay/Territories/", "Characters/PlayerController", "ArtRebirth04A/", "ArtTerritories04B/")))
        protected |= name in {RUNTIME + "Core/" + filename + ".cs" for filename in ("Act", "GInput", "Pad", "GameTime")}
        if not protected: continue
        inspected += 1; old = git("show", base + ":" + name); path = ROOT / name
        if not path.is_file(): changed.append(name); continue
        current = path.read_bytes(); payload_bytes += len(current)
        if old.startswith(b"version https://git-lfs.github.com/spec/v1"):
            expected = re.search(rb"oid sha256:([a-f0-9]{64})", old).group(1).decode(); same = hashlib.sha256(current).hexdigest() == expected
        else: same = old == current
        if not same: changed.append(name)
    check("Original assets/settings/input/travel/forts/discovery/territory logic and old content remain exact", not changed,
        {"checked": inspected, "bytes_checked": payload_bytes, "changed": changed})
    modified = set(git("diff", "--name-only", base, "--", RUNTIME).decode().splitlines()) & set(paths)
    unexpected = sorted(modified - {RUNTIME + host for host in HOSTS})
    check("Existing runtime mutations stay within reviewed integration hosts", not unexpected, {"unexpected": unexpected, "hosts": sorted(modified)})
    save_path = RUNTIME + "Gameplay/SaveSystem.cs"
    def fields(text):
        segment = text[text.index("public sealed class SaveData"):text.index("public static class SaveSystem")]
        return re.findall(r"^        public (?!sealed|static)([^\n]+;)$", segment, re.M)
    before, after = fields(git("show", base + ":" + save_path).decode()), fields((ROOT / save_path).read_text())
    check("Every original SaveData field declaration and initializer is preserved in order", after[:len(before)] == before, {"old_fields": len(before)})
    check("SaveData appends only the optional inhabited city block", after[len(before):] == ["InhabitedCitySave InhabitedCity = new InhabitedCitySave();"], after[len(before):])
    freeze = (ROOT / "Assets/NeonSyndicate/Tests/EditMode/SaveDataTests.cs").read_text()
    check("SaveData field freeze includes new optional block", '"InhabitedCity:InhabitedCitySave"' in freeze)
    game = (ROOT / (RUNTIME + "Core/Game.cs")).read_text()
    hooks = ["InhabitedCitySettings04C", "InhabitedCityBindings04C", "QuietSignal04C", "ArtInhabitedCity04C", "InhabitedCity04CComposition"]
    # Composition is pure and currently has no static state; only classes declaring a ResetStatics hook require registration.
    missing = []
    for name in hooks:
        matches = list((ROOT / RUNTIME).rglob(name + ".cs"))
        if matches and "ResetStatics()" in matches[0].read_text() and name + ".ResetStatics();" not in game: missing.append(name)
    check("New core static reset hooks are registered", not missing, missing)
    system = (ROOT / (RUNTIME + "Gameplay/InhabitedCity/InhabitedCitySystem.cs")).read_text()
    check("Disabled optional presentation still registers append-only save owner", "WorldSystems.Register(this)" in system and
        system.index("WorldSystems.Register(this)") < system.index("if (!EnabledAtBoot)"))
    boot = (ROOT / (RUNTIME + "Core/GameRoot.cs")).read_text()
    plan_at, build_at = boot.find("Game.Inhabited.Prepare(layout)"), boot.find("yield return Run(builder.Build(")
    check("Venue parcel plan precedes city mesh construction", 0 <= plan_at < build_at)
    roots = [ROOT / "Assets/NeonSyndicate/Resources/InhabitedCity04C"]
    placeholders = []; attributes = []
    for root in roots:
        for path in root.rglob("*"):
            if path.suffix not in (".obj", ".bytes", ".png", ".wav", ".blend"): continue
            relative = str(path.relative_to(ROOT)); raw = path.read_bytes()
            if raw.startswith(b"version https://git-lfs.github.com/spec/v1"): placeholders.append(relative)
            attributes.append({"path": relative, "filter": git("check-attr", "filter", "--", relative).decode().strip()})
    check("New resources contain actual asset payloads", not placeholders, placeholders)
    check("New binary/source art is assigned to Git LFS", all(row["filter"].endswith(": lfs") for row in attributes), attributes)
    report = {"scope": __doc__.strip(), "repaired_baseline": base, "checks": len(checks), "passed": sum(row["passed"] for row in checks),
        "failed": [row for row in checks if not row["passed"]], "details": checks, "native_save_loading": "NOT RUN", "native_gameplay": "NOT RUN"}
    args.report.parent.mkdir(parents=True, exist_ok=True); args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "details"}, indent=2)); return 0 if not report["failed"] else 1


if __name__ == "__main__": raise SystemExit(main())
