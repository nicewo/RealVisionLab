using System;

namespace RealVisionLab.Io
{
	/// <summary>
	/// Logical machine outputs. Physical bit positions are resolved by each backend
	/// (UART: Settings.Lamba/Piston/Kase/bosBit, S7: Settings.plc_lamba/plc_piston/plc_kase/plc_bos in DB1.DBB0).
	/// </summary>
	public struct MachineOutputs : IEquatable<MachineOutputs>
	{
		public bool Lamba;
		public bool Piston;
		public bool Kase;
		public bool Bos;

		public MachineOutputs(bool lamba, bool piston, bool kase, bool bos)
		{
			Lamba = lamba;
			Piston = piston;
			Kase = kase;
			Bos = bos;
		}

		/// <summary>All four outputs false (safe state).</summary>
		public static MachineOutputs AllOff
		{
			get { return new MachineOutputs(false, false, false, false); }
		}

		public bool Equals(MachineOutputs other)
		{
			return Lamba == other.Lamba && Piston == other.Piston && Kase == other.Kase && Bos == other.Bos;
		}

		public override bool Equals(object obj)
		{
			return obj is MachineOutputs && Equals((MachineOutputs)obj);
		}

		public override int GetHashCode()
		{
			return (Lamba ? 1 : 0) | (Piston ? 2 : 0) | (Kase ? 4 : 0) | (Bos ? 8 : 0);
		}

		public override string ToString()
		{
			return string.Format("Lamba={0} Piston={1} Kase={2} Bos={3}", Lamba, Piston, Kase, Bos);
		}
	}

	/// <summary>Logical machine inputs.</summary>
	public struct MachineInputs
	{
		/// <summary>Start button (UART: Settings.buton, S7: Settings.plc_buton).</summary>
		public bool Start;

		/// <summary>Reset button (UART: Settings.reset_buton; the S7 backend has no reset input).</summary>
		public bool Reset;

		public MachineInputs(bool start, bool reset)
		{
			Start = start;
			Reset = reset;
		}

		public override string ToString()
		{
			return string.Format("Start={0} Reset={1}", Start, Reset);
		}
	}
}
