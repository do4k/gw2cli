---
name: gw2cli
description: Query a Guild Wars 2 account via the gw2 CLI — characters, achievements, crafting recipes, trading post prices, WvW, PvP, masteries, and Wizard's Vault.
---

# GW2 CLI — Agent Skill Reference

Use the `gw2` CLI to answer questions about a Guild Wars 2 account. All commands assume the API key is stored (`gw2 auth set <key>`). Commands that need auth are marked 🔑.

---

## Installation

### Install this skill into Claude Code

```bash
npx skills add do4k/gw2cli
```

### Quick install (Linux / macOS)

```bash
curl -fsSL https://raw.githubusercontent.com/do4k/gw2cli/main/install.sh | bash
```

This script:
1. Detects your OS and architecture (linux/osx × x64/arm64)
2. Installs the .NET 11 runtime to `~/.dotnet` if not already present
3. Downloads the framework-dependent `gw2` binary from the latest GitHub release
4. Installs to `~/.local/bin/gw2`

Override install directory: `GW2CLI_INSTALL_DIR=/usr/local/bin bash install.sh`

### Manual download

Grab a binary from [GitHub Releases](https://github.com/do4k/gw2cli/releases):

| Binary | Description |
|---|---|
| `gw2-{rid}` | Self-contained — no .NET required, larger file (~60 MB) |
| `gw2-{rid}-fd` | Framework-dependent — requires .NET 11 runtime, smaller file (~1 MB) |

Where `{rid}` is one of: `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`, `win-x64`, `win-arm64`

### Install .NET 11 runtime manually

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- \
  --channel 11.0 --quality preview --runtime dotnet
```

### Windows

Download `gw2-win-x64.exe` (self-contained) or `gw2-win-x64-fd.exe` (framework-dependent) from [GitHub Releases](https://github.com/do4k/gw2cli/releases) and place it on your PATH.

---

## Authentication

```bash
gw2 auth set <api-key>       # Store API key (~/.gw2cli/config.json)
gw2 auth show                # Show masked stored key
gw2 auth clear               # Remove stored key
```

Generate an API key at https://account.arena.net → Applications → New Key  
Required permissions for full functionality: `account`, `characters`, `inventories`, `wallet`, `unlocks`, `progression`, `builds`, `guilds`, `trading post`

---

## Account 🔑

```bash
gw2 account                  # Account summary: world, AP, WvW rank, fractal level, expansions
gw2 account wallet           # All currency balances (gold, karma, laurels, etc.)
gw2 account bank             # Bank contents with item names and rarities
gw2 account inventory        # Shared inventory slots
gw2 account mounts           # Unlocked mounts
gw2 account achievements                          # Achievement progress by category
gw2 account achievements --category "Living World" # Filter categories by name
gw2 account achievements --in-progress           # Only show partially done categories
```

**Use for:** "What currencies do I have?", "What's in my bank?", "How many AP do I have?", "What expansions do I own?", "What mounts do I have?"

---

## Characters 🔑

```bash
gw2 characters                     # List all character names
gw2 characters "Charname"          # Character summary (race, profession, level, playtime, deaths)
gw2 characters equipment "Charname"  # Equipped gear with item names and rarities
gw2 characters skills "Charname"     # Skill bar (heal/utilities/elite) for PvE
gw2 characters skills "Charname" --mode pvp   # PvP skills
gw2 characters skills "Charname" --mode wvw   # WvW skills
gw2 characters crafting "Charname"   # Crafting disciplines and ratings
gw2 characters inventory "Charname"  # Character bag contents
```

**Use for:** "What are my characters?", "What is my Guardian wearing?", "What crafting disciplines does my Mesmer have?", "What level is my Thief?", "What skills is my character using?"

---

## Achievements

```bash
gw2 achievements daily              # Today's daily achievements with completion status 🔑
gw2 achievements daily --no-auth    # Daily names/requirements without account progress
gw2 achievements get <id>           # Full details for a specific achievement 🔑
gw2 achievements categories         # List all achievement categories with IDs
```

**Use for:** "What are today's dailies?", "Have I done my PvP daily?", "What are the requirements for achievement 1234?", "How many achievements are in the 'Story Journal' category?"

**Note on IDs:** The GW2 API uses numeric IDs. When looking up achievements, provide the ID. Category IDs can be found via `gw2 achievements categories`.

---

## Crafting

```bash
gw2 crafting recipe <recipe-id>          # Full recipe: ingredients, costs, profit estimate
gw2 crafting search <item-id>            # All recipes that produce this item
gw2 crafting used-in <item-id>           # All recipes that use this item as ingredient
```

**Important:** GW2 uses numeric item IDs. Common legendary items:
- Twilight (sword): item ID 30703
- The Bifrost (staff): item ID 30698  
- Bolt (sword): item ID 30699
- Eternity (greatsword): item ID 30704
- Sunrise (greatsword): item ID 30700
- The Dreamer (shortbow): item ID 30686

**Use for:** "How do I craft X?", "What items need Ectoplasm?", "What are the ingredients for recipe 3456?", "Is it profitable to craft item 12345?"

**Workflow for legendary crafting questions:**
1. `gw2 items get <item-id>` — confirm the item
2. `gw2 crafting search <item-id>` — find recipe(s)
3. `gw2 crafting recipe <recipe-id>` — get full ingredient list and costs
4. For each ingredient, repeat: `gw2 crafting search <ingredient-id>` to find sub-recipes

---

## Commerce / Trading Post

```bash
gw2 commerce price <item-id>             # Best buy order and sell listing with spread
gw2 commerce listings <item-id>          # Full order book (top 10 each side)
gw2 commerce listings <item-id> --limit 20  # Show top 20 listings
gw2 commerce exchange coins <copper>     # Convert coins → gems (e.g. 1000000 = 100g)
gw2 commerce exchange gems <gems>        # Convert gems → coins
gw2 commerce delivery                    # Pending TP pickup box 🔑
```

**Coin format:** 10000 copper = 1 gold. Pass raw copper value to exchange commands.  
`100g = 1000000 copper`, `1g = 10000`, `1s = 100`, `1c = 1`

**Use for:** "What's the current price of X?", "Is now a good time to sell my mats?", "How much does 400 gems cost in gold?", "What's in my TP delivery box?"

---

## Items

```bash
gw2 items get <item-id>                  # Item details: type, rarity, level, stats, TP price
```

**Use for:** "What is item 12345?", "What rarity is this item?", "What level do I need for this?"

---

## Common GW2 Item IDs Reference

### T6 Crafting Materials
- Vicious Claw: 26231
- Vicious Fang: 24351  
- Armored Scale: 24289
- Ancient Bone: 24358
- Elaborate Totem: 24300
- Powerful Venom Sac: 24283
- Primordial Orchid: 87645
- Glob of Ectoplasm: 19721

### Ascended Materials
- Dragonite Ore: 46733
- Empyreal Fragment: 46735
- Bloodstone Dust: 46731
- Obsidian Shard: 19925

### Legendary Precursors (example)
- Dusk (Twilight precursor): 28703
- Zap (Bolt precursor): 29167

---

## Workflow Examples

### "How do I craft a Legendary weapon?"
1. `gw2 account wallet` — check gold and materials
2. `gw2 crafting search <precursor-item-id>` — find precursor recipe
3. `gw2 crafting recipe <recipe-id>` — full ingredient list
4. `gw2 commerce price <ingredient-id>` — check material costs
5. `gw2 characters crafting "CharName"` — verify crafting discipline ratings

### "What should I farm today?"
1. `gw2 achievements daily` — see incomplete dailies
2. `gw2 account achievements --in-progress` — categories near completion
3. `gw2 account wallet` — current wealth

### "Is my gear up to date?"
1. `gw2 characters equipment "CharName"` — see current gear rarities
2. Look for non-Ascended/Legendary items in core slots
3. `gw2 crafting search <ascended-item-id>` — find upgrade recipes

---

## API Limitations

- **No text search:** Items and achievements are looked up by ID, not by name. Use the IDs reference above or the GW2 wiki to find IDs.
- **Rate limits:** The GW2 API rate-limits at ~600 requests/minute. Batch lookups are handled automatically.
- **Pagination:** Large result sets (bank slots, achievements) are fetched in full automatically.
- **Language:** All data returned in English (default). The API supports `en`, `es`, `de`, `fr`, `zh`.
