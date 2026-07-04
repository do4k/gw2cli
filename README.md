# gw2cli

Guild Wars 2 command-line interface. Wraps the [GW2 API v2](https://wiki.guildwars2.com/wiki/API:2) to query your account, characters, achievements, crafting recipes, and trading post — directly from the terminal.

```
gw2 account wallet
gw2 characters "My Mesmer" equipment
gw2 achievements daily
gw2 crafting recipe 7318
gw2 commerce price 19721
```

## Features

- **Account** — summary, wallet, bank, shared inventory, mounts, achievement progress
- **Characters** — list, gear, skill builds (PvE/PvP/WvW), crafting disciplines, bags
- **Achievements** — daily completions with account progress, category breakdown, per-achievement detail
- **Crafting** — recipe lookup with live ingredient costs and profit estimate, search by output or input item
- **Commerce** — best buy/sell prices, full order book, coin↔gem exchange, TP delivery box
- **Items** — item detail by ID (rarity, level, stats, TP price)
- **API key context** — stored once in `~/.gw2cli/config.json`, no flag needed on every command
- **Rich output** — coloured tables, rarity-coloured item names, gold/silver/copper formatting

## Installation

### Pre-built binaries

### Quick install (Linux / macOS)

```bash
curl -fsSL https://raw.githubusercontent.com/do4k/gw2cli/main/install.sh | bash
```

Detects your platform, installs the .NET 11 runtime if needed, and downloads the framework-dependent binary to `~/.local/bin/gw2`.

### Manual download

Download from [Releases](https://github.com/do4k/gw2cli/releases).

Two variants per platform:

| Variant | Suffix | Size | Requirement |
|---|---|---|---|
| Self-contained | `gw2-{rid}` | ~60 MB | None |
| Framework-dependent | `gw2-{rid}-fd` | ~1 MB | .NET 11 runtime |

Platforms: `osx-arm64`, `osx-x64`, `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`

```bash
# macOS / Linux — self-contained example
chmod +x gw2-osx-arm64
sudo mv gw2-osx-arm64 /usr/local/bin/gw2
```

### Build from source

Requires [.NET 11 SDK (preview)](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/do4k/gw2cli
cd gw2cli
dotnet run --project src/GW2CLI -- --help

# Self-contained binary (no runtime needed)
dotnet publish src/GW2CLI -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o out/

# Framework-dependent binary (smaller, requires .NET 11 runtime)
dotnet publish src/GW2CLI -c Release -r osx-arm64 --self-contained false -p:PublishSingleFile=true -o out/
```

## Setup

Generate an API key at **[account.arena.net](https://account.arena.net) → My Account → Applications → New Key**.

Recommended permissions: `account`, `characters`, `inventories`, `wallet`, `unlocks`, `progression`, `builds`, `trading post`

```bash
gw2 auth set YOUR_API_KEY_HERE
```

The key is saved to `~/.gw2cli/config.json`. Override per-command with `--api-key` / `-k`.

## Command Reference

### Auth

```bash
gw2 auth set <key>    # Store API key
gw2 auth show         # Show masked key
gw2 auth clear        # Remove key
```

### Account

```bash
gw2 account                                    # Summary: world, AP, WvW rank, expansions
gw2 account wallet                             # All currencies
gw2 account bank                               # Bank contents (item names + rarities)
gw2 account inventory                          # Shared inventory slots
gw2 account mounts                             # Unlocked mounts
gw2 account achievements                       # Progress by category
gw2 account achievements --category "Living"   # Filter by category name
gw2 account achievements --in-progress         # Only partially-done categories
```

### Characters

```bash
gw2 characters                          # List all characters
gw2 characters "Name"                   # Character summary
gw2 characters equipment "Name"         # Equipped gear
gw2 characters skills "Name"            # PvE skill bar
gw2 characters skills "Name" --mode pvp # PvP skills
gw2 characters skills "Name" --mode wvw # WvW skills
gw2 characters crafting "Name"          # Crafting disciplines + ratings
gw2 characters inventory "Name"         # Bag contents
```

### Achievements

```bash
gw2 achievements daily            # Today's dailies with completion status
gw2 achievements daily --no-auth  # Dailies without API key (no completion status)
gw2 achievements get <id>         # Full achievement detail
gw2 achievements categories       # List all categories with IDs
```

### Crafting

```bash
gw2 crafting recipe <id>        # Recipe: ingredients, costs, profit
gw2 crafting search <item-id>   # All recipes producing this item
gw2 crafting used-in <item-id>  # All recipes using this item as ingredient
```

> Items are identified by numeric ID — use the [GW2 wiki](https://wiki.guildwars2.com) or `gw2 items get` to look them up.

### Commerce

```bash
gw2 commerce price <item-id>             # Best buy/sell with spread and post-tax
gw2 commerce listings <item-id>          # Full order book (top 10 each side)
gw2 commerce listings <item-id> --limit 20
gw2 commerce exchange coins <copper>     # Coins → gems  (e.g. 1000000 = 100g)
gw2 commerce exchange gems <gems>        # Gems → coins
gw2 commerce delivery                    # Pending TP pickup
```

### Items

```bash
gw2 items get <item-id>    # Item detail: type, rarity, level, vendor value, TP price
```

## Common Item IDs

| Item | ID |
|---|---|
| Glob of Ectoplasm | 19721 |
| Orichalcum Ore | 19697 |
| Ancient Wood Log | 19722 |
| Vicious Claw (T6) | 26231 |
| Vicious Fang (T6) | 24351 |
| Armored Scale (T6) | 24289 |
| Ancient Bone (T6) | 24358 |
| Dragonite Ore | 46733 |
| Empyreal Fragment | 46735 |
| Bloodstone Dust | 46731 |

## Agent / AI Use with Claude Code

`SKILL.md` teaches Claude Code all gw2cli commands so you can ask questions like "how do I craft a legendary?" or "what dailies haven't I done?" in natural language.

### Install Claude Code

```bash
npm install -g @anthropic-ai/claude-code
```

### Add the gw2cli skill

```bash
git clone https://github.com/do4k/gw2cli ~/.claude/skills/gw2cli
```

Claude Code auto-discovers skills from `~/.claude/skills/`. Once cloned, start a session and ask:

- "What characters do I have and what level are they?"
- "How do I craft Twilight?"
- "What's the current price of Glob of Ectoplasm and is it worth selling?"
- "Which of my dailies are incomplete?"
- "What masteries am I missing for my account?"

The skill reference (`SKILL.md`) lists all commands, common item IDs, and multi-step workflows. Pair with `gw2 auth set <key>` so Claude can query live account data.

## CI / Releases

GitHub Actions builds self-contained binaries for all 6 targets on every `v*` tag push:

```bash
git tag v1.0.0 && git push origin v1.0.0
```

## License

MIT
