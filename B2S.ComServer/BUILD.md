# Building and Installing B2S.ComServer

## Prerequisites

- .NET Framework 4.8 Developer Pack
- MSBuild (comes with Visual Studio 2019+ or Build Tools)
- Windows x64 or x86

## Building

### Option 1: MSBuild Command Line

```powershell
# Navigate to the B2S.ComServer directory
cd B2S.ComServer

# Build Debug version
msbuild B2S.ComServer.csproj /t:Rebuild /p:Configuration=Debug

# Build Release version
msbuild B2S.ComServer.csproj /t:Rebuild /p:Configuration=Release
```

### Option 2: Visual Studio

1. Open `B2S.ComServer.csproj` in Visual Studio
2. Build the solution (Ctrl+Shift+B)

## Output

The compiled DLL will be located at:
- Debug: `B2S.ComServer\bin\Debug\B2S.ComServer.dll`
- Release: `B2S.ComServer\bin\Release\B2S.ComServer.dll`

## Installation

### Manual Installation

1. Copy `B2S.ComServer.dll` to your B2S Backglass Server installation directory
2. Register the COM server:
   ```cmd
   regasm /codebase B2S.ComServer.dll
   ```

### Uninstallation

```cmd
regasm /unregister B2S.ComServer.dll
```

## Plugin Support (Optional)

If you want plugin support, you need to build the B2SServerPluginInterface.dll first:

1. Clone the B2SServerPluginInterface repository:
   ```powershell
   git clone https://github.com/DirectOutput/B2SServerPluginInterface.git
   ```

2. Patch the project for .NET Framework 4.8:
   ```powershell
   $csproj = "B2SServerPluginInterface\B2SServerPluginInterface\B2SServerPluginInterface.csproj"
   (Get-Content $csproj) -replace '<TargetFramework>net40</TargetFramework>', '<TargetFramework>net48</TargetFramework>' | Set-Content $csproj
   ```

3. Build the interface:
   ```powershell
   msbuild B2SServerPluginInterface\B2SServerPluginInterface.sln /t:Rebuild /p:Configuration=Debug
   ```

4. Copy the DLL to the B2S plugin directory:
   ```powershell
   Copy-Item "B2SServerPluginInterface\B2SServerPluginInterface\bin\Debug\B2SServerPluginInterface.dll" "b2sbackglassserver\b2sbackglassserver\Plugin\"
   ```

5. Rebuild B2S.ComServer with plugin support:
   ```powershell
   cd B2S.ComServer
   msbuild B2S.ComServer.csproj /t:Rebuild /p:Configuration=Debug /p:DefineConstants="DEBUG;TRACE;COMSERVER;HAS_PLUGIN_INTERFACE"
   ```

## Testing

To test if the COM server is working:

1. Open a VBScript or PowerShell window
2. Create a COM object:
   ```vbscript
   Set b2s = CreateObject("B2S.Server")
   WScript.Echo b2s.B2SServerVersion
   ```
   
   Or in PowerShell:
   ```powershell
   $b2s = New-Object -ComObject "B2S.Server"
   $b2s.B2SServerVersion
   ```

3. You should see the version number: `2.1.6`

## Usage with Visual Pinball

The B2S.ComServer.dll is a drop-in replacement for B2SBackglassServer.dll. VPX tables using the following code will automatically use it:

```vbscript
Set Controller = CreateObject("B2S.Server")
Controller.B2SName = "YourBackglass"
Controller.Run GetPlayerHWnd
```

## Troubleshooting

### "Type library not registered"
- Make sure you ran `regasm /codebase B2S.ComServer.dll` as Administrator

### "Could not load file or assembly"
- Verify .NET Framework 4.8 is installed
- Check that all dependencies are in the same directory as the DLL

### "B2SBackglassServerEXE.exe not found"
- Make sure B2SBackglassServerEXE.exe is in the same directory as B2S.ComServer.dll
- Or place it in the current working directory

### Plugin not loading
- Plugins must be in a `Plugin` subdirectory relative to B2S.ComServer.dll
- Plugins must implement `B2S.IDirectPlugin` interface
- Check that B2SServerPluginInterface.dll is referenced correctly

## Performance Tips

- Use Release build for production (smaller, faster)
- The COM server uses registry-based IPC which is very fast
- Timer polling runs at ~27 FPS (37ms intervals)
- Registry writes are batched using StringBuilder for efficiency

## Differences from VB.NET Version

| Feature | VB.NET | C# ComServer |
|---------|--------|-------------|
| GUI Handling | In-process | Delegated to EXE |
| Compilation | VB Compiler | C# Roslyn |
| Size | ~150KB | ~28KB |
| Startup Time | Slower | Faster |
| Memory Usage | Higher (GUI loaded) | Lower (no GUI) |
| Plugin Architecture | MEF | MEF (same) |
| COM Interface | Same | Same |

## Advanced Configuration

### Custom Registry Paths

By default, the server uses `HKEY_CURRENT_USER\Software\B2S`. This can be modified in `RegistryHelper.cs`.

### Custom Timer Interval

The default polling interval is 37ms (27 FPS). Modify in `Server.cs`:

```csharp
_timer = new System.Timers.Timer(37); // Change this value
```

### Logging

Currently logging is minimal. To add logging, implement a logger in `RegistryHelper.cs` or `Server.cs`.
