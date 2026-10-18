#!/usr/bin/env python3
"""Source wiring guard for the 04I crystal population restoration.

This catches accidental removal of runtime connections, not actual creature AI,
compiled C# semantics, Unity scene import, play feel, or FPS. Engine tests remain mandatory.
"""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def inspect(root):
    source = root / "Assets/NeonSyndicate"
    paths = {
        "ecology": source / "Scripts/Runtime/Creatures/CreatureLogic.cs",
        "spawner": source / "Scripts/Runtime/Creatures/CreatureDirector.Ecology.cs",
        "story": source / "Scripts/Runtime/Gameplay/StoryMissions.cs",
        "loot": source / "Scripts/Runtime/Gameplay/LootArmory.Drops.cs",
        "tests": source / "Tests/EditMode/CreatureLogicTests.cs",
        "safety": source / "Scripts/Runtime/AI/StarterSafety.cs",
        "creature_rewards": source / "Scripts/Runtime/Creatures/CreatureDirector.cs",
    }
    missing = [str(path.relative_to(root)) for path in paths.values() if not path.is_file()]
    if missing:
        return {"passed": False, "missing_source": missing, "checks": []}
    text = {key: path.read_text(encoding="utf-8-sig") for key, path in paths.items()}
    checks = []

    def check(name, ok):
        checks.append({"name": name, "passed": bool(ok)})

    ecology = text["ecology"]
    city = ecology.split("else if (c.City)", 1)[-1].split("else if (c.Frost)", 1)[0]
    check("City day and night table retains hollow packs",
          'new SpawnCandidate("hollow"' in city and "if (!night) return;" not in city)
    check("City generic street has glass mite swarms", 'new SpawnCandidate("glass_mite"' in city)
    check("City park jackals, worksite golems and grave shades retained",
          all(id in city for id in ('"neon_jackal"', '"shard_golem"', '"hollow_shade"')))
    check("Saved spawn spot enumeration is append-only",
          "Inner,\n        /// <summary>04I" in ecology and "CityRift" in ecology)
    check("Elites and focus probabilities use finite shared policy",
          "class CrystalHordeRules04I" in ecology and
          "EliteChance(" in ecology and "FocusChance(" in ecology and "FactionFocusChance(" in ecology)

    spawner = text["spawner"]
    check("City rift candidates originate from actual city walk nodes",
          "_cityRiftWalks04I.Add(w.Pos)" in spawner and
          "case SpawnSpot.CityRift:" in spawner and
          "_spotScratch[_rng.Next(_spotScratch.Count)]" in spawner)
    check("City mobs still require standard physics/road/quiet admission",
          all(snippet in spawner for snippet in (
              "if (LocalQuiet.Blocks(c)) return false;",
              "if (L.IsInsideBuilding(c, 2f)) return false;",
              "if (L.RoadAt(c, 1.5f) != null",
              "SlotFree(c, sp)")))
    check("Ambient creatures retain bounded graphics and non-scripted AI",
          "CreatureEcology.Caps(Settings.Graphics" in spawner and
          "o.Hostile = false;" in spawner and
          "CrystalHordeRules04I.EliteChance(ctx, species)" in spawner)

    story = text["story"]
    check("Opening story contains player-optional crystal creatures",
          "SpawnCrystalWake04I(c, PrologueLayout.HunterCenter(manor)" in story and
          "m.Story[0].OnCleanup = CleanupCrystalWake04I;" in story)
    check("False King has stronger wave with bounded fight-or-retreat",
          "SpawnCrystalWake04I(c, meet +" in story and
          "crystalUntil = Time.time + 20f" in story and
          "m.Story[3].OnCleanup = CleanupCrystalWake04I;" in story)
    check("Mission crystal pack uses own tag and existing CreatureDirector",
          'c.Def.Id + ":crystal_wake_04i"' in story and
          "Game.Creatures.DespawnTag(" in story and
          'Game.Creatures.SpawnGroup("neon_jackal"' in story)
    check("Story Wardens, Weavers and Sentinels are crystal-equipped",
          all(s in story for s in (
              "CrystalNpcRole04F.BladeWarden",
              "CrystalNpcRole04F.FocusWeaver",
              "CrystalNpcRole04F.WardSentinel",
              "CrystalNpcRole04F.RiftDuelist")))

    loot = text["loot"]
    check("Creature kills use exactly one original weapon-drop pipeline",
          "CrystalHordeRules04I.FocusChance" in loot and
          "if (UnityEngine.Random.value < chance) DropRolled(src, at, -1);" in loot)
    check("Faction non-elite loot remains routed through original loot path",
          "CrystalHordeRules04I.FactionFocusChance" in loot)
    check("NoRewards practice bodies still suppressed",
          "if (options != null && options.NoRewards) return;" in text["creature_rewards"])
    check("Starter welcome-area policy still exists",
          "BlockAmbientAggression" in text["safety"] and "NotifyPlayerAggression" in text["safety"])
    check("Actual authored NUnit regression coverage is present",
          "CityCrystalHordesExistByDayAndGrowMoreDangerousAtNight" in text["tests"] and
          "CrystalHordeElitesAndFocusDropsStayBoundedWithoutTinySwarmFarming" in text["tests"])
    return {"passed": all(c["passed"] for c in checks),
            "checks": checks, "scope": __doc__.strip(),
            "native_unity": "NOT RUN", "nunit_executed": False,
            "type_checked": False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    result = inspect(args.root.resolve())
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": result["passed"],
                      "checks": len(result["checks"]),
                      "failed": [x["name"] for x in result["checks"] if not x["passed"]],
                      "scope": "source wiring only; not Unity or NUnit"}, indent=2))
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
