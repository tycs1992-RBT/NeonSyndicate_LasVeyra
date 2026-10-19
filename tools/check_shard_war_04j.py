#!/usr/bin/env python3
"""04J Shard War source-wiring regression assertions. This is NOT native Unity validation."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PREFIX = Path("Assets/NeonSyndicate")

def inspect(root):
    paths = {
        "spawn": PREFIX / "Scripts/Runtime/Creatures/CreatureDirector.Ecology.cs",
        "debug": PREFIX / "Scripts/Runtime/Creatures/CreatureDirector.Debug.cs",
        "loot": PREFIX / "Scripts/Runtime/Gameplay/LootArmory.Drops.cs",
        "generator": PREFIX / "Scripts/Runtime/Gameplay/LootGenerator.cs",
        "uniques": PREFIX / "Scripts/Runtime/Gameplay/LootTypes.cs",
        "loot_test": PREFIX / "Tests/EditMode/LootDropTests.cs",
    }
    missing = [str(p) for p in paths.values() if not (root / p).exists()]
    if missing:
        return {"passed": False, "missing_files": missing, "checks": []}
    src = {name: (root / path).read_text(encoding="utf-8-sig") for name, path in paths.items()}
    checks = []
    def check(name, ok):
        checks.append({"name": name, "passed": bool(ok)})

    spawn, debug, loot = src["spawn"], src["debug"], src["loot"]
    check("Uses existing ecology spawner, no rival manager",
          "private void TryPlanGroup(HabitatContext ctx)" in spawn
          and "private bool FindSpawnPoint(" in spawn
          and "_cityRiftWalks04I.Add(w.Pos)" in spawn)
    check("CityRift retries safe shifted walk nodes",
          "spot == SpawnSpot.CityRift ? 28 : 16" in spawn
          and "Mathf.Cos(a) * radius" in spawn
          and "if (!GroundFor(" in spawn
          and "if (!GroundOk(" in spawn
          and "if (SeenByCamera(at) || NearObjective(c))" in spawn)
    check("Existing spawn budgets and owned groups retained",
          "CreatureEcology.Caps(Settings.Graphics" in spawn
          and "CountAmbient(out groups, out bodies)" in spawn
          and "_ambient.Add(ag)" in spawn)
    ecology = (root / PREFIX / "Scripts/Runtime/Creatures/CreatureLogic.cs").read_text(encoding="utf-8-sig")
    creature_tests = (root / PREFIX / "Tests/EditMode/CreatureLogicTests.cs").read_text(encoding="utf-8-sig")
    check("Repeat-pack diversity is actually wired into the original director",
          "AmbientGroupsOfSpecies04J(candidate.Species)" in spawn
          and "RepeatPackWeight04J(" in spawn
          and "public static float RepeatPackWeight04J(int activeGroups)" in ecology
          and "ShardWarAmbientPackDiversityRespectsFiniteBudgetAndRetainsBothSpecies" in creature_tests)
    check("Diagnoses no candidates, roofs, roads, overlap, camera, timeouts",
          all(v in spawn for v in (
            "SpawnReason04J.NoCandidate", "SpawnReason04J.NoGround",
            "SpawnReason04J.ZoneOrRoad", "SpawnReason04J.PhysicsOrOccupied",
            "SpawnReason04J.CameraOrObjective", "SpawnReason04J.MeshTimeout")))
    check("Diagnostics visible through existing F2 Creatures page only",
          "SpawnLedger04J.Split" in debug and "RegisterDebugPage()" in debug)
    check("Crystal NPCs pass an explicit caster flag through real drop hook",
          "DropRolled(src, at, -1, n.HasCrystalRole04F)" in loot)
    check("Eligible creature kills request genuine CrystalCaster",
          'DropRolled(src, at, -1, sp.Id != "glass_mite")' in loot
          and "earnedCrystalCaster ? (int)LootArchetype.CrystalCaster : -1" in loot)
    check("Mythic random rolls cannot unlock boss-only signature indices",
          "r == Rarity.Mythic) r = Rarity.Legendary" in loot
          and "if (earnedCrystalCaster && unique < 0" in loot
          and "GuaranteedOnly" in src["uniques"])
    check("Original UID/pickup and boss reward paths remain",
          "SpawnDrop(g, at +" in loot and
          "GiveCores(src, sp.Resists)" in loot and
          "OnBossKilled(CreatureSpecies sp" in loot)
    check("Authored real Caster and save round-trip NUnit regression",
          "ShardWarAmbientCrystalDropsAreRealCastersWithStableSaveRecords" in src["loot_test"]
          and "LootRecords.FromSave(LootRecords.ToSave(g))" in src["loot_test"])
    return {
      "passed": all(row["passed"] for row in checks),
      "checks": checks,
      "limitations": "source text patterns only; no Unity compilation, physics, play, save or rewards executed",
      "native_unity": "not executed",
      "nunit_executed": False,
    }

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--root", type=Path, default=ROOT)
    p.add_argument("--report", type=Path, required=True)
    args = p.parse_args()
    result = inspect(args.root.resolve())
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": result["passed"], "checks": len(result["checks"]),
                      "failures": [x["name"] for x in result["checks"] if not x["passed"]],
                      "scope": "source-only"}, indent=2))
    return 0 if result["passed"] else 1

if __name__ == "__main__":
    raise SystemExit(main())
