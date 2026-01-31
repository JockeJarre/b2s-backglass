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
                if (root == null)
                    throw new Exception("Invalid backglass file: no root element");

                // Parse backglass info
                data.Name = GetNodeValue(root, "Name", data.Name);
                data.Author = GetNodeValue(root, "TableType", "");
                
                // Parse size
                var sizeNode = root.SelectSingleNode("BackglassSize");
                if (sizeNode != null)
                {
                    data.BackglassSize = new Size(
                        GetAttributeInt(sizeNode, "Width", 800),
                        GetAttributeInt(sizeNode, "Height", 600)
                    );
                }

                // Parse DMD location
                var dmdNode = root.SelectSingleNode("DMDDefaultLocation");
                if (dmdNode != null)
                {
                    data.DMDLocation = new Point(
                        GetAttributeInt(dmdNode, "X", 0),
                        GetAttributeInt(dmdNode, "Y", 0)
                    );
                    data.DMDSize = new Size(
                        GetAttributeInt(dmdNode, "Width", 128),
                        GetAttributeInt(dmdNode, "Height", 32)
                    );
                }

                // Parse background image
                var bgImageNode = root.SelectSingleNode("BackglassImage");
                if (bgImageNode != null)
                {
                    string base64 = bgImageNode.Attributes?["Value"]?.InnerText;
                    if (!string.IsNullOrEmpty(base64))
                    {
                        data.BackgroundImage = Base64ToImage(base64);
                    }
                }

                // Parse illuminations (bulbs/lamps)
                var illuminationNodes = root.SelectNodes("Illumination/Bulb");
                if (illuminationNodes != null)
                {
                    foreach (XmlElement node in illuminationNodes)
                    {
                        var bulb = ParseIllumination(node);
                        if (bulb != null)
                        {
                            data.Illuminations.Add(bulb);
                        }
                    }
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing backglass file: {ex.Message}", ex);
            }

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
    }
}
