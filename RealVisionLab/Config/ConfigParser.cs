using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RealVisionLab.Config
{
    /// <summary>
    /// Parses / serialises the string-encoded settings (Rect, Cam, Bekleme, PlcFonk).
    /// The persisted format is unchanged: Serialize(Parse(s)) == s for valid canonical strings.
    /// Parse* never throws: invalid records are flagged (IsValid/Error/RawText) and callers treat them as NOK.
    /// TryParse* returns true only when every record is valid and fills the error list.
    /// </summary>
    public static class ConfigParser
    {
        const NumberStyles IntStyle = NumberStyles.Integer;

        // ---- part name -------------------------------------------------------------------------

        /// <summary>Removes ',' and '/' (record separators) from a part name; null becomes "".</summary>
        public static string SanitizePartName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (name.IndexOf(',') < 0 && name.IndexOf('/') < 0) return name;
            return name.Replace(",", "").Replace("/", "");
        }

        // ---- cameras ---------------------------------------------------------------------------

        /// <summary>Cam = "camIndex,resIndex" records separated by '/'. Never returns an empty list.</summary>
        public static List<CameraConfig> ParseCameras(string text)
        {
            var list = new List<CameraConfig>();
            foreach (string rec in SplitRecords(text))
            {
                var c = new CameraConfig();
                string[] f = rec.Split(',');
                int a, b;
                if (f.Length != CameraConfig.FieldCount)
                    Fail(c, rec, "expected 2 fields (camIndex,resIndex), got " + f.Length);
                else if (!TryInt(f[0], out a) || !TryInt(f[1], out b))
                    Fail(c, rec, "camIndex/resIndex must be integers");
                else { c.CamIndex = a; c.ResIndex = b; }
                list.Add(c);
            }
            if (list.Count == 0)
                list.Add(new CameraConfig { Error = "Cam is empty (using camera 0,0)", RawText = text ?? "" });
            return list;
        }

        public static bool TryParseCameras(string text, out List<CameraConfig> cameras, out List<string> errors)
        {
            cameras = ParseCameras(text);
            errors = new List<string>();
            for (int i = 0; i < cameras.Count; i++)
                if (!cameras[i].IsValid) errors.Add("Cam[" + i + "]: " + cameras[i].Error);
            return errors.Count == 0;
        }

        public static string Serialize(IEnumerable<CameraConfig> cameras)
        {
            return string.Join("/", cameras.Select(c => c.IsValid
                ? c.CamIndex.ToString(CultureInfo.InvariantCulture) + "," + c.ResIndex.ToString(CultureInfo.InvariantCulture)
                : c.RawText));
        }

        // ---- ROI -------------------------------------------------------------------------------

        /// <summary>
        /// Rect = ROI records separated by '/'. <paramref name="cameraCount"/> is used for the CamId range check.
        /// Result has one entry per record (empty records are skipped); invalid entries carry safe values and CamId inside range.
        /// </summary>
        public static List<RoiConfig> ParseRois(string text, int cameraCount)
        {
            var list = new List<RoiConfig>();
            foreach (string rec in SplitRecords(text))
                list.Add(ParseRoi(rec, cameraCount));
            return list;
        }

        static RoiConfig ParseRoi(string rec, int cameraCount)
        {
            var r = new RoiConfig();
            string[] f = rec.Split(',');
            if (f.Length != RoiConfig.FieldCount)
            {
                Fail(r, rec, "expected " + RoiConfig.FieldCount + " fields, got " + f.Length + " (part name must not contain ',')");
                TryInt(f.Length > 6 ? f[6] : null, out int cam);
                r.CamId = cam >= 0 && cam < cameraCount ? cam : 0;
                if (f.Length > 10) r.PartName = f[f.Length - 1];
                return r;
            }

            var problems = new List<string>();
            int x, y, w, h, min, max, camId, op, thr;
            bool okX = TryInt(f[0], out x), okY = TryInt(f[1], out y), okW = TryInt(f[2], out w), okH = TryInt(f[3], out h);
            bool okMin = TryInt(f[4], out min), okMax = TryInt(f[5], out max), okCam = TryInt(f[6], out camId);
            bool okOp = TryInt(f[7], out op), okThr = TryInt(f[8], out thr);
            bool rst;
            bool okRst = bool.TryParse(f[9], out rst);

            if (!(okX && okY && okW && okH && okMin && okMax && okCam && okOp && okThr))
                problems.Add("numeric field is not an integer");
            if (!okRst) problems.Add("ResetOrStop must be True/False");
            if (okW && w <= 0) problems.Add("Width must be > 0 (was " + w + ")");
            if (okH && h <= 0) problems.Add("Height must be > 0 (was " + h + ")");
            if (okOp && (op < 0 || op > RoiConfig.MaxOperation)) problems.Add("Islem must be 0.." + RoiConfig.MaxOperation + " (was " + op + ")");
            if (okThr && (thr < 0 || thr > 255)) problems.Add("Threshold must be 0..255 (was " + thr + ")");
            if (okCam && (camId < 0 || camId >= cameraCount)) problems.Add("CamId " + camId + " is outside 0.." + (cameraCount - 1));
            if (okMin && okMax && min > max) problems.Add("MinOfset (" + min + ") > MaxOfset (" + max + ")");

            // Safe values (used for display; the runtime treats an invalid record as NOK anyway).
            r.X = x; r.Y = y; r.Width = w > 0 ? w : 1; r.Height = h > 0 ? h : 1;
            r.MinOffset = min; r.MaxOffset = max;
            r.CamId = okCam && camId >= 0 && camId < cameraCount ? camId : 0;
            r.Operation = okOp && op >= 0 && op <= RoiConfig.MaxOperation ? op : 0;
            r.Threshold = okThr && thr >= 0 && thr <= 255 ? thr : 0;
            r.SetResetOrStopRaw(okRst && rst, okRst ? f[9] : null);
            r.PartName = f[10];

            if (problems.Count > 0) Fail(r, rec, string.Join("; ", problems));
            return r;
        }

        public static bool TryParseRois(string text, int cameraCount, out List<RoiConfig> rois, out List<string> errors)
        {
            rois = ParseRois(text, cameraCount);
            errors = new List<string>();
            for (int i = 0; i < rois.Count; i++)
                if (!rois[i].IsValid) errors.Add("Rect[" + i + "]: " + rois[i].Error);
            return errors.Count == 0;
        }

        public static string Serialize(IEnumerable<RoiConfig> rois)
        {
            return string.Join("/", rois.Select(SerializeRoi));
        }

        static string SerializeRoi(RoiConfig r)
        {
            if (!r.IsValid) return r.RawText;
            var sb = new StringBuilder();
            sb.Append(r.X.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.Y.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.Width.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.Height.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.MinOffset.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.MaxOffset.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.CamId.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.Operation.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.Threshold.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(r.ResetOrStopText).Append(',')
              .Append(SanitizePartName(r.PartName));
            return sb.ToString();
        }

        // ---- wait times ------------------------------------------------------------------------

        /// <summary>Bekleme = 11 '-'-separated ms values (each >= 0). Invalid text yields the defaults with Error set.</summary>
        public static WaitTimes ParseWaitTimes(string text)
        {
            string[] f = (text ?? "").Split('-');
            var v = new int[WaitTimes.Count];
            string error = null;
            if (f.Length != WaitTimes.Count)
                error = "expected " + WaitTimes.Count + " values, got " + f.Length;
            else
            {
                for (int i = 0; i < f.Length; i++)
                {
                    if (!TryInt(f[i], out v[i]) || v[i] < 0) { error = "value " + i + " must be an integer >= 0"; break; }
                }
            }
            if (error != null)
            {
                WaitTimes d = WaitTimes.Default();
                d.Error = "Bekleme: " + error + " (defaults used)";
                return d;
            }
            return WaitTimes.FromArray(v);
        }

        public static bool TryParseWaitTimes(string text, out WaitTimes waits, out List<string> errors)
        {
            waits = ParseWaitTimes(text);
            errors = new List<string>();
            if (!waits.IsValid) errors.Add(waits.Error);
            return errors.Count == 0;
        }

        public static string Serialize(WaitTimes w)
        {
            return string.Join("-", w.ToArray().Select(i => i.ToString(CultureInfo.InvariantCulture)));
        }

        // ---- PLC functions ---------------------------------------------------------------------

        /// <summary>PlcFonk = "db,start,size,camId,roiNo" records separated by '/'.</summary>
        public static List<PlcFunctionConfig> ParsePlcFunctions(string text)
        {
            var list = new List<PlcFunctionConfig>();
            foreach (string rec in SplitRecords(text))
            {
                var p = new PlcFunctionConfig();
                string[] f = rec.Split(',');
                var v = new int[PlcFunctionConfig.FieldCount];
                bool ok = f.Length == PlcFunctionConfig.FieldCount;
                for (int i = 0; ok && i < v.Length; i++) ok = TryInt(f[i], out v[i]);
                if (!ok) Fail(p, rec, "expected 5 integer fields (db,start,size,camId,roiNo)");
                else if (v[0] < 0 || v[1] < 0 || v[2] < 0 || v[3] < 0 || v[4] < 0) Fail(p, rec, "fields must be >= 0");
                else { p.Db = v[0]; p.Start = v[1]; p.Size = v[2]; p.CamId = v[3]; p.RoiNo = v[4]; }
                list.Add(p);
            }
            return list;
        }

        public static bool TryParsePlcFunctions(string text, out List<PlcFunctionConfig> functions, out List<string> errors)
        {
            functions = ParsePlcFunctions(text);
            errors = new List<string>();
            for (int i = 0; i < functions.Count; i++)
                if (!functions[i].IsValid) errors.Add("PlcFonk[" + i + "]: " + functions[i].Error);
            return errors.Count == 0;
        }

        public static string Serialize(IEnumerable<PlcFunctionConfig> functions)
        {
            return string.Join("/", functions.Select(p => p.IsValid
                ? string.Join(",", new[] { p.Db, p.Start, p.Size, p.CamId, p.RoiNo }.Select(i => i.ToString(CultureInfo.InvariantCulture)))
                : p.RawText));
        }

        // ---- helpers ---------------------------------------------------------------------------

        static IEnumerable<string> SplitRecords(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            foreach (string s in text.Split('/'))
                if (s.Length > 0) yield return s;
        }

        static bool TryInt(string s, out int value)
        {
            if (s == null) { value = 0; return false; }
            return int.TryParse(s, IntStyle, CultureInfo.InvariantCulture, out value);
        }

        static void Fail(CameraConfig c, string raw, string msg) { c.Error = msg; c.RawText = raw; }
        static void Fail(RoiConfig r, string raw, string msg) { r.Error = msg; r.RawText = raw; }
        static void Fail(PlcFunctionConfig p, string raw, string msg) { p.Error = msg; p.RawText = raw; }

        /// <summary>
        /// Debug-only round-trip self-check against the defaults of Settings.settings
        /// (T12 turns this into unit tests). Returns an error text or null when OK.
        /// </summary>
        public static string SelfCheck()
        {
            const string rect = "734,586,487,315,0,0,0,0,95,False,Referans/607,300,75,75,10,5000,0,2,170,False,Bulunamadı";
            const string cam = "0,3";
            const string bekleme = "2000-1000-1000-1000-1000-1000-750-50-2000-2000-1000";
            const string plc = "0,0,0,0,2";
            if (Serialize(ParseRois(rect, 1)) != rect) return "Rect round-trip failed";
            if (Serialize(ParseCameras(cam)) != cam) return "Cam round-trip failed";
            if (Serialize(ParseWaitTimes(bekleme)) != bekleme) return "Bekleme round-trip failed";
            if (Serialize(ParsePlcFunctions(plc)) != plc) return "PlcFonk round-trip failed";
            if (!ParseRois(rect, 1).All(r => r.IsValid)) return "default Rect not valid";
            return null;
        }
    }
}
