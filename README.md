# theo-game-launcher
Launcher system for Theo's gamr

## Website

The Row Hero site and wiki (https://rowhero.uk) is an [Astro](https://astro.build) site.
Pushing to `master` builds and publishes it with GitHub Actions (`.github/workflows/deploy-site.yml`).

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
