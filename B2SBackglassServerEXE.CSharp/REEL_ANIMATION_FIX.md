# Reel Animation Fix

## Issues Fixed

### 1. **Score Data Not Converting from Registry Correctly**
**Problem**: The `RegistryMonitor_DataChanged` handler was treating character data as ASCII values instead of converting characters to digits.
- Character '1' has ASCII value 49
- Should convert to digit value 1

**Fix**: Added proper char-to-digit conversion:
```csharp
// OLD (WRONG):
int value = (int)e.Data[dataIndex]; // ASCII value 49 for '1'

// NEW (CORRECT):
int value = e.Data[dataIndex] - '0'; // Digit value 1
```

### 2. **Reel Illumination Not Implemented**
**Problem**: Score displays have illuminated reel images (lit/unlit versions) that should change based on lamp states from registry, but this wasn't connected.

**Fix**: Added reel illumination handling in `RegistryMonitor_LampsChanged`:
```csharp
// Update reel illumination based on lamp states
foreach (var kvp in _scoreDisplays)
{
    var scoreDisplay = kvp.Value;
    var score = _backglassData.Scores.FirstOrDefault(s => s.ID == kvp.Key);
    
    if (score != null && score.ReelIlluB2SID > 0 && score.ReelIlluB2SID < e.States.Length)
    {
        bool shouldIlluminate = e.States[score.ReelIlluB2SID];
        scoreDisplay.SetIlluminated(shouldIlluminate);
    }
}
```

### 3. **Animation Timer Already Implemented**
The `ScoreDisplay` control already has:
- `SetValue(int digitIndex, int value)` - Sets target value for a digit and starts animation
- `SetScore(long score)` - Sets all digits from a score value
- Animation timer that increments digits until they reach target (spinning reel effect)
- `SetIlluminated(bool)` - Switches between normal and illuminated reel images

## How It Works

### Registry Communication Flow

1. **COM Server** (B2S.ComServer.dll) receives commands from VPinballX
2. **COM Server** writes data to registry:
   - `B2SLamps` - String of '0' and '1' for lamp states
   - `B2SSetData` - String of digit characters for scores/credits/etc.
3. **EXE** (B2SBackglassServerEXE.exe) polls registry every 37ms (~27 FPS)
4. **EXE** updates:
   - Illuminations based on lamp states
   - Score reels based on set data
   - Reel illumination based on lamp states

### Score Display Animation

When a digit value changes:
1. `SetValue()` or `SetScore()` sets the target value
2. Animation timer starts (50ms intervals, ~20 FPS)
3. Each tick, current value increments by 1 (wraps at 10)
4. When current == target, animation stops
5. This creates a "spinning reel" effect

### Reel Illumination

Each score display can have:
- **Normal reel images**: `ImportedEMR_T12_0` through `ImportedEMR_T12_9`
- **Illuminated reel images**: `ImportedEMR_T12_0_1` through `ImportedEMR_T12_9_1` (with set ID suffix)

When `SetIlluminated(true)` is called, the display switches to illuminated images (if available).

## Testing

To test reel animations:
1. Start the EXE with a .directb2s file containing score displays
2. Use a COM client or registry tool to write to `HKCU\Software\B2S\B2SSetData`
3. Set data like "00000123450000" - digits will animate to those values
4. Use `B2SLamps` to control illumination of reels

## Files Modified

- `Forms/BackglassForm.cs` - Fixed `RegistryMonitor_DataChanged` and added reel illumination in `RegistryMonitor_LampsChanged`
- `Controls/ScoreDisplay.cs` - Already had animation support, no changes needed
- `Core/RegistryMonitor.cs` - Already working correctly

## Related Documentation

- See `SCALING_ARCHITECTURE.md` for coordinate system details
- See `BUILD_STATUS.md` for implementation progress
