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
        public List<Reel> Reels { get; set; } = new List<Reel>();
        public List<Animation> Animations { get; set; } = new List<Animation>();
        public List<Sound> Sounds { get; set; } = new List<Sound>();
        
        // Reel rolling interval
        public int ReelRollingInterval { get; set; } = 50;
        
        // Lookup dictionaries for fast access
        public Dictionary<int, Illumination> IlluminationsByID { get; set; } = new Dictionary<int, Illumination>();
        public Dictionary<string, Illumination> IlluminationsByName { get; set; } = new Dictionary<string, Illumination>();
        public Dictionary<string, Animation> AnimationsByName { get; set; } = new Dictionary<string, Animation>();
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
        public Point Location { get; set; }
        public Size Size { get; set; }
        public int DigitCount { get; set; }
        public int Spacing { get; set; }
        public int RollingDirection { get; set; } // 0=Down, 1=Up
        public List<ScoreDigit> Digits { get; set; } = new List<ScoreDigit>();
    }

    /// <summary>
    /// Represents a single digit image for a score display
    /// </summary>
    public class ScoreDigit
    {
        public int Value { get; set; } // 0-9
        public Image? Image { get; set; }
    }

    /// <summary>
    /// Represents an EM (electromechanical) reel
    /// </summary>
    public class Reel
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; } // 0=Normal, 1=Illuminated
        public Point Location { get; set; }
        public Size Size { get; set; }
        public int Spacing { get; set; }
        public int RollingDirection { get; set; } // 0=Down, 1=Up
        public int RollingInterval { get; set; } = 50;
        public List<ReelImage> Images { get; set; } = new List<ReelImage>();
        public List<ReelImage> IlluminatedImages { get; set; } = new List<ReelImage>();
    }

    /// <summary>
    /// Represents a single reel position image
    /// </summary>
    public class ReelImage
    {
        public int Value { get; set; }
        public Image? Image { get; set; }
    }
}
