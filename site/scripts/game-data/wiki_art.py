"""Export the wiki's art from the installed game into public/img/wiki.

Usage (from the repo root):
    python -I scripts/game-data/wiki_art.py "%LOCALAPPDATA%/lfairclo/RowHero/RowHero_Data"
"""
import os, shutil, sys, tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import unity_extract  # noqa: E402

# Resources/PlayerIcons/<n> is the portrait of the rower or coach whose Rower.index is n.
ROWERS = {0: "nathaniel", 1: "theo-f", 2: "paul", 3: "theo-m", 4: "wilder", 5: "edward",
          6: "guy", 7: "jacapo", 8: "jonathan", 9: "alex", 10: "jamie", 11: "jordan",
          12: "casey", 13: "frankie", 14: "robin"}
COACHES = {20: "bobby", 21: "german", 22: "sam", 23: "dickie", 24: "donald", 25: "matt", 26: "charlie"}
ACHIEVEMENT_ICONS = ["charles", "river", "nat", "henley", "nobuy", "crew", "record", "note", "star",
                     "scared", "lightning", "gold", "infinite"]
TROPHIES = ["charles", "river", "nat", "henley"]


def main():
    data_dir = sys.argv[1]
    out = os.path.join(os.path.dirname(__file__), "..", "..", "public", "img", "wiki")
    os.makedirs(out, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        saved = unity_extract.extract(data_dir, tmp, ["playericons/", "achievements/", "trophies/"])
        wanted = {f"playericons/{i}": f"rower-{slug}.png" for i, slug in ROWERS.items()}
        wanted |= {f"playericons/{i}": f"coach-{slug}.png" for i, slug in COACHES.items()}
        wanted |= {f"achievements/{a}": f"ach-{a}.png" for a in ACHIEVEMENT_ICONS}
        wanted |= {f"trophies/{t}": f"trophy-{t}.png" for t in TROPHIES}
        for path, name in wanted.items():
            shutil.copyfile(saved[path], os.path.join(out, name))
    print(f"wrote {len(wanted)} images to {os.path.normpath(out)}")


if __name__ == "__main__":
    main()
