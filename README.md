# Row Hero

The Row Hero launcher, the website and wiki at https://rowhero.uk, and the manifest the launcher
reads. Everything lives on `main`.

| Path | What it is |
| --- | --- |
| `manifest.json` | Versions and download links for the launcher, game and game data. Installed launchers read it from `main`, so it stays at the repo root on `main`. |
| `*.xaml`, `*.cs`, `Config/`, `Models/`, `Services/`, `Assets/`, `GameLauncher.csproj` | The launcher (WPF, .NET 8). The repo and branch it reads the manifest from are in `Config/AppConfig.cs`. |
| `src/`, `public/`, `astro.config.mjs`, `package.json` | The website and wiki (Astro). |
| `scripts/game-data/` | How the wiki's data and art were pulled from the game. |

The zips and installer that `manifest.json` links to are release assets (tags `launcher`, `game`,
`gamedata`, `installer`), not files in the repo.

## Website

The Row Hero site and wiki (https://rowhero.uk) is an [Astro](https://astro.build) site.
Pushing to `main` builds and publishes it with GitHub Actions (`.github/workflows/deploy-site.yml`).

```sh
npm install
npm run dev      # http://localhost:4321/
npm run build    # static site in dist/
```

- Pages: `src/pages` (home, privacy, terms, and the wiki under `src/pages/wiki`)
- Wiki data: `src/data/*.json`, checked against the schemas in `src/content.config.ts`
- Images, `SongConverter.html` and other files served as-is: `public/`
- How the wiki data was pulled from the game: `scripts/game-data`

`manifest.json` stays at the repo root: the launcher and the home page read it straight from GitHub.
