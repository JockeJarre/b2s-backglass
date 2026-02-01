# B2SBackglassServerEXE C# Rewrite

## ⚠️ Status: **In Development** - Foundation Phase

This is a C# rewrite of the B2SBackglassServerEXE.exe component, designed to be a drop-in replacement for the VB.NET version with the same functionality and better maintainability.

## Why Rewrite?

- **Modern codebase**: C# with latest language features
- **Better maintainability**: Clear separation of concerns
- **Future-ready**: Foundation for OpenGL/3D backglasses
- **Performance**: Optimized rendering and memory management
- **Same functionality**: 100% compatible with existing ecosystem

## Current Status

This project is in the **planning and foundation** stage. See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for the complete development roadmap.

### Completed
- ✅ Architecture design
- ✅ Implementation plan
- ⏳ Project skeleton (in progress)

### In Progress
- 📝 Core project structure
- 📝 Registry communication
- 📝 Settings system

### Planned
- ⏳ Backglass rendering
- ⏳ DMD display
- ⏳ Settings UI
- ⏳ Testing & validation

## Quick Start (When Complete)

```bash
# Build
msbuild B2SBackglassServerEXE.CSharp.csproj /p:Configuration=Release

# Run (same as VB version)
B2SBackglassServerEXE.exe "TableName" "0"
```

## Features (Target)

### Core Functionality
- ✅ Load .directb2s backglass files
- ✅ Display on primary or secondary monitor
- ✅ Registry-based communication with COM server
- ✅ B2STableSettings.xml support
- ✅ Dual mode (Authentic/Fantasy)
- ✅ DMD display window
- ✅ LED/Reel rendering

### Rendering
- ✅ Multi-layer image composition
- ✅ Lamp state animations
- ✅ Rotation and scaling
- ✅ DPI awareness (Per-Monitor V2)
- ✅ Hardware-accelerated drawing (where available)

### Configuration
- ✅ Settings dialog with all options
- ✅ XML-based persistence
- ✅ Test mode
- ✅ Screenshot capture

## Architecture

```
B2SBackglassServerEXE.CSharp/
├── Core/                # Business logic
├── Forms/               # UI forms
├── Rendering/           # Graphics engine
├── Models/              # Data structures
└── Utilities/           # Helpers
```

See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for detailed architecture.

## Compatibility

### Maintains Compatibility With:
- ✅ B2S.ComServer.dll / B2SBackglassServer.dll
- ✅ .directb2s file format
- ✅ B2STableSettings.xml format
- ✅ Registry communication protocol
- ✅ Command-line arguments
- ✅ Visual Pinball X tables

### Differences from VB Version:
- **Language**: C# instead of VB.NET
- **Codebase**: Cleaner, more maintainable
- **Performance**: Optimized rendering pipeline
- **Future**: Foundation for 3D/OpenGL (Phase 2)

## Development Timeline

**Phase 1** (Current): WinForms C# Implementation
- **Target**: Drop-in replacement with same features
- **Duration**: 20-30 hours development + testing
- **Framework**: .NET Framework 4.8, WinForms

**Phase 2** (Future): OpenGL/3D Enhancement
- **Target**: 3D backglasses, advanced effects
- **Duration**: 20-40 hours development + testing
- **Framework**: OpenTK or MonoGame

## Building

### Prerequisites
- .NET Framework 4.8 Developer Pack
- Visual Studio 2019+ or MSBuild

### Build Commands

```bash
# Debug
msbuild B2SBackglassServerEXE.CSharp.csproj /p:Configuration=Debug

# Release
msbuild B2SBackglassServerEXE.CSharp.csproj /p:Configuration=Release
```

## Testing

### Test Scenarios
1. ✅ Launch with various .directb2s files
2. ✅ Registry communication
3. ✅ Settings persistence
4. ✅ Multi-monitor setups
5. ✅ DPI scaling
6. ✅ Memory leak testing
7. ✅ Performance profiling

### Test Tables
- Use variety of backglass files (small, large, complex)
- Test with different resolutions
- Test Authentic vs Fantasy modes
- Test with/without DMD

## Contributing

This project welcomes contributions! Areas where help is needed:

- 🎨 **UI/UX**: Settings dialog design
- 🎮 **Testing**: Testing with various backglass files
- 📖 **Documentation**: Code comments, user guides
- 🔧 **Optimization**: Performance improvements
- 🎯 **Features**: Animation system, LED rendering

## License

Same as the B2S Backglass Server project.

## Roadmap

### v1.0 - Foundation (Current)
- [ ] Core functionality matching VB version
- [ ] Settings system
- [ ] Basic rendering

### v1.1 - Polish
- [ ] Performance optimizations
- [ ] Enhanced error handling
- [ ] Logging system

### v2.0 - 3D/OpenGL
- [ ] OpenGL rendering pipeline
- [ ] 3D backglass support
- [ ] Advanced visual effects
- [ ] Shader support

## FAQ

**Q: When will this be ready?**  
A: Phase 1 (WinForms version) target: 2-4 weeks development time

**Q: Will it work with my existing backglasses?**  
A: Yes! 100% compatible with .directb2s format

**Q: Can I use it now?**  
A: Not yet - still in development. Check back soon!

**Q: Why C# instead of staying with VB?**  
A: Better tooling, more maintainable, easier to find contributors, foundation for future enhancements

**Q: Will there be 3D backglasses?**  
A: Yes, planned for Phase 2 using OpenGL

## Links

- [Implementation Plan](IMPLEMENTATION_PLAN.md) - Detailed technical plan
- [B2S Backglass Server](https://github.com/vpinball/b2s-backglass) - Main project
- [VPinball Standalone](https://github.com/vpinball/vpinball/tree/standalone/standalone/inc/b2s) - C++ reference implementation

---

**Status**: 🚧 Under Development  
**Version**: 0.1.0-alpha  
**Last Updated**: 2026-01-31
