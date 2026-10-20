# Content guide

All game content lives in `src/HappyCooking.Core/Content/Data/*.json` and is embedded in the Core assembly. Adding content is a JSON edit, with no code change. Text fields are bilingual objects: `{ "en": "...", "id": "..." }`.

After any change, run:

```bash
dotnet test tests/HappyCooking.Core.Tests --filter "FullyQualifiedName~ContentTests"
```

`GameContent.Validate()` reports unknown ids (a recipe pointing at a missing ingredient, a quest with a bad target, and so on). The tests also play every recipe to make sure it can reach 3 stars.

## Recipes (`recipes.json`)

| Field | Meaning |
|---|---|
| `id` | Unique id, also used in quests and events |
| `name`, `description`, `funFact` | Bilingual text. The fun fact is shown after cooking and in the recipe book. |
| `region` | Region id |
| `difficulty` | 1–3, shown as dots |
| `unlockLevel` | Player level needed (the region must also be unlocked) |
| `coins`, `xp` | Base reward. 3 stars pays 100%, 2 stars 80%, 1 star 60%. |
| `color` | `#RRGGBB`, used for UI swatches and generic 3D dishes |
| `model` | GLB name in `App/Assets/Models` (without `.glb`), or `null` for a procedural dish |
| `creative` | `true` for imaginative dishes (Creative tab, trophy) |
| `ingredients` | Ingredient ids. One of each is used per cook. |
| `steps` | The mini-games, in order |

### Steps

| `kind` | Uses | Notes |
|---|---|---|
| `Tap` | `count` | Par time is 0.3 s per tap |
| `Heat` | `target`, `zone`, `speed` | The needle rises at `speed` per second. Finishing above `zone max + 0.15` burns the dish. |
| `Pour` | `target`, `zone` | Fills at 0.42 per second while held |
| `Rhythm` | `count`, `zone`, `speed` | The zone is centred. `speed` is marker cycles per second. |
| `Pick` | `items` | The right ingredients. 2–3 decoys are added automatically. |
| `Decorate` | `count` | Always full marks |

Every step needs a `verb`. The text shown is `step.<verb>` from `strings.json`, formatted with the step's `item` name (or the recipe name if `item` is empty). **When you add a new verb, add `step.<verb>` (and optionally `action.<verb>` for the button label) to `strings.json` in both languages.**

```json
{ "kind": "Heat", "verb": "fry", "item": "egg", "target": 0.6, "zone": 0.18, "speed": 0.4 }
```

## Other files

| File | Key fields |
|---|---|
| `ingredients.json` | `id`, `name`, `color`, `source` (`Market`/`Farm`/`Sea`), `price` |
| `regions.json` | `id`, `name`, `description`, `unlockLevel`, `color`, `host` (character id), `mapX`/`mapY` (0–1 position of the pin on the world map), `fish` (fish ids) |
| `characters.json` | `id`, `name`, `role`, `greeting`, `model`, `color`. A portrait is expected at `App/Assets/Images/Portraits/<id>.jpg`. |
| `crops.json` | `id`, `ingredient`, `seedPrice`, `growSeconds`, `yield`, `xp`, `color` |
| `fish.json` | `id`, `name`, `ingredient`, `rarity` (`Common`…`Legendary`), `nightOnly`, `coins`, `color` |
| `pets.json` | `id`, `name`, `description`, `bonus` (`Tips`, `CookingZone`, `Growth`, `FishLuck`, `Xp`, `Patience`), `amount` (0.25 = +25%), `price`, `priceInGems`, `unlockLevel`, `color`. A picture is expected at `Images/Pets/<id>.png`. |
| `quests.json` | `goal` (`CookRecipe`, `CookInRegion`, `CookAny`, `EarnStars`, `ServeCustomers`, `Harvest`, `CatchFish`, `FeedPet`, `AdoptPet`, `PlaceDecoration`, `ReachLevel`), `target`, `amount`, rewards, `requires` (the previous quest id, for story order), `giver` |
| `achievements.json` | `stat` (see `AchievementStat`), `threshold`, `rewardGems`, `icon` (name in `Images/Icons`) |
| `decorations.json` | `price`, `rating` (points; every 4 points adds a restaurant star), `shape` (`pot`, `lantern`, `plant`, `balloons`, `fountain`, `aquarium`, `rug`, `piano`), `color` |
| `events.json` | `startMonth`/`startDay`, `endMonth`/`endDay` (ranges may cross New Year), `coinMultiplier`, `featuredRecipes` |
| `strings.json` | UI text: `key → { en, id }`. A missing key shows the key itself, so gaps are easy to spot. |

## Dynamic string keys

Some keys are built in code. Keep them complete when you add enum values:

`step.<verb>`, `action.<verb>`, `hint.<stepkind>`, `weather.<sunny|cloudy|rain|snow>`, `rarity.<rarity>`, `pet.bonus.<bonus>`, `fishing.status.<phase>`, `fishing.button.<phase>`, `cloud.<outcome>`, `friends.redeem.<error>`.

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
