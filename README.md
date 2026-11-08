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
`gamedata`, `installer`), not files in the repo. See [Releasing](#releasing).

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

## Launcher

WPF on .NET 8, so it builds on Windows only. `Assets/logo.png` and `Assets/menu_bg.png` aren't in
the repo; add them before building (see `Assets/PUT_YOUR_IMAGES_HERE.txt`).

```sh
dotnet publish GameLauncher.csproj -c Release -r win-x64 --self-contained true -o publish
```

## Releasing

Each download has its own fixed release tag. To ship a new version, replace the asset on that tag,
then raise the version (and changelog) in `manifest.json` on `main`. Installed launchers pick it up
the next time they open.

| Tag | Asset | What goes in it |
| --- | --- | --- |
| `launcher` | `GameLauncher.zip` | The contents of `publish/`. |
| `game` | `RowHero.zip` | The contents of the Unity build folder, with `RowHero.exe` at the top of the zip. Unpacked into `%LocalAppData%\lfairclo\RowHero`. |
| `gamedata` | `GameData.zip` | Unpacked into `%AppData%\lfairclo\RowHero`. |
| `installer` | `RowHeroInstaller.exe` | The home page's download button links straight to it; not in the manifest. |

```sh
gh release upload launcher GameLauncher.zip --clobber -R RowHeroGame/RowHero
```

For a new launcher, set `CurrentLauncherVersion` in `Config/AppConfig.cs` to the version you're
about to put in the manifest *before* you publish. A launcher whose own version is lower than the
manifest's downloads the update again every time it starts.
