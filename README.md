# PixelForge - 2D RPG Engine & Editor (Prototype)

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Status](https://img.shields.io/badge/status-Prototype-yellow)](docs/)

**PixelForge** is an early-stage 2D RPG engine and editor built in C#.
The runtime uses **MonoGame**, while the editor is a **cross-platform Avalonia** desktop app.
This repository includes the engine runtime library, editor tooling, shared data models, and an experimental Claude-powered code generation library.

## 🎮 Project Status

PixelForge is in active development. The codebase contains working implementations for core engine systems and editor windows, but some features are still iterative or not fully integrated.
There is **no standalone game runtime project** yet—use the editor to create project data or embed the engine library in your own host.

## 🚀 Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- (Optional) Anthropic API key for VibeCode experiments

### Build & Run

```bash
# Clone the repository
git clone https://github.com/yourusername/PixelForge.git
cd PixelForge

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Launch the editor
dotnet run --project src/PixelForge.Editor/PixelForge.Editor.csproj
```

### Run Tests

```bash
dotnet test
```

## ✨ What’s Implemented Today

### Engine Runtime (PixelForge.Engine)
- MonoGame game loop (`GameEngine`) with input and resource management
- JSON-backed map loading/saving with multi-layer tile rendering
- Event interpreter with commands like messages, choices, variables/switches, transfers, audio playback, battle/shop hooks, and scripting
- Battle controllers: turn-based, ATB, and action-economy modes
- RPG systems: party management, inventory, shops, save/load, quests, dialogue
- In-game UI: message windows, battle menus, minimap, and main menu suite (inventory, skills, equipment, status, quest log, save/load, shop)

### Editor Tooling (PixelForge.Editor)
- Project creation, load/save, and recent projects
- Map editor with layers, tileset selection, paint/erase tools, grid toggle, and zoom
- Database editor for actors, classes, skills, items, weapons, armors, enemies, troops, states, quests, tilesets, animations, system config, and common events
- Event editor for map events and command pages
- Dialogue editor and string editor
- Script editor (C#)
- Asset browser with import/delete/preview for graphics and audio categories
- Export tool (dotnet publish wrapper + optional archive creation)
- Plugin manager (tracks enabled plugins via `plugins.json`)
- Project settings window

### VibeCode (PixelForge.VibeCode)
- Claude API client, project context builder, and code generator
- Currently a standalone library; not wired into the editor UI yet

## 📁 Repository Structure

```
PixelForge/
├── src/
│   ├── PixelForge.Engine/              # Game engine runtime (MonoGame)
│   ├── PixelForge.Editor/              # Avalonia editor application
│   ├── PixelForge.Shared/              # Shared data models
│   └── PixelForge.VibeCode/            # Claude API integration library
├── tests/                              # Engine/editor tests
├── docs/                               # Guides and reference docs
└── samples/                            # Sample content (if any)
```

## 📚 Documentation

- Getting Started: [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md)
- API Reference: [docs/API_REFERENCE.md](docs/API_REFERENCE.md)
- Vibe Coding: [docs/VIBE_CODING.md](docs/VIBE_CODING.md)

> Note: Some docs describe planned functionality and may not fully match the current implementation.

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**PixelForge - A C#-powered RPG engine + editor in progress**

[Documentation](docs/) • [Getting Started](docs/GETTING_STARTED.md) • [API Reference](docs/API_REFERENCE.md)

</div>
