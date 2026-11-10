# Game data scripts

How the wiki's data and art were pulled out of an installed copy of Row Hero (game 1.3.0; songs from game data 1.0.1).
You shouldn't need these unless the game's rowers, songs or art change.

Run them from `site/`. Paths below are the default install; swap in yours if it differs.

| Script | What it does |
| --- | --- |
| `wiki_art.py` | Saves the rower and coach portraits, achievement icons and trophies into `public/img/wiki`. Uses `unity_extract.py`. |
| `unity_extract.py` | Reads Unity 6 asset files (no type trees) and saves any `Resources/` texture as a PNG. |
| `silhouettes.py` | Traces the wiki home's section silhouettes from the game's sprites into `src/lib/silhouettes.json`. |
| `songs_json.py` | Writes `src/data/songs.json`: note counts (normal mode) and lengths from each song's chart and audio. |
| `ildump.ps1` | Disassembles `Assembly-CSharp.dll` to readable IL, which is where rowers, coaches, achievements, regatta line-ups and difficulties live. |
| `rvadump.ps1` | Prints the game's static arrays (regatta round layouts, note-gap limits) that `ildump.ps1` can't show. |

```sh
# Art
python -I scripts/game-data/wiki_art.py "$LOCALAPPDATA/lfairclo/RowHero/RowHero_Data"

# Songs
python -I scripts/game-data/songs_json.py "$USERPROFILE/AppData/LocalLow/lfairclo/RowHero/RowHero/Songs"

# Code, to check rowers, achievements and line-ups by hand (PowerShell 7)
pwsh -File scripts/game-data/ildump.ps1 "$LOCALAPPDATA/lfairclo/RowHero/RowHero_Data/Managed/Assembly-CSharp.dll" il.txt
pwsh -File scripts/game-data/rvadump.ps1 "$LOCALAPPDATA/lfairclo/RowHero/RowHero_Data/Managed/Assembly-CSharp.dll"
```

`characters.json`, `events.json` and `achievements.json` in `src/data` are edited by hand from the IL:
the `Rower` subclasses (name, effect, cost, side), `AvailableRowers` (the club rowers), `Coaches` (each
coach and what unlocks them), `Achievements.BuildList`, and `ReggadaData` (round layouts). Rowers and
coaches go by first name only. The three bonus-song achievements are secret in the game, so the wiki
shows them as ??? and describes them by boss order (`Bosses.playing` 1 to 3 credits `bonus_0` to `bonus_2`). The real-world details on each event (location, month, distance) aren't in the game.
