using System;

namespace RealVisionLab.Config
{
    /// <summary>
    /// One ROI record of Settings.Rect:
    /// X,Y,W,H,MinOfset,MaxOfset,CamId,Islem,Threshold,ResetOrStop,PartName
    /// An invalid record (<see cref="IsValid"/> == false) keeps its original text in <see cref="RawText"/>
    /// so it round-trips unchanged; numeric fields hold safe values and the runtime treats it as NOK.
    /// </summary>
    public sealed class RoiConfig
    {
        public const int FieldCount = 11;
        public const int MaxOperation = 20;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
        public int MinOffset { get; set; }
        public int MaxOffset { get; set; }
        /// <summary>Camera index. Always inside [0, cameraCount) for records produced by the parser.</summary>
        public int CamId { get; set; }
        /// <summary>Scan operation index (0..20), see Property.list.</summary>
        public int Operation { get; set; }
        public int Threshold { get; set; }

        bool _resetOrStop;
        string _resetOrStopRaw; // keeps the stored casing ("True"/"true") for byte-exact round trip
        /// <summary>Stored as True/False text; field 9.</summary>
        public bool ResetOrStop
        {
            get { return _resetOrStop; }
            set { if (value != _resetOrStop) _resetOrStopRaw = null; _resetOrStop = value; }
        }

        string _partName = "";
        /// <summary>Part name (field 10). ',' and '/' are removed on assignment (they would corrupt Rect).</summary>
        public string PartName
        {
            get { return _partName; }
            set { _partName = ConfigParser.SanitizePartName(value); }
        }

        public bool IsValid { get { return Error == null; } }
        /// <summary>Validation error of this record, or null when valid.</summary>
        public string Error { get; internal set; }
        /// <summary>Original text of an invalid record (serialised back unchanged).</summary>
        public string RawText { get; internal set; }

        internal void SetResetOrStopRaw(bool value, string raw) { _resetOrStop = value; _resetOrStopRaw = raw; }

        internal string ResetOrStopText
        {
            get { return _resetOrStopRaw ?? (_resetOrStop ? "True" : "False"); }
        }
    }
}
