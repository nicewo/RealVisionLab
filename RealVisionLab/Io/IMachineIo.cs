namespace RealVisionLab.Io
{
	/// <summary>
	/// Single entry point for machine output writes and input reads.
	/// Implementations must be thread-safe and must not throw from the Try* methods.
	/// </summary>
	public interface IMachineIo
	{
		bool IsConnected { get; }

		/// <summary>Writes all four logical outputs. Returns false and an error text on failure.</summary>
		bool TryWriteOutputs(MachineOutputs outputs, out string error);

		/// <summary>Reads the logical inputs. On failure returns false; <paramref name="inputs"/> is then all false.</summary>
		bool TryReadInputs(out MachineInputs inputs, out string error);

		/// <summary>Best effort: all four outputs false. Never throws.</summary>
		void ForceSafeState();
	}
}
