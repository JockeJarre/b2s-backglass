namespace B2SBackglassServerEXE.Models
{
    /// <summary>
    /// Represents settings from B2STableSettings.xml
    /// </summary>
    public class TableSettings
    {
        // Will be expanded as we implement full settings support
        public string TableName { get; set; } = string.Empty;
        public bool StartAsEXE { get; set; } = true;
        public bool HideBackglass { get; set; } = false;
        public bool HideDMD { get; set; } = false;
    }
}
