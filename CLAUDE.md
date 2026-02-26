# RetroCanvas

A C# library for creating 90s demoscene-style graphics and audio effects using SDL2.

## Build & Run

```bash
# Build
dotnet build

# Run interactive sample selector
dotnet run --project src/RetroCanvas.Samples

# Release build
dotnet build --configuration Release
```

**Prerequisites:** SDL2 native libraries
- Linux: `sudo apt install libsdl2-dev` (or distro equivalent)
- macOS: `brew install sdl2`
- Windows: bundled via Silk.NET

## Project Structure

```
src/
  RetroCanvas.Core/          # Low-level unsafe VGA + audio engine (Silk.NET/SDL2)
    Vga/                     # FrameBuffer (unsafe byte*), Clut, CrtcRegisters, VgaDriver
    Audio/                   # AudioDriver, SampleBuffer, Synthesis/
    Text/                    # CP437 text mode rendering
  RetroCanvas.Abstractions/  # High-level safe OOP layer
    DemoScene.cs             # Abstract base class — override lifecycle hooks
    DemoHost.cs              # Game loop orchestrator
    DemoHostConfig.cs        # Config record
  RetroCanvas.Samples/       # Five example demos (plasma, copper bars, synth, etc.)
```

## Tech Stack

- .NET 10.0, C# with nullable enabled, unsafe blocks allowed in Core
- **Silk.NET.SDL** v2.23.0 for SDL2 bindings
- **MinVer** for automatic versioning from git tags

## Architecture

Two-layer design:
1. **RetroCanvas.Core** — unsafe, direct SDL2, for advanced users
2. **RetroCanvas.Abstractions** — safe adapters, `DemoScene`/`DemoHost` lifecycle

**Frame loop order:**
1. `OnVerticalRetrace(IFrameBuffer, IClut)` — draw frame
2. `OnScanlineRetrace(line, IClut)` × N — copper-list per-scanline palette tricks
3. Flip buffers → upload texture → present

**Audio:** SDL2 audio thread calls back into `IAudioCallback`. All synthesis (Oscillator → Filter → Envelope) is per-sample and allocation-free using `Span<short>`.

## Key Conventions

- `FrameBuffer` uses `NativeMemory` and `byte*` pointers — keep unsafe code isolated to Core
- Adapters (`FrameBufferAdapter`, `ClutAdapter`) wrap Core types with safe interfaces
- Audio callbacks must be lock-free and allocation-free
- Use `Span<T>` in hot paths to avoid heap allocations
- `DemoHostConfig` is an immutable record with sensible defaults

## CI/CD

GitHub Actions (`.github/workflows/publish.yml`): restore → build → test → pack → publish. NuGet packages publish on `v*` tags using `NUGET_API_KEY` secret.
