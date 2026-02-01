# B2S.ComServer

This is a C# rewrite of the B2SBackglassServer.dll COM server, designed as a drop-in replacement for the original VB.NET implementation.

## Overview

The B2S.ComServer.dll is a high-performance COM server focused on low-latency handling of COM calls from Visual Pinball (VPX). It does **NOT** handle any GUI rendering - all visual operations are delegated to the B2SBackglassServerEXE.exe through efficient Windows Registry communication.

## Quick Start

```powershell
# Build the COM server
cd B2S.ComServer
msbuild B2S.ComServer.csproj /t:Rebuild /p:Configuration=Release

# Register for COM
regasm /codebase bin\Release\B2S.ComServer.dll
```

See [BUILD.md](BUILD.md) for detailed build and installation instructions.

## Key Features

- **Low Latency**: Optimized for minimal overhead in COM method calls
- **Registry-Based IPC**: Communicates with B2SBackglassServerEXE.exe via Windows Registry for maximum performance
- **Full COM Compatibility**: Implements all COM methods from the original B2SBackglassServer
- **Plugin Support**: Full MEF-based plugin architecture compatible with B2SServerPluginInterface
- **No GUI Dependencies**: Zero Windows Forms dependencies for faster loading and execution

## Architecture

```
VPX (Visual Pinball) 
    ↓ (COM calls)
B2S.ComServer.dll 
    ↓ (Registry communication)
B2SBackglassServerEXE.exe (handles all GUI)
```

## Files

- **Server.cs**: Main COM server class with all COM methods
- **IB2SServer.cs**: COM interface definition
- **RegistryHelper.cs**: Efficient registry communication helper
- **PluginHost.cs**: MEF-based plugin loader and manager
- **B2SVersionInfo.cs**: Version constants
- **BUILD.md**: Detailed build and installation instructions
- **README.md**: This file

## COM Registration

The assembly uses the following COM identifiers:
- **CLSID**: `09e233a3-cc79-457a-b49e-f637588891e5`
- **IID**: `5693c68c-5834-466d-aaac-a86922076efd`
- **ProgID**: `B2S.Server`

## Registry Communication

The COM server communicates with the EXE through the following registry keys under `HKEY_CURRENT_USER\Software\B2S`:

- `B2SLamps`: Lamp states (401 characters, '0' or '1')
- `B2SSolenoids`: Solenoid states (251 characters, '0' or '1')
- `B2SGIStrings`: GI String states (251 characters, '0' or '5')
- `B2SSetData`: General data (251 characters)
- `B2SAnimations`: Animation commands (name=state pairs)
- `B2SRotations`: Rotation state
- `B2SSounds`: Sound commands
- `B2SLEDXX`: LED values (XX = digit number)

## Plugin Support

The server loads plugins from the `Plugin` directory using MEF (Managed Extensibility Framework). Plugins must implement the `B2S.IDirectPlugin` interface from B2SServerPluginInterface.dll.

## Building

```bash
dotnet build B2S.ComServer.csproj
```

## Installation

1. Build the project
2. Copy `B2S.ComServer.dll` to your B2S installation directory
3. Register for COM interop:
   ```cmd
   regasm /codebase B2S.ComServer.dll
   ```

## Differences from Original

- **Language**: C# instead of VB.NET
- **No GUI**: All GUI code removed for performance
- **Modern .NET**: Uses .NET Framework 4.8 with modern C# language features
- **Nullable Reference Types**: Enabled for better null safety
- **Async-ready**: Timer uses System.Timers for better async patterns

## Performance Benefits

- Faster COM marshalling with optimized C# interop
- Reduced memory footprint (no GUI components loaded)
- Lower latency in data processing methods
- Efficient registry write operations using StringBuilder

## Compatibility

This DLL is a **drop-in replacement** for the original B2SBackglassServer.dll. It maintains the same COM interface and ProgID, ensuring existing VPX tables work without modification.

## Version Information

- **Version**: 2.1.6.999-comserver
- **Based on**: B2SBackglassServer 2.1.6

## License

Same license as the original B2S Backglass Server project.
