# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Discord bot for the Pokémon Sleep community, built as an ASP.NET Core 8 web application (`.NET 8`, `net8.0`). It hosts HTTP endpoints alongside background workers that run continuously. The single solution is `Eevee.Sleep.sln` with one project: `Eevee.Sleep.Bot/`.

## Commands

```bash
# Run locally (Development environment — registers slash commands to the configured guild, not globally)
dotnet run --project Eevee.Sleep.Bot

# Build release artifact (win-x64, self-contained single file — matches CI)
dotnet publish Eevee.Sleep.Bot/Eevee.Sleep.Bot.csproj \
  --runtime win-x64 \
  --configuration Release \
  --self-contained true \
  -p:PublishSingleFile=true

# Restore packages
dotnet restore

# Build (debug)
dotnet build Eevee.Sleep.Bot
```

There are no automated tests in this project.

CI runs on Azure Pipelines (`.azure/pipeline.yml`) and triggers on non-draft PRs. It publishes a win-x64 self-contained single-file artifact named `Eevee.Sleep.Artifact`.

## Architecture

### Entry Point & DI Setup
[Program.cs](Eevee.Sleep.Bot/Program.cs) wires everything up: Discord client/services, MongoDB controllers (as singletons), and all background workers via `AddHostedService`. `BuildCommon()` / `BootAsync()` / `InitLogging()` extension methods in [InitializingExtensions.cs](Eevee.Sleep.Bot/Extensions/InitializingExtensions.cs) handle config initialization, MongoDB ping, and logging factory wiring. Slash commands register to the guild in Development; globally in Production.

### Configuration
All config lives in `appsettings.json` (template with `<PLACEHOLDER>` values). Access is always through [ConfigHelper.cs](Eevee.Sleep.Bot/Utils/ConfigHelper.cs) — never read `IConfiguration` directly. Key sections:
- `Discord`: token, guild ID, channel IDs, role IDs, role event config
- `Mongo.Url`: MongoDB connection string (default: `mongodb://localhost:23015/`)
- `Api.Internal`: inbound/outbound/Chester tokens, `GenerateActivation` endpoint
- `Discord.GameAnnouncement.ProxyUrlTemplate`: proxy URL for in-game announcement images

### Background Workers
All workers extend `BackgroundService`. Registered in `Program.cs`:

| Worker | Purpose |
|---|---|
| `DiscordClientWorker` | Connects the bot, initializes `InteractionHandler`, logs in |
| `OfficialSiteAnnouncementCrawlingWorker` | Polls pokemonsleep.net every 2 min to scrape announcement indexes + details |
| `OfficialSiteAnnouncementUpdateWatchingWorker` | Watches MongoDB change stream; sends Discord message when new/updated announcement |
| `InGameAnnouncementCrawlingWorker` / `InGameAnnouncementUpdateWatchingWorker` | Same pattern for in-game announcements |
| `ActivationCheckerWorker` | Validates subscriber activation codes |
| `ActivationKeyRemovalWatcher` / `ActivationDataRemovalWatcher` | Watch MongoDB change streams for activation removal events |
| `DiscordPaginationContextCleanupWorker` | Cleans up expired in-memory pagination states (3-min TTL) |
| `DiscordMessageSelfDestructWorker` | Deletes messages scheduled for auto-deletion |

**Crawling pattern**: `IAnnouncementCrawler` (max 3 retries, 10s delay between retries) → index scrape → detail scrape → upsert to MongoDB detail collection + insert to history collection. A `SemaphoreSlim(1,1)` ensures only one crawl runs at a time.

**Update watching pattern**: Uses MongoDB change streams (`WatchAsync`) on detail collections; triggers on Insert/Update/Modify/Replace.

### Discord Interaction Handling
[InteractionHandler.cs](Eevee.Sleep.Bot/Handlers/InteractionHandler.cs) wires all Discord events:
- Slash commands via Discord.Net `InteractionService` (modules auto-discovered via reflection)
- `ButtonExecuted` → [ButtonClickedHandler.cs](Eevee.Sleep.Bot/Handlers/ButtonClickedHandler.cs)
- `ModalSubmitted` → handled inline in `InteractionHandler` with `switch` on `ModalId`
- Guild events → dedicated handlers in `Handlers/EventHandlers/`

**Button/Modal IDs**: Stored as enums (`ButtonId`, `ModalId`, `ModalFieldId`). Custom IDs are serialized via [ButtonInteractionInfoSerializer.cs](Eevee.Sleep.Bot/Utils/ButtonInteractionInfoSerializer.cs) to carry payload (e.g., role ID). Enum values are parsed from Discord's `CustomId` string via `EnumExtensions`.

### Slash Command Modules
Located in `Modules/SlashCommands/`, each extends `InteractionModuleBase<SocketInteractionContext>`:
- `AdminSlashModule` — admin-only (`RequireUserPermission(Administrator)`): `role-event` command that opens a modal to bulk-create Pokémon roles with emotes, reactions, and announcements
- `RoleManagementSlashModule` — user-facing role display/add/remove with pagination (10 items/page, 3-min TTL)
- `RoleRestrictionSlashModule` — role restriction management
- `LotterySlashModule` — lottery functionality
- `CalcSlashModule` — calculator (uses Eval.net)
- `ExportSlashModule` — data export
- `BotSlashModule` — bot utility commands

### MongoDB Layer
`MongoConst` defines all collections across 3 databases:
- `auth`: `activation`, `activationKey`, `activationPreset`
- `discord`: `role/record`, `role/tracked`, `role/restricted`, `reactionRole`, `selfDestruct`
- `game`: `announcement/officialSite/index`, `.../details`, `.../history`, `announcement/inGame/...`, `currentVersion/chester`

Controllers in `Controllers/Mongo/` are plain classes (not ASP.NET controllers) that wrap collection operations. They are registered as singletons in DI.

### HTTP API Endpoints
ASP.NET controllers in `Controllers/` (not `Controllers/Mongo/`):
- `GET /game/announcement/{locale}` — list announcements
- `GET /game/announcement/{locale}/{announcementId}` — announcement detail
- `POST /subscribed-user` — subscriber management (inbound token auth)
- `POST /send-user-activation` — trigger activation code generation via internal API

### Pagination
`DiscordPaginationContext<T>` is an in-memory static store (keyed by Discord user ID string). State expires after 3 minutes. Pagination buttons (`PageNext`, `PagePrevious`) update the existing message in-place via `component.UpdateAsync`.

### Role Event Flow
`/admin role-event` → modal → `AdminSlashModule.ParseCsvEntries` + validation → `AdminSlashModule.ShowPreview` → confirm/cancel buttons → `RoleEventHelper.ExecuteRoleEvent` creates emotes, roles, reaction-role messages, and posts the announcement.
