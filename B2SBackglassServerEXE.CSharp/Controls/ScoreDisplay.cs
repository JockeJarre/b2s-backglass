using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using B2SBackglassServerEXE.Models;

namespace B2SBackglassServerEXE.Controls
{
    /// <summary>
    /// Displays a score/reel display (player scores, credits, ball number, etc.)
    /// </summary>
    public class ScoreDisplay : Control
    {
        private Score _scoreData;
        private ReelImageStorage _reelStorage;
        private int[] _currentValues;
        private bool _illuminated = false;
        private float _scaleFactor = 1.0f;
        
        public ScoreDisplay(Score scoreData, ReelImageStorage reelStorage, float scaleFactor)
        {
            _scoreData = scoreData;
            _reelStorage = reelStorage;
            _scaleFactor = scaleFactor;
            _currentValues = new int[scoreData.Digits];
            
            // Set control properties
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            
            // Calculate total width based on digit count and spacing
            int totalWidth = (scoreData.Size.Width + scoreData.Spacing) * scoreData.Digits;
            this.Size = new Size(
                (int)(totalWidth * scaleFactor),
                (int)(scoreData.Size.Height * scaleFactor)
            );
            this.Location = new Point(
                (int)(scoreData.Location.X * scaleFactor),
                (int)(scoreData.Location.Y * scaleFactor)
            );
            
            this.Visible = scoreData.DisplayState;
        }
        
        public void SetIlluminated(bool illuminated)
        {
            if (_illuminated != illuminated)
            {
                _illuminated = illuminated;
                this.Invalidate();
            }
        }
        
        public void SetValue(int digitIndex, int value)
        {
            if (digitIndex >= 0 && digitIndex < _currentValues.Length)
            {
                if (_currentValues[digitIndex] != value)
                {
                    _currentValues[digitIndex] = value;
                    this.Invalidate();
                }
            }
        }
        
        public void SetScore(long score)
        {
            string scoreStr = score.ToString().PadLeft(_scoreData.Digits, '0');
            if (scoreStr.Length > _scoreData.Digits)
            {
                scoreStr = scoreStr.Substring(scoreStr.Length - _scoreData.Digits);
            }
            
            for (int i = 0; i < _scoreData.Digits && i < scoreStr.Length; i++)
            {
                if (char.IsDigit(scoreStr[i]))
                {
                    _currentValues[i] = scoreStr[i] - '0';
                }
            }
            this.Invalidate();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            try
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                
                // Determine which image dictionary to use
                var imageDict = _illuminated ? _reelStorage.ReelIlluImages : _reelStorage.ReelImages;
                
                // Get reel type name (strip trailing digit count like "reel_3" -> "reel")
                string reelType = _scoreData.ReelType;
                if (reelType.Length > 2 && char.IsDigit(reelType[reelType.Length - 1]) && reelType[reelType.Length - 2] == '_')
                {
                    reelType = reelType.Substring(0, reelType.Length - 2);
                }
                
                // Render each digit
                for (int i = 0; i < _scoreData.Digits; i++)
                {
                    int value = _currentValues[i];
                    
                    // Build image name: ReelType_Value or ReelType_Value_SetID (for illuminated)
                    string imageName;
                    if (_illuminated && _scoreData.ReelIlluImageSet > 0)
                    {
                        imageName = $"{reelType}_{value}_{_scoreData.ReelIlluImageSet}";
                    }
                    else
                    {
                        imageName = $"{reelType}_{value}";
                    }
                    
                    if (imageDict.ContainsKey(imageName))
                    {
                        var image = imageDict[imageName];
                        int x = (int)(((_scoreData.Size.Width + _scoreData.Spacing) * i) * _scaleFactor);
                        int y = 0;
                        int width = (int)(_scoreData.Size.Width * _scaleFactor);
                        int height = (int)(_scoreData.Size.Height * _scaleFactor);
                        
                        e.Graphics.DrawImage(image, x, y, width, height);
                    }
                    else
                    {
                        // Debug: draw placeholder if image not found
                        System.Diagnostics.Debug.WriteLine($"[SCORE] Image not found: {imageName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SCORE] Error painting: {ex.Message}");
            }
        }
        
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Don't paint background - transparent
        }
    }
}
