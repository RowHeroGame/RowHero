// @ts-check
import { defineConfig } from "astro/config";

// Served by GitHub Pages at https://rowhero.uk (the custom domain is set in the repo's Pages settings).
export default defineConfig({
  site: "https://rowhero.uk",
  // "preserve" keeps privacy.astro -> privacy.html, so links already out in the world
  // (Google sign-in consent screen, the game's SongConverter link) keep their file names.
  build: { format: "preserve" },
  // Lossless whitespace handling (HTML rules), not Astro 7's default JSX rules, which drop the
  // spaces around inline elements that wrap onto a new line in hand-written copy.
  compressHTML: true,
});
