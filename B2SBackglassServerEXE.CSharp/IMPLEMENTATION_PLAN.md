# B2SBackglassServerEXE C# Rewrite - Phase 1 Plan

## Project Overview

This document outlines the architecture and implementation plan for rewriting B2SBackglassServerEXE.exe from VB.NET to C# while maintaining full compatibility with the existing B2S ecosystem.

## Scope Analysis

### VB.NET EXE Structure (Current)
Based on analysis of the codebase:

**Core Components:**
- EXEServer.vb - Main entry point, timer management
- formBackglass.vb - Main backglass rendering form (~3000+ lines)
- formDMD.vb - DMD display form
- formSettings.vb - Settings dialog  
- formMode.vb - Dual mode selector
- B2SScreen.vb - Screen/display management
- B2SLED.vb - LED rendering
- B2SAnimation.vb - Animation system
- B2SSettings.vb - XML settings handling

**Estimated Total:** 15,000+ lines of VB code

## Phase 1: WinForms C# Implementation

### Why WinForms First?

1. **Compatibility**: Direct port of existing functionality
2. **Testing**: Can validate against VB version
3. **Timeline**: Achievable in reasonable timeframe
4. **Migration Path**: Foundation for future OpenGL/3D

### Architecture Design

```
B2SBackglassServerEXE.CSharp/
├── Program.cs                    # Entry point
├── Core/
│   ├── B2SServer.cs             # Main server class
│   ├── RegistryMonitor.cs       # Registry change watcher
│   ├── SettingsManager.cs       # XML settings (B2STableSettings.xml)
│   └── BackglassLoader.cs       # .directb2s file parser
├── Forms/
│   ├── BackglassForm.cs         # Main backglass window
│   ├── DMDForm.cs               # DMD display
│   ├── SettingsForm.cs          # Settings dialog
│   └── ModeSelectionForm.cs     # Dual mode selection
├── Rendering/
│   ├── BackglassRenderer.cs     # Image layer composition
│   ├── LEDRenderer.cs           # LED display rendering
│   ├── AnimationEngine.cs       # Animation system
│   └── ImageCache.cs            # Image management
├── Models/
│   ├── BackglassData.cs         # directb2s XML model
│   ├── TableSettings.cs         # Settings model
│   ├── LampState.cs             # Lamp data structures
│   └── AnimationDefinition.cs   # Animation data
└── Utilities/
    ├── DpiHelper.cs             # DPI awareness
    ├── XmlHelper.cs             # XML parsing
    └── Win32Api.cs              # P/Invoke declarations
```

## Implementation Phases

### Phase 1.1: Foundation (2-3 hours)
**Goal**: Working executable that can be launched

- [x] Project setup (.NET Framework 4.8, WinForms)
- [ ] Program.cs entry point
- [ ] Command-line argument parsing
- [ ] Registry reading for GameName/B2SName
- [ ] Basic BackglassForm skeleton
- [ ] DPI awareness manifest

### Phase 1.2: Settings System (2-3 hours)
**Goal**: Read and apply B2STableSettings.xml

- [ ] SettingsManager class
- [ ] XML deserialization
- [ ] Settings model classes
- [ ] Default settings
- [ ] Settings dialog UI

### Phase 1.3: Backglass Loading (3-4 hours)
**Goal**: Load and display .directb2s files

- [ ] XML parser for .directb2s format
- [ ] Image extraction from embedded resources
- [ ] BackglassData model
- [ ] Basic image display
- [ ] Layer management

### Phase 1.4: Registry Communication (2-3 hours)
**Goal**: Receive commands from COM DLL

- [ ] RegistryMonitor with FileSystemWatcher pattern
- [ ] Registry value parsing (B2SLamps, B2SSolenoids, etc.)
- [ ] State update handlers
- [ ] Timer-based polling

### Phase 1.5: Rendering Engine (4-6 hours)
**Goal**: Display lamps, animations, LEDs

- [ ] Layer compositor
- [ ] Lamp state rendering
- [ ] Animation playback
- [ ] LED/DMD rendering
- [ ] Performance optimization (double buffering, dirty regions)

### Phase 1.6: Settings UI (3-4 hours)
**Goal**: Complete settings dialog

- [ ] Settings form design
- [ ] Tab pages for different categories
- [ ] Data binding
- [ ] Save/Load functionality
- [ ] Validation

### Phase 1.7: DMD Display (2-3 hours)
**Goal**: Separate DMD window

- [ ] DMD form
- [ ] Multi-monitor support
- [ ] Grill overlay
- [ ] Size/position persistence

### Phase 1.8: Polish & Testing (4-6 hours)
**Goal**: Production-ready

- [ ] Error handling
- [ ] Logging
- [ ] Memory leak testing
- [ ] Multi-table testing
- [ ] Performance profiling
- [ ] Documentation

**Total Estimated Time: 22-33 hours**

## Key Technical Decisions

### 1. Registry Monitoring

**Approach**: Use RegistryWatcher with polling fallback
```csharp
class RegistryMonitor
{
    private Timer _pollTimer;
    private string _lastLampsValue;
    
    public event EventHandler<LampChangedEventArgs> LampsChanged;
    
    public void StartMonitoring()
    {
        _pollTimer = new Timer();
        _pollTimer.Interval = 37; // ~27 FPS
        _pollTimer.Tick += PollRegistry;
        _pollTimer.Start();
    }
    
    private void PollRegistry(object sender, EventArgs e)
    {
        string currentValue = ReadRegistryValue("B2SLamps");
        if (currentValue != _lastLampsValue)
        {
            ProcessLampChanges(currentValue);
            _lastLampsValue = currentValue;
        }
    }
}
```

### 2. XML Parsing

**Approach**: XDocument for .directb2s, XmlSerializer for settings
```csharp
class BackglassLoader
{
    public BackglassData LoadFromFile(string filename)
    {
        var doc = XDocument.Load(filename);
        var data = new BackglassData
        {
            Name = doc.Root.Element("Name")?.Value,
            // ... parse all elements
        };
        return data;
    }
}
```

### 3. Image Rendering

**Approach**: Pre-render layers, composite on-demand
```csharp
class BackglassRenderer
{
    private Dictionary<string, Bitmap> _layerCache;
    private Bitmap _compositeBuffer;
    
    public void RenderFrame(Graphics g)
    {
        // Render active layers to composite buffer
        using (var bufferGfx = Graphics.FromImage(_compositeBuffer))
        {
            foreach (var layer in _activeLayers)
            {
                bufferGfx.DrawImage(layer.Image, layer.Position);
            }
        }
        
        // Blit to screen
        g.DrawImage(_compositeBuffer, Point.Empty);
    }
}
```

### 4. DPI Awareness

**Approach**: Per-monitor DPI with manifest + Win32 APIs
```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/PM</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </windowsSettings>
</application>
```

## Migration from VB: Key Differences

### Command-Line Parsing
**VB:**
```vb
If My.Application.CommandLineArgs.Count > 0 Then
    B2SData.TableFileName = My.Application.CommandLineArgs(0).ToString
End If
```

**C#:**
```csharp
static void Main(string[] args)
{
    if (args.Length > 0)
    {
        B2SData.TableFileName = args[0];
    }
}
```

### Registry Access
**VB:**
```vb
Using regkey As RegistryKey = Registry.CurrentUser.OpenSubKey("Software\B2S")
    B2SSettings.GameName = regkey.GetValue("B2SGameName", String.Empty)
End Using
```

**C#:**
```csharp
using (var regkey = Registry.CurrentUser.OpenSubKey("Software\\B2S"))
{
    B2SSettings.GameName = regkey?.GetValue("B2SGameName", string.Empty) as string ?? string.Empty;
}
```

### Timer Management
**VB:**
```vb
Private timer As Windows.Forms.Timer = Nothing
timer = New Windows.Forms.Timer
AddHandler timer.Tick, AddressOf Timer_Tick
timer.Interval = 37
timer.Start()
```

**C#:**
```csharp
private System.Windows.Forms.Timer _timer;
_timer = new System.Windows.Forms.Timer();
_timer.Tick += Timer_Tick;
_timer.Interval = 37;
_timer.Start();
```

## Compatibility Requirements

### Must Support:
1. ✅ Same command-line arguments as VB version
2. ✅ Read B2STableSettings.xml (exact format)
3. ✅ Parse .directb2s files (same XML schema)
4. ✅ Registry communication protocol
5. ✅ DMD display on separate monitor
6. ✅ DPI scaling behavior
7. ✅ Settings persistence

### Should Support:
- Dual mode (Authentic/Fantasy)
- Animations and rotations
- LED/Reel displays
- Sound playback
- Screenshot capture
- Test mode

### Could Support (Future):
- Plugin architecture
- Custom themes
- Enhanced logging
- Performance metrics

## Testing Strategy

### Unit Tests
- Settings XML parsing
- Registry value parsing
- Image manipulation
- State management

### Integration Tests
- Full backglass loading
- Registry communication
- Multi-monitor handling

### Manual Tests
- Various backglass files
- Different screen resolutions
- DPI scaling scenarios
- Settings combinations

## Performance Targets

| Metric | Target | VB Baseline |
|--------|--------|-------------|
| Startup Time | < 500ms | ~800ms |
| Frame Rate | 30+ FPS | 25-30 FPS |
| Memory Usage | < 100 MB | ~150 MB |
| CPU Usage (idle) | < 5% | ~8% |

## Known Challenges

### 1. Large Backglass Files
**Issue**: Some .directb2s files exceed 50MB
**Solution**: Lazy loading, image streaming, compression

### 2. Complex Animations
**Issue**: Multi-step animations with timing
**Solution**: Animation state machine, interpolation

### 3. Registry Polling Overhead
**Issue**: Frequent registry reads
**Solution**: Batch reads, change detection, caching

### 4. GDI+ Performance
**Issue**: WinForms rendering can be slow
**Solution**: Hardware acceleration hints, dirty regions, buffer reuse

## Future Enhancements (Phase 2)

### OpenGL/3D Rendering
When ready to implement:

**Option A: OpenTK**
```csharp
class OpenGLBackglassRenderer : GLControl
{
    protected override void OnLoad(EventArgs e)
    {
        GL.ClearColor(Color.Black);
        LoadTextures();
        SetupProjection();
    }
    
    protected override void OnPaint(PaintEventArgs e)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Render layers as textured quads
        foreach (var layer in _layers)
        {
            RenderTexturedQuad(layer.Texture, layer.Transform);
        }
        
        SwapBuffers();
    }
}
```

### Benefits of OpenGL:
- Hardware acceleration
- True 3D backglasses
- Better performance with many layers
- Advanced effects (particles, shaders)
- Can reference VPinball C++ code

## Conclusion

This plan provides a clear path to creating a C# replacement for B2SBackglassServerEXE.exe:

**Immediate**: Phase 1 WinForms implementation (20-30 hours)
**Future**: Phase 2 OpenGL/3D enhancement (20-40 hours)

The phased approach ensures:
1. Working replacement ASAP
2. Testable at each stage
3. Foundation for future enhancements
4. Minimal risk

Ready to proceed with implementation?
