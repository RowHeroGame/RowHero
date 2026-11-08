# Row Hero

The Row Hero launcher, the website and wiki at https://rowhero.uk, and the manifest the launcher
reads. Everything lives on `main`. The game itself (the Unity project) is in a separate private repo.

| Path | What it is |
| --- | --- |
| `manifest.json` | Versions and download links for the launcher, game and game data. Installed launchers read it from the root of `main`, so it can't move. |
| `site/` | The website and wiki (Astro). |
| `launcher/` | The launcher (WPF, .NET 8). The repo and branch it reads the manifest from are in `launcher/Config/AppConfig.cs`. |

The zips and installer that `manifest.json` links to are release assets (tags `launcher`, `game`,
`gamedata`, `installer`), not files in the repo. See [Releasing](#releasing).

## Website

The Row Hero site and wiki (https://rowhero.uk) is an [Astro](https://astro.build) site in `site/`.
Pushing a change under `site/` to `main` builds and publishes it with GitHub Actions
(`.github/workflows/deploy-site.yml`).

```sh
cd site
npm install
npm run dev      # http://localhost:4321/
npm run build    # static site in site/dist/
```

- Pages: `site/src/pages` (home, privacy, terms, and the wiki under `site/src/pages/wiki`)
- Wiki data: `site/src/data/*.json`, checked against the schemas in `site/src/content.config.ts`
- Images, `SongConverter.html` and other files served as-is: `site/public/`
- How the wiki data was pulled from the game: `site/scripts/game-data`

`manifest.json` stays at the repo root: the launcher and the home page read it straight from GitHub.

## Launcher

WPF on .NET 8, so it builds on Windows only. `launcher/Assets/logo.png` and
`launcher/Assets/menu_bg.png` aren't in the repo; add them before building (see
`launcher/Assets/PUT_YOUR_IMAGES_HERE.txt`).

```sh
cd launcher
dotnet publish GameLauncher.csproj -c Release -r win-x64 --self-contained true -o publish
```

## Releasing

Each download has its own fixed release tag. To ship a new version, replace the asset on that tag,
then raise the version (and changelog) in `manifest.json` on `main`. Installed launchers pick it up
the next time they open.

| Tag | Asset | What goes in it |
| --- | --- | --- |
| `launcher` | `GameLauncher.zip` | The contents of `launcher/publish/`. |
| `game` | `RowHero.zip` | The contents of the Unity build folder, with `RowHero.exe` at the top of the zip. Unpacked into `%LocalAppData%\lfairclo\RowHero`. |
| `gamedata` | `GameData.zip` | Unpacked into `%AppData%\lfairclo\RowHero`. |
| `installer` | `RowHeroInstaller.exe` | The home page's download button links straight to it; not in the manifest. |

```sh
gh release upload launcher GameLauncher.zip --clobber -R RowHeroGame/RowHero
```

For a new launcher, set `CurrentLauncherVersion` in `launcher/Config/AppConfig.cs` to the version
you're about to put in the manifest *before* you publish. A launcher whose own version is lower than
the manifest's downloads the update again every time it starts.
