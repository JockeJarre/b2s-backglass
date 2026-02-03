# B2S Backglass Server C# - Scaling Architecture Documentation

## Critical Coordinate System Understanding

After extensive testing and iteration, the following coordinate system and scaling logic has been established:

### The Problem
The .directb2s file contains multiple coordinate systems that need to be understood and handled correctly:
1. **BackglassSize** (from `<BackglassSize>` XML element) - The "design canvas" size (e.g., 800x600)
2. **BackgroundImage actual size** - The embedded PNG image size (e.g., 1280x1024)
3. **Target Window size** - The actual display window size (e.g., 2560x2880 for 4K backglass screen)
4. **Element coordinates** (Illuminations, Scores, etc.) - Coordinates stored in XML

### The Solution - Coordinate System Rules

**RULE 1: XML Coordinates are relative to BackgroundImage size, NOT BackglassSize**

This is the critical insight that took many iterations to discover. When the VB version loads coordinates from the XML, those coordinates are ALREADY positioned relative to the background image size, not the BackglassSize.

**RULE 2: Scale Factor Formula**

```csharp
// CORRECT scaling for ALL elements (illuminations, scores, reels, etc.):
float scaleX = (float)WindowSize.Width / BackgroundImage.Width;
float scaleY = (float)WindowSize.Height / BackgroundImage.Height;

// Apply to XML position and size:
int scaledX = (int)(xmlX * scaleX);
int scaledY = (int)(xmlY * scaleY);
int scaledWidth = (int)(xmlWidth * scaleX);
int scaledHeight = (int)(xmlHeight * scaleY);
```

**RULE 3: BackglassSize is NOT used for coordinate scaling**

The BackglassSize element (e.g., 800x600) is only used for:
- Determining aspect ratio
- Legacy compatibility
- NEVER for coordinate transformation

### Why This Matters

Before understanding this:
- Used `WindowSize / BackglassSize` → WRONG, elements appeared too small and mispositioned
- Mixed different scale factors for different elements → WRONG, inconsistent rendering

After understanding this:
- Use `WindowSize / BackgroundImage.Size` → CORRECT, elements appear in the right place at the right size

### Implementation Pattern

**For Illuminations (BackglassForm.cs, OnPaint method):**

```csharp
float scaleX = (float)this.ClientSize.Width / _backglassData.BackgroundImage.Width;
float scaleY = (float)this.ClientSize.Height / _backglassData.BackgroundImage.Height;

int x = (int)(illumination.Location.X * scaleX);
int y = (int)(illumination.Location.Y * scaleY);
int w = (int)(illumination.Size.Width * scaleX);
int h = (int)(illumination.Size.Height * scaleY);

g.DrawImage(imageToRender, x, y, w, h);
```

**For Score/Reel Displays (ScoreDisplay constructor):**

```csharp
// Pass scale factors based on BackgroundImage
float scaleX = (float)windowSize.Width / backgroundImageSize.Width;
float scaleY = (float)windowSize.Height / backgroundImageSize.Height;

// Apply in constructor:
this.Location = new Point(
    (int)(scoreData.Location.X * scaleFactorX),
    (int)(scoreData.Location.Y * scaleFactorY)
);

this.Size = new Size(
    (int)(scoreData.Size.Width * scaleFactorX),
    (int)(scoreData.Size.Height * scaleFactorY)
);
```

**For Individual Reel Digit Rendering (ScoreDisplay.OnPaint):**

```csharp
// Each digit within a score display:
int digitX = (int)(baseX + (digitSpacing * scaleFactorX) * i);
int digitY = baseY;
int digitW = (int)(reelImage.Width * scaleFactorX);
int digitH = (int)(reelImage.Height * scaleFactorY);

g.DrawImage(reelImage, digitX, digitY, digitW, digitH);
```

### Common Mistakes to Avoid

❌ **WRONG:** Using BackglassSize for scaling
```csharp
// DON'T DO THIS:
float scale = windowSize.Width / backglassData.BackglassSize.Width;
```

❌ **WRONG:** Using pre-calculated _scaleFactor that was based on BackglassSize
```csharp
// DON'T DO THIS:
_scaleFactor = GetScaleFactor(targetSize, backglassFileSize);  // backglassFileSize is BackglassSize!
```

❌ **WRONG:** Different scale factors for different element types
```csharp
// DON'T DO THIS:
// Illuminations use one scale, reels use another
```

✅ **CORRECT:** Always use BackgroundImage size
```csharp
// DO THIS:
float scaleX = (float)windowSize.Width / backgroundImage.Width;
float scaleY = (float)windowSize.Height / backgroundImage.Height;
```

### Testing Verification

Use debug output to verify scaling:
```csharp
System.Diagnostics.Debug.WriteLine($"Background image size: {backgroundImage.Width}x{backgroundImage.Height}");
System.Diagnostics.Debug.WriteLine($"Window size: {windowSize.Width}x{windowSize.Height}");
System.Diagnostics.Debug.WriteLine($"Scale factor: {scaleX}x{scaleY}");
System.Diagnostics.Debug.WriteLine($"XML position: ({xmlX},{xmlY}), size: {xmlW}x{xmlH}");
System.Diagnostics.Debug.WriteLine($"Scaled position: ({scaledX},{scaledY}), size: {scaledW}x{scaledH}");
```

Example output for correct scaling:
```
Background image size: 1280x1024
Window size: 2560x2880
Scale factor: 2x2.8125
XML Loc=(528,164), Size=647x74
Scaled Loc=(1056,461), Size=1294x208
```

### Files That Must Use This Scaling

1. **BackglassForm.cs** - OnPaint method for illuminations
2. **ScoreDisplay.cs** - Constructor and OnPaint for reels/scores
3. **DMDForm.cs** - If rendering DMD elements
4. **Any future element types** - Animations, etc.

### Reference VB Code Behavior

The original VB code does this calculation implicitly through PictureBox controls that automatically scale based on their container's size. The key insight is that ALL elements in the .directb2s file are positioned and sized relative to the background image, not the BackglassSize element.

---

**Last Updated:** 2026-02-01
**Status:** Verified working for Illuminations and Reels
**Next Steps:** Apply same scaling to Animations, DMD elements when implemented
