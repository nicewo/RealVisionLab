namespace RealVisionLab.Config
{
    /// <summary>
    /// Settings.Bekleme: 11 '-'-separated millisecond values. Names are inferred from their use
    /// in MainPage.Islemler / Reset / Qr_Oku_harici (index in the stored string is given per property).
    /// </summary>
    public sealed class WaitTimes
    {
        public const int Count = 11;

        /// <summary>[0] after Lamba+Piston on, before the scan starts.</summary>
        public int LampPistonSettle { get; set; }
        /// <summary>[1] frame poll interval (the frame deadline is max(5 x this, 5000 ms)).</summary>
        public int FramePoll { get; set; }
        /// <summary>[2] after a frame was captured, before the ROIs are evaluated.</summary>
        public int AfterFrame { get; set; }
        /// <summary>[3] after Audi_alttan_uzat (op 11).</summary>
        public int AudiExtend { get; set; }
        /// <summary>[4] after the scan of all cameras.</summary>
        public int AfterScan { get; set; }
        /// <summary>[5] after ONAYLANDI, before the stamp (Kase) output.</summary>
        public int BeforeStamp { get; set; }
        /// <summary>[6] stamp (Kase) output on time.</summary>
        public int StampOn { get; set; }
        /// <summary>[7] after the stamp output, before Reset.</summary>
        public int AfterStamp { get; set; }
        /// <summary>[8] after a NOK with reset (RST ROI), before Reset.</summary>
        public int BeforeNokReset { get; set; }
        /// <summary>[9] external QR / button wait: value x 10 ticks of 100 ms.</summary>
        public int ExternalWait { get; set; }
        /// <summary>[10] at the end of Reset, before "HAZIR".</summary>
        public int AfterReset { get; set; }

        public bool IsValid { get { return Error == null; } }
        public string Error { get; internal set; }

        public int[] ToArray()
        {
            return new[]
            {
                LampPistonSettle, FramePoll, AfterFrame, AudiExtend, AfterScan, BeforeStamp,
                StampOn, AfterStamp, BeforeNokReset, ExternalWait, AfterReset
            };
        }

        public static WaitTimes FromArray(int[] v)
        {
            return new WaitTimes
            {
                LampPistonSettle = v[0], FramePoll = v[1], AfterFrame = v[2], AudiExtend = v[3],
                AfterScan = v[4], BeforeStamp = v[5], StampOn = v[6], AfterStamp = v[7],
                BeforeNokReset = v[8], ExternalWait = v[9], AfterReset = v[10]
            };
        }

        /// <summary>Defaults of Settings.settings ("2000-1000-1000-1000-1000-1000-750-50-2000-2000-1000").</summary>
        public static WaitTimes Default()
        {
            return FromArray(new[] { 2000, 1000, 1000, 1000, 1000, 1000, 750, 50, 2000, 2000, 1000 });
        }
    }
}
