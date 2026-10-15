#!/usr/bin/env python3
"""Author the exact 04A stronghold arrangements consumed by Unity and offline previews.

Original procedural composition, no external art. Coordinates are local to the
existing 02I fort origin. Existing sockets, the entire original collider set and
the central entry/core/warden area remain owned by the original game.
"""
import json
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/NeonSyndicate/Resources/VisualRebirth04A/Layouts"


def meta(path, folder=False):
    target = Path(str(path) + ".meta")
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, "crystal-manor-04a/" + str(path.relative_to(ROOT))).hex
    importer = "folderAsset: yes\nDefaultImporter" if folder else "TextScriptImporter"
    target.write_text(f"fileFormatVersion: 2\nguid: {guid}\n{importer}:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


class Layout:
    def __init__(self, site, title, style):
        self.data = dict(version="04A", site=site, title=title, previewStyle=style,
                         parts=[], collisions=[], route=[])

    def part(self, model, role, at, scale=(1, 1, 1), yaw=0, fine=False, euler=None):
        self.data["parts"].append(dict(model=model, role=role, position=list(at),
            scale=list(scale), euler=list(euler or (0, yaw, 0)), fine=fine))

    def collision(self, name, at, size):
        self.data["collisions"].append(dict(name=name, position=list(at), size=list(size)))

    def waypoint(self, name, at):
        self.data["route"].append(dict(name=name, position=list(at)))

    def deck(self, name, x, z, top, width, depth):
        # The source terrace has beveled corners. Two inset boxes form a conservative
        # cross entirely inside its octagonal footprint, never an invisible corner.
        self.part("terrace", "paving", (x, top-.35, z), (width/8, .35/.6, depth/8))
        self.collision(name + " center", (x, top-.15, z), (width*.75, .3, depth*.96))
        self.collision(name + " sides", (x, top-.15, z), (width*.96, .3, depth*.75))

    def stairs(self, name, x, start, base):
        self.part("stair", "stone", (x, base, start+3))
        for i in range(12):
            rise = (i+1)*.25
            self.collision(name + " tread " + str(i+1), (x, base+rise/2, start+.25+i*.5), (4, rise, .5))
        self.waypoint(name + " foot", (x, base+.25, start+.25))
        self.waypoint(name + " landing", (x, base+3, start+5.75))

    def garden(self, x, z, species="willow", scale=.55, yaw=0):
        self.part("planter", "stone", (x, .09, z), (1.1, .85, 1.1))
        self.part(species+"_trunk", "bark", (x, .3, z), (scale, scale, scale), yaw)
        self.part(species+"_canopy", "leaf", (x, .3, z), (scale, scale, scale), yaw)
        for i, (dx, dz) in enumerate(((-1.2, -.7), (1, -.9), (.7, 1.1), (-1.1, .9))):
            self.part("fern", "leaf", (x+dx, .2, z+dz), (.5, .65, .5), i*87, True)
            self.part("flower", "flower", (x+dx*.8, .2, z+dz*.8), (.55, .55, .55), i*57, True)

    def save(self):
        path = OUT / (self.data["site"] + ".json")
        path.write_text(json.dumps(self.data, indent=2) + "\n")
        meta(path)
        print(path.relative_to(ROOT), len(self.data["parts"]), "parts,", len(self.data["collisions"]), "colliders")


def base_court(p, kind):
    """Shared scale grammar; gate, rear skyline and planting differ by culture."""
    # Shallow tessellated paving overlays the existing physical slab, keeping foot
    # height within two centimetres of the unchanged floor. Broad lanes remain open.
    for z in (-9, -1, 7):
        p.part("terrace", "paving", (0, .071, z), (1.5, .025, 1.02))
    p.part("sky_ring", "metal", (0, .09, -7), (.72, .035, .72))
    # Narrow architecture on top of the original wall, behind socket boundaries.
    for side in (-1, 1):
        for z in (-8, 0, 8):
            model = "arcade" if kind != "rift" else "ruin"
            p.part(model, "stone", (side*15.25, 2.17, z), (.95, .75, .65), 90)
            p.part("buttress", "trim", (side*15.7, .08, z+3.4), (.65, .9, .65), side*90)
            p.part("banner", "cloth", (side*15.8, 5.2, z), (.65, .85, .65), side*90)
            p.part("lantern", "metal", (side*14.6, 3.1, z+2.5), (.6, .6, .6), fine=True)
        for z in (-14.2, 14.2):
            p.part("tower", "stone", (side*15.25, .08, z), (.55, .65, .55))
            p.part("spire", "roof", (side*15.25, 7.85, z), (.5, .64 if kind == "court" else .42, .5))
            p.part("crystal_cluster", "crystal", (side*15.25, 14.5 if kind == "court" else 11.4, z), (.28, .45, .28))
        for z in (-14, -6, 2, 10):
            # The lane is between the inside courtyard and socket footprints.
            p.part("threshold", "paving", (side*5.8, .08, z), (.8, .25, .8), fine=True)
        p.part("ward_pylon", "trim", (side*6.9, .08, -20.2), (.72, .9, .72))
        p.part("sigil", "metal", (side*6.9, 3.3, -20.8), (.7, .7, .7), fine=True)
    p.waypoint("original challenge sigil", (0, .07, -20))
    p.waypoint("original clear gate", (0, .07, -14))
    p.waypoint("original core interaction", (0, .07, 2.65))


def home():
    p = Layout("hearthspire", "Hearthspire, the Sevenfold Sanctuary", 0)
    base_court(p, "court")
    # Twin gates form an inhabited arrival sequence while leaving a 6.5m opening.
    p.part("portal", "stone", (0, .08, -15.8), (1.3, 1.12, .9))
    p.part("portal", "trim", (0, .08, -21), (1.3, .78, .65))
    p.part("sky_ring", "metal", (0, 11, -15.8), (.65, .65, .65), euler=(90, 0, 0))
    p.part("crystal_cluster", "crystal", (0, 9.8, -15.8), (.6, 1.5, .6))
    for side in (-1, 1):
        p.part("guardian", "stone", (side*9.3, .1, -20.3), (.65, .8, .65), -side*18)
        p.part("banner", "cloth", (side*4.8, 7.1, -16.2), (.8, 1.2, .8))
        p.part("window", "glass", (side*8.8, 2.4, -15.3), (1, 1, .7))
        for x in (4, 7):
            p.part("flower", "flower", (side*x, .12, -18), (.65, .65, .65), x*37, True)
        p.part("market_awning", "cloth", (side*6.8, .12, -6), (.72, .72, .65), side*90)
        p.part("bench", "bark", (side*6.4, .15, -1), (.8, .8, .8), side*90)
        for z in (-11.3, 4.7):
            p.part("planter", "stone", (side*6.3, .09, z), (.75, .55, .75))
            p.part("fern", "leaf", (side*6.3, .74, z), (.65, .8, .65), fine=True)
            p.part("flower", "flower", (side*6.3, .74, z), (.65, .8, .65), fine=True)
    # A real, open-sided crystal core chamber. Decorative columns remain outside
    # the protected 12.4m-wide courtyard and never intersect the six sockets.
    for x in (-6.7, 6.7):
        for z in (1, 11.5):
            p.part("column", "stone", (x, .1, z), (.65, 1.1, .65))
            p.collision("core pavilion column", (x, 3.38, z), (.55, 6.56, .55))
            p.part("capital", "metal", (x, 6.1, z), (.48, .45, .48))
    p.part("rib_vault", "trim", (0, 3.7, 6.2), (2.1, .75, 1.7))
    p.part("roof", "roof", (0, 6.7, 6.2), (1.94, .8, 1.65))
    p.part("roof_finial", "metal", (0, 10.14, 6.2), (.8, .8, .8))
    p.part("sky_ring", "metal", (0, 5.6, 5), (.85, .85, .85))
    for x in (-5.9, 5.9):
        p.part("window", "glass", (x, 2, 12.5), (.7, 1.05, .7))
        p.part("lantern", "metal", (x, 2.1, 1.3), (.65, .65, .65), fine=True)
    # Multi-level west promenade: 0m -> 3m -> 6m, then a continuous north balcony.
    # The route remains outside the original perimeter and every socket envelope.
    p.stairs("west cloister first stair", -20, -16, .07)
    p.deck("west cloister lower landing", -20, -6, 3.07, 4, 8.4)
    p.waypoint("west cloister lower terrace", (-20, 3.07, -6))
    p.stairs("west cloister second stair", -20, -2, 3.07)
    p.deck("west cloister upper walk", -20, 8, 6.07, 4, 8.4)
    p.deck("west cloister upper turn", -20, 16, 6.07, 4, 8.4)
    p.deck("north scholar overlook", -8.3, 20, 6.07, 25, 5)
    p.waypoint("west cloister upper walk", (-20, 6.07, 8))
    p.waypoint("west cloister upper turn", (-20, 6.07, 18.3))
    p.waypoint("north scholar overlook", (-6, 6.07, 20))
    # Arcade supports are visible at ground height; ground can pass underneath.
    for z in (-6, 8, 16):
        p.part("arcade", "stone", (-20, .08, z), (.62, .49 if z < 0 else .98, .6), 90)
    for x in (-16, -9, -2):
        p.part("arcade", "stone", (x, .08, 20), (1.12, .98, .8))
    for z, y in ((-6, 3.07), (8, 6.07), (15, 6.07)):
        p.part("railing", "trim", (-22, y, z), (1.2, 1, 1), 90)
        p.collision("west promenade outer parapet", (-22, y+.55, z), (.18, 1.1, 7.2))
    for x in (-16, -9, -2):
        p.part("railing", "trim", (x, 6.07, 22.35), (1.16, 1, 1))
        p.collision("north overlook parapet", (x, 6.62, 22.35), (6.96, 1.1, .18))
    # Open-sided scholar pavilion at the end of the elevated pedestrian route.
    for x in (-10, -3):
        for z in (18.2, 21.6):
            p.part("column", "stone", (x, 6.07, z), (.45, .65, .45))
            p.collision("scholar pavilion column", (x, 8.02, z), (.4, 3.9, .4))
    p.part("rib_vault", "trim", (-6.5, 6.37, 19.9), (1.1, .9, .55))
    p.part("roof", "roof", (-6.5, 9.97, 19.9), (1.13, .65, .66))
    p.part("bench", "bark", (-6.5, 6.08, 21.6), (.8, .8, .7))
    p.part("monolith", "trim", (-3.8, 6.07, 20), (.4, .6, .4), 90)
    # Singular observatory skyline, next to the balcony rather than blocking it.
    p.part("observatory", "stone", (8.2, .1, 19.3), (.47, 1.1, .47))
    p.part("sky_ring", "metal", (8.2, 15.7, 19.3), (.7, .7, .7), euler=(22, 0, 16))
    p.part("crystal_cluster", "crystal", (8.2, 16.1, 19.3), (.8, 1.2, .8))
    for z in (-10, 2, 13):
        p.garden(19.1, z, "willow", .55, z*5)
    p.garden(15.5, 20, "cypress", .65)
    p.part("fountain", "stone", (19.1, .08, -18.8), (.65, .65, .65))
    p.part("sky_ring", "water", (19.1, 1.4, -18.8), (.35, .08, .35))
    p.part("bench", "bark", (15.2, .08, -20.5), (.7, .7, .7), 30)
    p.part("terrace", "stone", (19.1, 3.6, 13), (.52, .667, .52))
    p.part("root_arch", "bark", (19.1, .1, 12.5), (.5, .7, .55), 90)
    p.part("ore", "crystal", (19.3, .1, 20), (.8, .7, .8), 42)
    p.waypoint("eastern moonwell", (18, .07, -18.8))
    p.waypoint("willow habitat garden", (19, .07, 8))
    p.waypoint("decorative scholar resource outcrop", (19, .07, 19))
    return p


def grove():
    p = Layout("galehook", "Galehook, the Rootwoven Refuge", 2)
    base_court(p, "grove")
    p.part("root_arch", "bark", (0, .1, -15.4), (1.32, 1.45, 1))
    p.part("portal", "stone", (0, .1, -15.6), (1.3, .75, .8))
    p.part("root_arch", "bark", (0, .1, -21), (1.35, 1, .7))
    p.part("sky_ring", "metal", (0, 8.8, -15.4), (.55, .55, .55), euler=(90, 0, 0))
    # The two immense trunks stay behind the rear wall. Their branching crowns
    # overlap as a shelter, while combat and original module landings stay open.
    for side in (-1, 1):
        p.part("living_trunk", "bark", (side*7.4, .08, 18.6), (.8, 1.18, .47), side*6)
        p.part("living_canopy", "leaf", (side*7.4, .08, 18.6), (.8, 1.18, .47), side*6)
        p.part("root_arch", "bark", (side*15.7, .08, 1), (.7, 1.5, .8), 90)
        p.part("banner", "cloth", (side*5.4, 5.4, -15.8), (.75, 1.2, .75))
        p.part("guardian", "stone", (side*9.7, .08, -20.6), (.55, .65, .55), side*26)
        p.garden(side*19.2, -9, "willow", .53, side*28)
        p.garden(side*19.2, 8, "willow", .53, side*61)
        p.garden(side*19.2, 19, "cypress", .6)
        for z in (-18, -2, 15):
            p.part("rock", "stone", (side*20.4, .08, z), (.65, .75, .7), z*23)
            p.part("mushroom", "leaf", (side*18.3, .12, z+.8), (.8, .8, .8), z*17, True)
            p.part("reeds", "leaf", (side*20.4, .12, z+.7), (.8, 1.2, .8), fine=True)
            p.part("flower", "flower", (side*17.9, .12, z-1), (.7, .8, .7), fine=True)
        for z in (-8, 0, 8):
            p.part("vine", "leaf", (side*15.5, 5.4, z), (.8, 1.3, .8), side*90, True)
        p.part("market_awning", "cloth", (side*6.9, .12, -7), (.7, .7, .65), side*90)
    p.part("root_arch", "bark", (0, 4.4, 17.3), (1.65, 1.7, 1))
    p.part("crystal_cluster", "crystal", (0, 4.2, 19.3), (1.4, 2.2, .8))
    p.part("sky_ring", "metal", (0, 10, 18.6), (1.2, 1.2, 1.2), euler=(15, 0, 0))
    p.part("fountain", "stone", (0, .1, 20.6), (.7, .75, .7))
    p.part("sky_ring", "water", (0, 1.55, 20.6), (.4, .07, .4))
    p.part("ore", "crystal", (-19.4, .1, -20.5), (.75, .8, .75))
    p.waypoint("outer refuge garden", (19, .07, -2))
    p.waypoint("rootwoven rear sanctuary", (0, .07, 21))
    return p


def rift():
    p = Layout("sunscar", "Sunscar, the Fracture Engine Bastion", 3)
    base_court(p, "rift")
    p.part("portal", "roof", (0, .08, -15.7), (1.35, 1, 1))
    p.part("portal", "metal", (0, .08, -21), (1.3, .7, .7))
    p.part("sky_ring", "metal", (0, 8, -15.7), (.75, .75, .75), euler=(90, 0, 0))
    p.part("sigil", "crystal", (0, 8, -16), (1.4, 1.4, 1.4))
    for side in (-1, 1):
        p.part("buttress", "metal", (side*5.1, .08, -15.7), (1.1, 1.4, 1.1), side*14)
        p.part("banner", "cloth", (side*7.4, 6.4, -15.9), (1.2, 1.6, 1))
        for z in (-19.6, -8, 5, 18.5):
            p.part("rock", "roof", (side*19.7, .08, z), (.85, 1.2, .85), z*17)
            p.part("ore", "crystal", (side*19.7, .15, z), (.65, 1.0, .65), z*17)
            p.part("ward_pylon", "metal", (side*17.9, .08, z-1), (.55, .8, .55))
        for z in (-10, 2, 12):
            p.part("buttress", "roof", (side*16.2, 2.1, z), (1.15, 1.8, .8), side*90)
    # Asymmetric extraction yard, a tall fractured engine and a counterweight tower.
    p.part("drill", "metal", (-7.3, .1, 19.4), (1.35, 1.8, 1.05))
    p.part("crystal_cluster", "crystal", (-7.3, .1, 19.4), (1.35, 1.5, 1.05))
    p.part("tower", "roof", (8.3, .1, 19.4), (.9, 1.7, .85))
    p.part("spire", "metal", (8.3, 20.5, 19.4), (.7, .7, .7))
    p.part("sky_ring", "metal", (-7.3, 14.5, 19.4), (1.3, 1.3, .7), euler=(0, 0, 18))
    p.part("bridge", "metal", (.5, 12.2, 19.3), (.75, .85, 1.95), 90)
    p.part("ruin", "stone", (0, .1, 19.3), (1.1, 1.6, .75))
    for x in (-11, -3, 4, 12):
        p.part("crystal_cluster", "roof", (x, .1, 16.5), (.8, 1.8+abs(x)*.035, .75), x*9)
        p.part("crystal_cluster", "crystal", (x, 1.4, 16.3), (.24, 1.3, .24), x*9)
    for side in (-1, 1):
        p.part("market_awning", "cloth", (side*19.4, .1, -1), (.65, .8, .65), side*90)
        p.part("monolith", "metal", (side*8, .1, -20.4), (.7, 1, .7), side*30)
    p.waypoint("extraction outer yard", (-19, .07, 10))
    p.waypoint("decorative ore prospect", (19, .07, -19))
    return p


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    meta(OUT, True)
    for build in (home, grove, rift):
        build().save()


if __name__ == "__main__":
    main()
