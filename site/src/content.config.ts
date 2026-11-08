// Wiki data. Each collection is one JSON file in src/data; the schemas make the build fail
// if an entry is missing a field or points at a regatta that doesn't exist.
import { defineCollection, reference } from "astro:content";
import { file } from "astro/loaders";
import { z } from "astro/zod";

const characters = defineCollection({
  loader: file("src/data/characters.json"),
  schema: z.object({
    name: z.string(),
    kind: z.enum(["shop", "club"]), // club rowers are the free, no-perk NPCs every career starts with
    who: z.string(),
    side: z.enum(["stroke", "bow", "both"]),
    cost: z.number().int().nonnegative(),
    effect: z.string().nullable(),
    image: z.string(), // file in public/img/wiki
  }),
});

const events = defineCollection({
  loader: file("src/data/events.json"),
  schema: z.object({
    name: z.string(),
    order: z.number().int().min(1).max(4),
    realName: z.string(),
    location: z.string(),
    country: z.string(),
    month: z.string(),
    raceType: z.enum(["Head race", "Side by side"]),
    distance: z.string(),
    image: z.string(), // file in public/img
    trophy: z.string(), // file in public/img/wiki
    rounds: z.array(z.enum(["race", "shop", "boss"])).length(6),
  }),
});

const songs = defineCollection({
  loader: file("src/data/songs.json"),
  schema: z.object({
    title: z.string(),
    artist: z.string(),
    regatta: reference("events").nullable(), // null: free play only
    round: z.number().int().min(1).max(6).nullable(),
    difficulty: z.enum(["Easy", "Medium", "Hard", "Expert"]),
    notes: z.number().int().positive(), // normal mode
    length: z.number().int().positive(), // seconds
  }),
});

const achievements = defineCollection({
  loader: file("src/data/achievements.json"),
  schema: z.object({
    order: z.number().int(),
    name: z.string(),
    icon: z.string(), // file in public/img/wiki
    how: z.string(), // [text](path) links are resolved against the site root
  }),
});

export const collections = { characters, events, songs, achievements };
