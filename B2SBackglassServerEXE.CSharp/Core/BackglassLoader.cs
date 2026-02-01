using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;

namespace B2SBackglassServerEXE.Core
{
    /// <summary>
    /// Loads and parses .directb2s backglass files
    /// </summary>
    public class BackglassLoader
    {
        private string _currentDirectory;

        public BackglassLoader()
        {
            _currentDirectory = Directory.GetCurrentDirectory();
        }

        public Models.BackglassData? Load(string tableName)
        {
            string backglassFile = FindBackglassFile(tableName);
            
            if (string.IsNullOrEmpty(backglassFile))
            {
                throw new FileNotFoundException($"Backglass file not found for table: {tableName}");
            }

            return ParseBackglassFile(backglassFile);
        }

        private string? FindBackglassFile(string tableName)
        {
            // Try various naming patterns (same as VB version)
            string[] patterns = new[]
            {
                $"{tableName}.directb2s",
                $"{Program.GameName}.directb2s",
                $"{Path.GetFileNameWithoutExtension(tableName)}.directb2s"
            };

            foreach (var pattern in patterns)
            {
                string fullPath = Path.Combine(_currentDirectory, pattern);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Try fuzzy matching (simplified version)
            try
            {
                var files = Directory.GetFiles(_currentDirectory, "*.directb2s");
                if (files.Length > 0)
                {
                    // Simple match: first file found
                    return files[0];
                }
            }
            catch
            {
                // Ignore search errors
            }

            return null;
        }

        private Models.BackglassData ParseBackglassFile(string filename)
        {
            System.Diagnostics.Debug.WriteLine($"[LOADER] Parsing backglass file: {filename}");
            
            var data = new Models.BackglassData
            {
                FileName = filename,
                Name = Path.GetFileNameWithoutExtension(filename)
            };

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filename);

                var root = xmlDoc.DocumentElement;
                if (root == null || root.Name != "DirectB2SData")
                    throw new Exception("Invalid backglass file: no DirectB2SData root element");

                System.Diagnostics.Debug.WriteLine($"[LOADER] Root element: {root.Name}");

                // Parse backglass info - attributes are stored in "Value" attribute
                data.Name = GetNodeAttributeValue(root, "Name", "Value", data.Name);
                data.TableType = GetNodeAttributeValue(root, "TableType", "Value", "");
                data.DMDType = GetNodeAttributeValue(root, "DMDType", "Value", "");
                data.GrillHeight = int.Parse(GetNodeAttributeValue(root, "GrillHeight", "Value", "0"));
                var grillNode = root.SelectSingleNode("GrillHeight");
                if (grillNode?.Attributes?["Small"] != null)
                {
                    data.SmallGrillHeight = int.Parse(grillNode.Attributes["Small"].InnerText);
                }
                var dualNode = root.SelectSingleNode("DualBackglass");
                if (dualNode != null)
                {
                    data.DualBackglass = GetNodeAttributeValue(root, "DualBackglass", "Value", "0") == "1";
                }
                
                System.Diagnostics.Debug.WriteLine($"[LOADER] Backglass name: {data.Name}, Type: {data.TableType}");
                
                // Parse size - BackglassSize has Width/Height attributes directly
                var sizeNode = root.SelectSingleNode("BackglassSize");
                if (sizeNode != null)
                {
                    data.BackglassSize = new Size(
                        GetAttributeInt(sizeNode, "Width", 800),
                        GetAttributeInt(sizeNode, "Height", 600)
                    );
                    System.Diagnostics.Debug.WriteLine($"[LOADER] Backglass size: {data.BackglassSize.Width}x{data.BackglassSize.Height}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[LOADER] WARNING: No BackglassSize node found");
                }

                // Parse DMD location - LocX/LocY attributes 
                var dmdNode = root.SelectSingleNode("DMDDefaultLocation");
                if (dmdNode != null)
                {
                    data.DMDLocation = new Point(
                        GetAttributeInt(dmdNode, "LocX", 0),
                        GetAttributeInt(dmdNode, "LocY", 0)
                    );
                    data.DMDSize = new Size(
                        GetAttributeInt(dmdNode, "Width", 128),
                        GetAttributeInt(dmdNode, "Height", 32)
                    );
                }

                // Parse background image (from Images/BackglassImage node)
                var bgImageNode = root.SelectSingleNode("Images/BackglassImage");
                var bgOnImageNode = root.SelectSingleNode("Images/BackglassOnImage");
                var bgOffImageNode = root.SelectSingleNode("Images/BackglassOffImage");
                
                // Handle three-state backglass (On/Off images)
                if (bgOffImageNode != null)
                {
                    string? base64Off = bgOffImageNode.Attributes?["Value"]?.InnerText;
                    if (!string.IsNullOrEmpty(base64Off))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LOADER] Loading background OFF image ({base64Off.Length} chars base64)");
                        data.BackglassOffImage = Base64ToImage(base64Off);
                    }
                    
                    if (bgOnImageNode != null)
                    {
                        string? base64On = bgOnImageNode.Attributes?["Value"]?.InnerText;
                        if (!string.IsNullOrEmpty(base64On))
                        {
                            System.Diagnostics.Debug.WriteLine($"[LOADER] Loading background ON image ({base64On.Length} chars base64)");
                            data.BackglassOnImage = Base64ToImage(base64On);
                            data.BackgroundImage = data.BackglassOffImage; // Start with off state
                        }
                    }
                }
                else if (bgImageNode != null)
                {
                    // Single background image
                    string? base64 = bgImageNode.Attributes?["Value"]?.InnerText;
                    if (!string.IsNullOrEmpty(base64))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LOADER] Loading background image ({base64.Length} chars base64)");
                        data.BackgroundImage = Base64ToImage(base64);
                        if (data.BackgroundImage != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LOADER] Background image loaded: {data.BackgroundImage.Width}x{data.BackgroundImage.Height}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[LOADER] ERROR: Failed to decode background image!");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[LOADER] WARNING: No Images/BackglassImage node found in XML");
                }

                // Parse illuminations (bulbs/lamps) - XML structure is Illumination/Bulb
                var illuminationNodes = root.SelectNodes("Illumination/Bulb");
                if (illuminationNodes != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOADER] Found {illuminationNodes.Count} illuminations");
                    foreach (XmlElement node in illuminationNodes)
                    {
                        var bulb = ParseIllumination(node);
                        if (bulb != null)
                        {
                            data.Illuminations.Add(bulb);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[LOADER] No Illumination/Bulb nodes found");
                }

                // Parse animations
                var animationNodes = root.SelectNodes("Animations/Animation");
                if (animationNodes != null)
                {
                    foreach (XmlElement node in animationNodes)
                    {
                        var animation = ParseAnimation(node);
                        if (animation != null)
                        {
                            data.Animations.Add(animation);
                        }
                    }
                }

                // Parse sounds
                var soundNodes = root.SelectNodes("Sounds/Sound");
                if (soundNodes != null)
                {
                    foreach (XmlElement node in soundNodes)
                    {
                        var sound = ParseSound(node);
                        if (sound != null)
                        {
                            data.Sounds.Add(sound);
                        }
                    }
                }

                // Parse scores/reels
                var scoresNode = root.SelectSingleNode("Scores");
                if (scoresNode != null)
                {
                    // Get reel rolling interval from Scores node
                    if (scoresNode.Attributes?["ReelRollingInterval"] != null)
                    {
                        data.ReelRollingInterval = GetAttributeInt(scoresNode, "ReelRollingInterval", 50);
                    }
                    
                    var scoreNodes = root.SelectNodes("Scores/Score");
                    if (scoreNodes != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LOADER] Found {scoreNodes.Count} score displays");
                        foreach (XmlElement node in scoreNodes)
                        {
                            var score = ParseScore(node, data.ReelRollingInterval);
                            if (score != null)
                            {
                                data.Scores.Add(score);
                            }
                        }
                    }
                }

                // Parse reels
                var reelsNode = root.SelectSingleNode("Reels");
                if (reelsNode != null)
                {
                    System.Diagnostics.Debug.WriteLine("[LOADER] Parsing reels");
                    ParseReels(reelsNode, data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOADER] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LOADER] Stack: {ex.StackTrace}");
                throw new Exception($"Error parsing backglass file: {ex.Message}", ex);
            }

            System.Diagnostics.Debug.WriteLine($"[LOADER] Successfully parsed {data.Illuminations.Count} illuminations, {data.Animations.Count} animations");
            return data;
        }

        private Models.Illumination? ParseIllumination(XmlElement node)
        {
            try
            {
                var illumination = new Models.Illumination
                {
                    ID = GetAttributeInt(node, "ID", 0),
                    Name = GetAttributeString(node, "Name", ""),
                    Parent = GetAttributeString(node, "Parent", "Backglass"),
                    Location = new Point(
                        GetAttributeInt(node, "LocX", 0),
                        GetAttributeInt(node, "LocY", 0)
                    ),
                    Size = new Size(
                        GetAttributeInt(node, "Width", 100),
                        GetAttributeInt(node, "Height", 100)
                    ),
                    Visible = GetAttributeInt(node, "Visible", 1) == 1,
                    InitialState = GetAttributeInt(node, "InitialState", 0),
                    Intensity = GetAttributeInt(node, "Intensity", 100),
                    DualMode = GetAttributeInt(node, "DualMode", 0),
                    ZOrder = GetAttributeInt(node, "ZOrder", 0)
                };

                // Parse ROM ID info
                if (node.Attributes?["B2SID"] != null)
                {
                    illumination.RomID = GetAttributeInt(node, "B2SID", 0);
                    illumination.RomIDType = 1;
                    illumination.RomIDValue = GetAttributeInt(node, "B2SValue", 0);
                }
                else
                {
                    illumination.RomID = GetAttributeInt(node, "RomID", 0);
                    illumination.RomIDType = GetAttributeInt(node, "RomIDType", 0);
                    illumination.RomInverted = GetAttributeInt(node, "RomInverted", 0) == 1;
                }

                // Parse images
                string imageData = GetAttributeString(node, "Image", "");
                if (!string.IsNullOrEmpty(imageData))
                {
                    illumination.OnImage = Base64ToImage(imageData);
                }

                string offImageData = GetAttributeString(node, "OffImage", "");
                if (!string.IsNullOrEmpty(offImageData))
                {
                    illumination.OffImage = Base64ToImage(offImageData);
                }

                return illumination;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing illumination: {ex.Message}");
                return null;
            }
        }

        private Models.Animation? ParseAnimation(XmlElement node)
        {
            try
            {
                var animation = new Models.Animation
                {
                    Name = GetAttributeString(node, "Name", ""),
                    Interval = GetAttributeInt(node, "Interval", 100)
                };

                // Parse animation steps
                var stepNodes = node.SelectNodes("Step");
                if (stepNodes != null)
                {
                    foreach (XmlElement stepNode in stepNodes)
                    {
                        var step = new Models.AnimationStep
                        {
                            Duration = GetAttributeInt(stepNode, "Duration", 100),
                            Bulbs = GetAttributeString(stepNode, "Bulbs", "").Split(',')
                                .Select(b => b.Trim())
                                .Where(b => !string.IsNullOrEmpty(b))
                                .ToArray(),
                            Visible = GetAttributeInt(stepNode, "Visible", 1) == 1
                        };
                        animation.Steps.Add(step);
                    }
                }

                return animation;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing animation: {ex.Message}");
                return null;
            }
        }

        private Models.Sound? ParseSound(XmlElement node)
        {
            try
            {
                return new Models.Sound
                {
                    Name = GetAttributeString(node, "Name", ""),
                    ID = GetAttributeInt(node, "ID", 0),
                    Data = GetAttributeString(node, "Data", "")
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing sound: {ex.Message}");
                return null;
            }
        }

        private Image? Base64ToImage(string base64String)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    return Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error decoding image: {ex.Message}");
                return null;
            }
        }

        private string GetNodeValue(XmlNode parent, string nodeName, string defaultValue)
        {
            var node = parent.SelectSingleNode(nodeName);
            return node?.InnerText ?? defaultValue;
        }

        private int GetAttributeInt(XmlNode node, string attributeName, int defaultValue)
        {
            var attr = node.Attributes?[attributeName];
            if (attr != null && int.TryParse(attr.InnerText, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        private string GetAttributeString(XmlNode node, string attributeName, string defaultValue)
        {
            return node.Attributes?[attributeName]?.InnerText ?? defaultValue;
        }

        private string GetNodeAttributeValue(XmlNode parent, string nodeName, string attributeName, string defaultValue)
        {
            var node = parent.SelectSingleNode(nodeName);
            return node?.Attributes?[attributeName]?.InnerText ?? defaultValue;
        }

        private Models.Score? ParseScore(XmlElement node, int rollingInterval)
        {
            try
            {
                var score = new Models.Score
                {
                    ID = GetAttributeInt(node, "ID", 0),
                    Name = GetAttributeString(node, "Name", ""),
                    Location = new Point(
                        GetAttributeInt(node, "LocX", 0),
                        GetAttributeInt(node, "LocY", 0)
                    ),
                    Size = new Size(
                        GetAttributeInt(node, "Width", 100),
                        GetAttributeInt(node, "Height", 100)
                    ),
                    DigitCount = GetAttributeInt(node, "DigitCount", 6),
                    Spacing = GetAttributeInt(node, "Spacing", 0),
                    RollingDirection = GetAttributeInt(node, "RollingDirection", 0)
                };

                // Parse digit images (0-9)
                var digitNodes = node.SelectNodes("Digit");
                if (digitNodes != null)
                {
                    foreach (XmlElement digitNode in digitNodes)
                    {
                        int value = GetAttributeInt(digitNode, "Value", 0);
                        string imageData = GetAttributeString(digitNode, "Image", "");
                        if (!string.IsNullOrEmpty(imageData))
                        {
                            score.Digits.Add(new Models.ScoreDigit
                            {
                                Value = value,
                                Image = Base64ToImage(imageData)
                            });
                        }
                    }
                }

                return score;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing score: {ex.Message}");
                return null;
            }
        }

        private void ParseReels(XmlNode reelsNode, Models.BackglassData data)
        {
            try
            {
                // Reels can have multiple structures - check all variants
                // Variant 1: Reels/Image
                var imageNodes = reelsNode.SelectNodes("Image");
                if (imageNodes != null && imageNodes.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOADER] Found {imageNodes.Count} reel images (Variant 1)");
                    foreach (XmlElement imageNode in imageNodes)
                    {
                        ParseReelImageNode(imageNode, data);
                    }
                }

                // Variant 2: Reels/Images/Image
                var imagesContainer = reelsNode.SelectSingleNode("Images");
                if (imagesContainer != null)
                {
                    imageNodes = imagesContainer.SelectNodes("Image");
                    if (imageNodes != null && imageNodes.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LOADER] Found {imageNodes.Count} reel images (Variant 2)");
                        foreach (XmlElement imageNode in imageNodes)
                        {
                            ParseReelImageNode(imageNode, data);
                        }
                    }
                }

                // Variant 3: Reels/IlluminatedImages
                var illumContainer = reelsNode.SelectSingleNode("IlluminatedImages");
                if (illumContainer != null)
                {
                    // Check for direct IlluminatedImage children
                    var illumNodes = illumContainer.SelectNodes("IlluminatedImage");
                    if (illumNodes != null && illumNodes.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LOADER] Found {illumNodes.Count} illuminated reel images");
                        foreach (XmlElement illumNode in illumNodes)
                        {
                            ParseIlluminatedReelNode(illumNode, data);
                        }
                    }

                    // Check for Set/IlluminatedImage structure
                    var setNodes = illumContainer.SelectNodes("Set");
                    if (setNodes != null)
                    {
                        foreach (XmlElement setNode in setNodes)
                        {
                            illumNodes = setNode.SelectNodes("IlluminatedImage");
                            if (illumNodes != null)
                            {
                                foreach (XmlElement illumNode in illumNodes)
                                {
                                    ParseIlluminatedReelNode(illumNode, data);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing reels: {ex.Message}");
            }
        }

        private void ParseReelImageNode(XmlElement node, Models.BackglassData data)
        {
            // This would create reel objects - simplified for now
            System.Diagnostics.Debug.WriteLine($"[LOADER] Parsing reel image node: {node.OuterXml.Substring(0, Math.Min(100, node.OuterXml.Length))}");
        }

        private void ParseIlluminatedReelNode(XmlElement node, Models.BackglassData data)
        {
            // This would create illuminated reel objects - simplified for now
            System.Diagnostics.Debug.WriteLine($"[LOADER] Parsing illuminated reel node");
        }
    }
}
