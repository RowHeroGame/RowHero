"""Build src/data/songs.json from the game's chart files.

Usage (from the repo root):
    python -I scripts/game-data/songs_json.py "%USERPROFILE%/AppData/LocalLow/lfairclo/RowHero/RowHero/Songs"

Titles, artists, difficulties, the regatta line-ups and the note-gap limits are transcribed from the
game code (Assembly-CSharp: SongCatalog, Charles/River/Nat/Henley, NoteGap). Note counts and lengths
are worked out from each song's chart (.json) and audio (.wav), the same way the game's free play menu
does: notes in normal mode (closely spaced notes in one lane removed), length from the audio file.
"""
import json, os, sys, wave

# Regatta -> song per round (None = shop). Round 6 is the boss. From Charles/River/Nat/Henley.songByRound.
REGATTAS = {
    "head-of-the-charles": ["Chamber Of Reflection", None, "A horse with no name", None, "Down Under", "Feel good inc"],
    "head-of-the-river": ["Could you be loved", "Brazil", None, "Gold", None, "Californication"],
    "nat-schools": ["Longview", None, "Highwayman", "Tangerine", None, "Little black submarines"],
    "henley": ["Fortunate son", None, "Lady hear me tonight", "Move on up", "Seventeen going under", "The chain"],
}
FREE_PLAY_ONLY = ["La bamba"]  # in the Songs folder but in no regatta: free play's "Other Songs"
# SecretBosses (Freebird, All along the watchtower, Ramble on) are left out on purpose.

DISPLAY = {  # SongCatalog.displayNames
    "a horse with no name": "A Horse With No Name", "down under": "Down Under", "feel good inc": "Feel Good Inc.",
    "tangerine": "Tangerine", "chamber of reflection": "Chamber of Reflection", "could you be loved": "Could You Be Loved",
    "highwayman": "Highwayman", "californication": "Californication", "la bamba": "La Bamba", "longview": "Longview",
    "gold": "Gold", "fortunate son": "Fortunate Son", "little black submarines": "Little Black Submarines",
    "brazil": "Brazil", "lady hear me tonight": "Lady (Hear Me Tonight)", "move on up": "Move on Up (Single Edit)",
    "seventeen going under": "Seventeen Going Under", "the chain": "The Chain",
}
ARTIST = {  # SongCatalog.defaultArtists
    "a horse with no name": "America", "feel good inc": "Gorillaz", "tangerine": "Led Zeppelin",
    "chamber of reflection": "Mac DeMarco", "down under": "Men at Work", "longview": "Green Day",
    "fortunate son": "Creedence Clearwater Revival", "la bamba": "Los Lobos", "gold": "Spandau Ballet",
    "could you be loved": "Bob Marley & The Wailers", "californication": "Red Hot Chili Peppers",
    "highwayman": "The Highwaymen", "brazil": "Declan McKenna", "move on up": "Curtis Mayfield",
    "seventeen going under": "Sam Fender", "the chain": "Fleetwood Mac", "lady hear me tonight": "Modjo",
    "little black submarines": "The Black Keys",
}
DIFFICULTY = {  # SongCatalog.defaultDifficulties
    "a horse with no name": "Easy", "down under": "Medium", "feel good inc": "Medium", "tangerine": "Hard",
    "chamber of reflection": "Easy", "could you be loved": "Medium", "highwayman": "Hard", "californication": "Hard",
    "la bamba": "Hard", "longview": "Medium", "gold": "Medium", "fortunate son": "Expert",
    "little black submarines": "Expert", "brazil": "Medium", "lady hear me tonight": "Expert",
    "move on up": "Expert", "seventeen going under": "Expert", "the chain": "Expert",
}
GAP_MS = {"feel good inc": 500, "californication": 300, "little black submarines": 500, "the chain": 500}  # NoteGap.SongMs
DEFAULT_GAP_MS = 500  # NoteGap.DefaultMs and every entry of NoteGap.RegattaMs


def slug(s):
    return "".join(c if c.isalnum() else "-" for c in s.lower()).strip("-").replace("--", "-")


def normal_note_count(chart, gap_ms):
    """NoteGap.Filter: drop a note if the previous kept note in the same lane is less than gap_ms earlier."""
    times, notes = chart["timings"], chart.get("notes")
    if not notes or len(notes) != len(times):
        return len(times)
    seconds_per_tick = 60 / chart["BPM"] / chart["timingResolution"]
    gap = max(gap_ms / 1000, 0.001) - 1e-6
    last, kept = {}, 0
    for tick, note in zip(times, notes):
        t, lane = tick * seconds_per_tick, (note[0] if note else "?")
        if lane in last and t - last[lane] < gap:
            continue
        last[lane] = t
        kept += 1
    return kept


def audio_seconds(path):
    with wave.open(path) as w:
        return w.getnframes() / w.getframerate()


def main():
    songs_dir = sys.argv[1]
    out = os.path.join(os.path.dirname(__file__), "..", "..", "src", "data", "songs.json")
    rows = [(regatta, rnd + 1, name) for regatta, line_up in REGATTAS.items() for rnd, name in enumerate(line_up) if name]
    rows += [(None, None, name) for name in FREE_PLAY_ONLY]
    songs = []
    for regatta, rnd, name in rows:
        key = name.lower()
        with open(os.path.join(songs_dir, name + ".json"), encoding="utf-8") as f:
            chart = json.load(f)
        songs.append({
            "id": slug(key),
            "title": DISPLAY[key],
            "artist": ARTIST[key],
            "regatta": regatta,
            "round": rnd,
            "difficulty": DIFFICULTY[key],
            "notes": normal_note_count(chart, GAP_MS.get(key, DEFAULT_GAP_MS)),
            "length": round(audio_seconds(os.path.join(songs_dir, name + ".wav"))),
        })
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(songs, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(f"wrote {len(songs)} songs to {os.path.normpath(out)}")


if __name__ == "__main__":
    main()
