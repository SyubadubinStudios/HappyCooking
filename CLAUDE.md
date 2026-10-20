# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Happy Cooking**: a 3D cooking adventure game for kids (5–12), built with .NET 10, Avalonia 12.1.2 and ThreeNet 0.12 (Vibe_Graphics). `solution-design.md` is the source of truth for features. `PLAN.md` is the roadmap and `Progress.md` the dev log (keep it updated). Docs live in `docs/en` and `docs/id`.

## Commands

```bash
dotnet build src/HappyCooking.Desktop                          # desktop app (+ Core, Data, App)
dotnet run --project src/HappyCooking.Desktop -- --demo         # mid-game profile, never saved
dotnet test tests/HappyCooking.Core.Tests                       # all tests
dotnet test tests/HappyCooking.Core.Tests --filter "FullyQualifiedName~CookingSessionTests"   # one class
dotnet build HappyCooking.slnx                                  # everything; needs the android + ios workloads
dotnet run --project src/HappyCooking.Desktop -- --screenshots docs/screenshots   # screenshot tour (EN+ID)
python tools/art/optimize_screenshots.py docs/screenshots        # PNG -> JPEG for docs
python tools/art/prepare_art.py                                  # 2D art: assets/art -> App/Assets/Images
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P tools/blender/process_assets.py   # 3D models
powershell -ExecutionPolicy Bypass -File installer/build-installer.ps1   # Windows installer (Inno Setup 6)
powershell -ExecutionPolicy Bypass -File installer/build-android.ps1 -KeyFile <keyfile>   # signed release APK (keystore kept outside the repo)
```

Other flags: `--skip-title`, `--db <path>`. Package versions are central in `Directory.Packages.props`.

## Architecture (big picture)

- **Core** (`src/HappyCooking.Core`, no UI): `GameSession` wires 12 systems (`Systems/`) to a `GameEvents` hub of `readonly record struct` events. Quests and achievements react to events. Systems own the rules; `PlayerProfile` is plain data. Time comes from `IClock` (tests use `ManualClock`).
- **Content** is embedded JSON in `Core/Content/Data`, loaded via a source-generated JSON context into `GameContent` (frozen dictionaries). `GameContent.Validate()` plus `ContentTests` check all cross-references, so run the tests after editing content.
- **UI strings** are in `strings.json` (`{en,id}`). Many keys are composed in code: `step.<verb>`, `action.<verb>`, `hint.<stepkind>`, `weather.<x>`, `rarity.<x>`, `pet.bonus.<x>`, `fishing.status|button.<phase>`, `cloud.<outcome>`, `friends.redeem.<error>`. Add keys in both languages when adding verbs or enum values. In XAML use `{svc:Tr key}`; in view models use `Loc.Instance`.
- **Data**: `SqliteSaveStore` (profile JSON per slot + cooking log) and `FolderCloudSaveProvider`/`CloudSync`. After a newer cloud save is downloaded, `GameHost.LockSaves()` stops the older in-memory profile from overwriting it.
- **App**: `GameHost.Current` is the singleton (session, autosave, Sfx, toasts). `ShellViewModel` navigates by creating a fresh `PageViewModel` each visit. Views are mapped by DataTemplates in `ShellView.axaml`. Each page creates its 3D `Stage` in `OnActivated` and disposes it in `OnDeactivated`, so only one scene is alive. `StageHost` wraps `ThreeNetView` and ticks `Stage.Update`; pages step game logic via `Stage.Updated`.
- **Mobile heads**: Android uses `AvaloniaAndroidApplication<App>` + non-generic `AvaloniaMainActivity` (Avalonia 12 API). iOS needs macOS to link and sign.

## Gotchas

- ThreeNet `Node.Visible` is **set-only**; track visibility yourself.
- Never give the `Window` a Width/Height larger than the screen. Avalonia keeps measuring at the requested size, and page content spills under the dock (`MainWindow.FitToScreen` handles this).
- Use `DockPanel` (header docked, body fills) for page bodies rather than Grid `*` rows, which overflowed in practice.
- Load GLBs through `ModelLibrary` (cached bytes; `LoadAnimated` for characters so each has its own clips). Clip names may carry Blender suffixes (`Idle.001`), and `CharacterActor` strips them.
- **Blender MCP**: never run `bpy.ops.wm.read_factory_settings`. It kills the MCP server.

## Project requirements (from `happy-cooking.txt`)

- Code should follow .NET best practices with attention to performance and memory efficiency (this is a real-time 3D game that must also run on mobile).
- Documentation must be complete and **bilingual: English and Bahasa Indonesia**.
- Include screenshots in the README and docs.
- Credit must appear in both the docs and the app itself: **"Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios"** (Made by Ariana Mischa Fadhila of Syubadubin Studios).
- Maintain `PLAN.md` (development roadmap) and `Progress.md` (development tracking). Keep `Progress.md` updated as work is completed.
- UI/UX should be polished and professional; use the project's `frontend-design` skill (`.claude/skills/frontend-design/`) when designing UI.
- Use **Blender MCP** for sculpting, rigging, and animating 3D models.
- Use **Rodin MCP** to generate 3D assets, images, and 2D art.
