namespace B2S.ComServer
{
    public static class B2SVersionInfo
    {
        public const string B2S_VERSION_MAJOR = "2";
        public const string B2S_VERSION_MINOR = "1";
        public const string B2S_VERSION_REVISION = "6";
        public const string B2S_VERSION_BUILD = "999";
        public const string B2S_VERSION_HASH = "comserver";
        
        public static readonly string B2S_VERSION_STRING = 
            $"{B2S_VERSION_MAJOR}.{B2S_VERSION_MINOR}.{B2S_VERSION_REVISION}";
        
        public static readonly string B2S_BUILD_STRING = 
            $"{B2S_VERSION_MAJOR}.{B2S_VERSION_MINOR}.{B2S_VERSION_REVISION}.{B2S_VERSION_BUILD}";
        
        public static readonly string B2S_BUILD_STRING_HASH = 
            $"{B2S_VERSION_MAJOR}.{B2S_VERSION_MINOR}.{B2S_VERSION_REVISION}.{B2S_VERSION_BUILD}-{B2S_VERSION_HASH}";
    }
}
