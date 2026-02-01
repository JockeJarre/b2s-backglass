using System;
using System.Collections.Generic;
using System.Drawing;

namespace B2SBackglassServerEXE.Models
{
    /// <summary>
    /// Represents a complete .directb2s backglass file
    /// Based on official B2S file format specification
    /// </summary>
    public class BackglassData
    {
        public string FileName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public string DMDType { get; set; } = string.Empty;
        public Size BackglassSize { get; set; } = new Size(800, 600);
        
        // Background images
        public Image? BackgroundImage { get; set; }
        public Image? BackglassOnImage { get; set; }
        public Image? BackglassOffImage { get; set; }
        
        // DMD info
        public Point DMDLocation { get; set; } = Point.Empty;
        public Size DMDSize { get; set; } = new Size(128, 32);
        
        // Grill info
        public int GrillHeight { get; set; }
        public int SmallGrillHeight { get; set; }
        public bool DualBackglass { get; set; }
        
        // Collections
        public List<Illumination> Illuminations { get; set; } = new List<Illumination>();
        public List<Score> Scores { get; set; } = new List<Score>();
        public List<Animation> Animations { get; set; } = new List<Animation>();
        public List<Sound> Sounds { get; set; } = new List<Sound>();
        
        // Reel rolling interval
        public int ReelRollingInterval { get; set; } = 50;
        
        // Reel image storage (global for all reels)
        public ReelImageStorage ReelStorage { get; set; } = new ReelImageStorage();
        
        // Lookup dictionaries for fast access
        public Dictionary<int, Illumination> IlluminationsByID { get; set; } = new Dictionary<int, Illumination>();
        public Dictionary<string, Illumination> IlluminationsByName { get; set; } = new Dictionary<string, Illumination>();
        public Dictionary<string, Animation> AnimationsByName { get; set; } = new Dictionary<string, Animation>();
        public Dictionary<int, Score> ScoresByID { get; set; } = new Dictionary<int, Score>();
    }

    /// <summary>
    /// Represents a single illumination/bulb element
    /// </summary>
    public class Illumination
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Parent { get; set; } = "Backglass"; // "Backglass" or "DMD"
        
        // Position and size
        public Point Location { get; set; }
        public Size Size { get; set; }
        
        // ROM control
        public int RomID { get; set; }
        public int RomIDType { get; set; } // 0=Lamp, 1=B2SID, 2=Solenoid, 3=GIString
        public int RomIDValue { get; set; }
        public bool RomInverted { get; set; }
        
        // Visual properties
        public bool Visible { get; set; } = true;
        public int InitialState { get; set; } // 0=Off, 1=On
        public int Intensity { get; set; } = 100;
        public int DualMode { get; set; } // 0=Both, 1=Authentic, 2=Fantasy
        public int ZOrder { get; set; }
        
        // Images
        public Image? OnImage { get; set; }
        public Image? OffImage { get; set; }
        
        // Current state
        public bool IsOn { get; set; }
    }

    /// <summary>
    /// Represents an animation definition
    /// </summary>
    public class Animation
    {
        public string Name { get; set; } = string.Empty;
        public int Interval { get; set; } = 100;
        public List<AnimationStep> Steps { get; set; } = new List<AnimationStep>();
        
        // Runtime state
        public bool IsPlaying { get; set; }
        public int CurrentStep { get; set; }
    }

    /// <summary>
    /// Represents a single step in an animation
    /// </summary>
    public class AnimationStep
    {
        public int Duration { get; set; }
        public string[] Bulbs { get; set; } = Array.Empty<string>();
        public bool Visible { get; set; } = true;
    }

    /// <summary>
    /// Represents a sound file embedded in the backglass
    /// </summary>
    public class Sound
    {
        public string Name { get; set; } = string.Empty;
        public int ID { get; set; }
        public string Data { get; set; } = string.Empty; // Base64 encoded sound data
    }

    /// <summary>
    /// Represents a score display (player score, ball number, credits, etc.)
    /// </summary>
    public class Score
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Parent { get; set; } = "Backglass"; // "Backglass" or "DMD"
        public Point Location { get; set; }
        public Size Size { get; set; }
        public int Digits { get; set; }
        public int Spacing { get; set; }
        public int RollingDirection { get; set; } // 0=Down, 1=Up
        public int RollingInterval { get; set; } = 50;
        public string ReelType { get; set; } = string.Empty;
        public Color ReelLitColor { get; set; } = Color.White;
        public Color ReelDarkColor { get; set; } = Color.Black;
        public float Glow { get; set; }
        public float Thickness { get; set; }
        public float Shear { get; set; }
        public bool DisplayState { get; set; } = true; // false=hidden
        public int B2SStartDigit { get; set; }
        public int B2SScoreType { get; set; } // 0=NotUsed, 1=Scores, 2=Credits
        public int B2SPlayerNo { get; set; }
        public int ReelIlluImageSet { get; set; }
        public int ReelIlluB2SID { get; set; }
        public int ReelIlluB2SValue { get; set; }
        
        // Reel images storage - keyed by position (0-9, etc.)
        public Dictionary<string, Image> ReelImages { get; set; } = new Dictionary<string, Image>();
        public Dictionary<string, Image> ReelIntermediateImages { get; set; } = new Dictionary<string, Image>();
    }

    /// <summary>
    /// Global reel image storage (shared across all score displays)
    /// </summary>
    public class ReelImageStorage
    {
        public Dictionary<string, Image> ReelImages { get; set; } = new Dictionary<string, Image>();
        public Dictionary<string, Image> ReelIntermediateImages { get; set; } = new Dictionary<string, Image>();
        public Dictionary<string, Image> ReelIlluImages { get; set; } = new Dictionary<string, Image>();
        public Dictionary<string, Image> ReelIntermediateIlluImages { get; set; } = new Dictionary<string, Image>();
    }
}
