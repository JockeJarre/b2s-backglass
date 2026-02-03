# B2S Backglass Scaling and Rendering Architecture

## Critical Understanding

After extensive analysis of the VB.NET codebase and testing, the scaling architecture works as follows:

### Coordinate Systems

1. **XML File Coordinates**: All positions and sizes in the .directb2s XML file are ALREADY relative to the BackgroundImage size (NOT the BackglassSize)
2. **BackglassSize**: The `<BackglassSize Width="X" Height="Y"/>` node is NOT used for scaling calculations - it's metadata only
3. **BackgroundImage**: The actual background PNG image size is the reference size for all elements
4. **Window/Display**: The final rendered window size (can be different resolution/aspect ratio)

### Scaling Formula

```
FINAL_POSITION = XML_POSITION * (WINDOW_SIZE / BACKGROUND_IMAGE_SIZE)
FINAL_SIZE = XML_SIZE * (WINDOW_SIZE / BACKGROUND_IMAGE_SIZE)
```

**Example:**
- Background image: 1280x1024
- Window size: 2560x2880 (for 4K second monitor)
- Scale factor: 2.0x2.8125
- XML illumination at (100, 50) size 80x60
- Final position: (200, 140.625) size 160x168.75

### Implementation Pattern

```csharp
float scaleX = (float)WindowWidth / BackgroundImageWidth;
float scaleY = (float)WindowHeight / BackgroundImageHeight;

int finalX = (int)(xmlX * scaleX);
int finalY = (int)(xmlY * scaleY);
int finalWidth = (int)(xmlWidth * scaleX);
int finalHeight = (int)(xmlHeight * scaleY);
```

### Apply This to ALL Visual Elements

- ✅ Background images (stretched to window size)
- ✅ Illuminations (lamps)
- ✅ Score reels/displays
- ⚠️ Animations (TODO)
- ⚠️ DMD displays (TODO)

### Common Mistakes to Avoid

❌ Do NOT scale by BackglassSize
❌ Do NOT use two-step scaling (XML→BackgroundImage→Window)
❌ Do NOT apply DPI scaling on top of this (handled by manifest)

✅ DO use single-step scaling: XML → Window
✅ DO use BackgroundImage size as the reference
✅ DO apply the same formula to positions AND sizes

## Rendering Logic - Illuminations

### State-Based Rendering

Illuminations have three states:
1. **IsOn = true**: Show OnImage (if exists)
2. **IsOn = false**: Show OffImage (if exists), otherwise don't render
3. **No images**: Don't render

### Critical Fix (2026-01-31)

**WRONG** (Old code):
```csharp
if (!illumination.Visible) continue;  // ❌ This skips lamps that are off!
```

**CORRECT** (New code):
```csharp
// Determine which image to show
if (illumination.IsOn && illumination.OnImage != null)
    imageToRender = illumination.OnImage;
else if (!illumination.IsOn && illumination.OffImage != null)
    imageToRender = illumination.OffImage;

if (imageToRender == null) continue;  // ✅ Only skip if no image available
```

This ensures lamps with OffImage are visible even when IsOn=false, allowing them to "blink" when the state changes.

## Test Cases

1. **1-2-3 (Automaticos 1973).directb2s**
   - Background: 1280x1024
   - Score reel 1: XML=(528,164) size 647x74
   - On 4K display (2560x2880): Should be at (844, 279) size 1035x126
   
2. Verify illuminations match background positions visually

3. **Test Mode** (Press 'T' key): Toggles all lamps to verify blinking works
