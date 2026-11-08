"""Export the wiki's art from the installed game into public/img/wiki.

Usage (from the repo root):
    python -I scripts/game-data/wiki_art.py "%LOCALAPPDATA%/lfairclo/RowHero/RowHero_Data"
"""
import os, shutil, sys, tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import unity_extract  # noqa: E402

# Resources/PlayerIcons/<n> is the portrait of the rower whose Rower.index is n.
ROWERS = ["nathaniel", "theo-f", "paul", "theo-m", "wilder", "edward",
          "guy", "jacapo", "jonathan", "alex", "sam", "jordan", "charlie", "frankie", "robin"]
ACHIEVEMENT_ICONS = ["charles", "river", "nat", "henley", "nobuy", "crew", "record", "star", "scared"]
TROPHIES = ["charles", "river", "nat", "henley"]


def main():
    data_dir = sys.argv[1]
    out = os.path.join(os.path.dirname(__file__), "..", "..", "public", "img", "wiki")
    os.makedirs(out, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        saved = unity_extract.extract(data_dir, tmp, ["playericons/", "achievements/", "trophies/"])
        wanted = {f"playericons/{i}": f"rower-{slug}.png" for i, slug in enumerate(ROWERS)}
        wanted |= {f"achievements/{a}": f"ach-{a}.png" for a in ACHIEVEMENT_ICONS}
        wanted |= {f"trophies/{t}": f"trophy-{t}.png" for t in TROPHIES}
        for path, name in wanted.items():
            shutil.copyfile(saved[path], os.path.join(out, name))
    print(f"wrote {len(wanted)} images to {os.path.normpath(out)}")


if __name__ == "__main__":
    main()
