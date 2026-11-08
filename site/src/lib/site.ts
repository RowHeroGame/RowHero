// Shared helpers: base-aware URLs and the small formatting rules the wiki repeats.
import { getCollection, type CollectionEntry } from "astro:content";

const BASE = import.meta.env.BASE_URL.replace(/\/$/, "");

/** A site path ("wiki/", "img/logo.png", "wiki/songs/") prefixed with the GitHub Pages base. */
export function url(path = ""): string {
  return `${BASE}/${path.replace(/^\//, "")}`;
}

export const DOWNLOAD_URL = "https://github.com/RowHeroGame/RowHero/releases/download/installer/RowHeroInstaller.exe";
export const CONTACT = "mailto:rowherogame@gmail.com";

export type Character = CollectionEntry<"characters">;
export type Event = CollectionEntry<"events">;
export type Song = CollectionEntry<"songs">;
export type Achievement = CollectionEntry<"achievements">;

export const characterUrl = (id: string) => url(`wiki/characters/${id}/`);
export const eventUrl = (id: string) => url(`wiki/events/${id}/`);
export const songUrl = (id: string) => url(`wiki/songs/#song-${id}`);
export const achievementUrl = (id: string) => url(`wiki/achievements/#ach-${id}`);

/** The wiki's four sections: the tiles on the wiki's home, each its own list page. */
export const SECTIONS = [
  { key: "characters", name: "Characters", href: url("wiki/characters/") },
  { key: "events", name: "Events", href: url("wiki/events/") },
  { key: "achievements", name: "Achievements", href: url("wiki/achievements/") },
  { key: "songs", name: "Songs", href: url("wiki/songs/") },
] as const;

export const SIDE_LABEL = { stroke: "Stroke side", bow: "Bow side", both: "Both sides" } as const;

export function cost(points: number): string {
  return points === 0 ? "Free" : `${points.toLocaleString("en-GB")} points`;
}

export function duration(seconds: number): string {
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`;
}

const escape = (s: string) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");

/** Escape text, then turn [label](path) into links to site pages. */
export function linkify(text: string): string {
  return escape(text).replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (_, label, path) => `<a href="${url(path)}">${label}</a>`);
}

/** Everything the wiki shows, sorted the way the game lists it. */
export async function wikiData() {
  const byName = (a: Character, b: Character) => a.data.name.localeCompare(b.data.name);
  const characters = await getCollection("characters");
  const events = (await getCollection("events")).sort((a, b) => a.data.order - b.data.order);
  const eventOrder = new Map(events.map((e) => [e.id, e.data.order]));
  const songs = (await getCollection("songs")).sort(
    (a, b) =>
      (eventOrder.get(a.data.regatta?.id ?? "") ?? 99) - (eventOrder.get(b.data.regatta?.id ?? "") ?? 99) ||
      (a.data.round ?? 0) - (b.data.round ?? 0),
  );
  const achievements = (await getCollection("achievements")).sort((a, b) => a.data.order - b.data.order);
  return {
    shopRowers: characters.filter((c) => c.data.kind === "shop").sort(byName),
    clubRowers: characters.filter((c) => c.data.kind === "club"), // game order
    characters,
    events,
    songs,
    achievements,
  };
}

/** Round-by-round line-up for one regatta; throws if songs.json and events.json disagree. */
export function lineUp(event: Event, songs: Song[]) {
  return event.data.rounds.map((kind, i) => {
    const song = songs.find((s) => s.data.regatta?.id === event.id && s.data.round === i + 1);
    if ((kind === "shop") !== !song) {
      throw new Error(`${event.id} round ${i + 1} is a ${kind} but ${song ? `has song ${song.id}` : "has no song"}`);
    }
    return { round: i + 1, kind, song };
  });
}
