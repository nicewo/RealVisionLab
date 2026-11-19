namespace RealVisionLab.Config
{
    /// <summary>One record of Settings.Cam: "camIndex,resIndex".</summary>
    public sealed class CameraConfig
    {
        public const int FieldCount = 2;

        /// <summary>Index into the DirectShow device list.</summary>
        public int CamIndex { get; set; }
        /// <summary>Index into the camera's VideoCapabilities.</summary>
        public int ResIndex { get; set; }

        public bool IsValid { get { return Error == null; } }
        public string Error { get; internal set; }
        public string RawText { get; internal set; }
    }

    /// <summary>One record of Settings.PlcFonk: "db,start,size,camId,roiNo" (roiNo is 1-based).</summary>
    public sealed class PlcFunctionConfig
    {
        public const int FieldCount = 5;

        public int Db { get; set; }
        public int Start { get; set; }
        public int Size { get; set; }
        public int CamId { get; set; }
        public int RoiNo { get; set; }

        public bool IsValid { get { return Error == null; } }
        public string Error { get; internal set; }
        public string RawText { get; internal set; }
    }
}
