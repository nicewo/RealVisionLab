using System;
using System.Collections.Generic;

namespace RealVisionLab.Io
{
	/// <summary>
	/// Fans writes out to every backend that is enabled (e.g. Settings.PlcEnable / UartEnable,
	/// evaluated at call time) and connected. Disconnected or disabled backends are skipped
	/// silently, as the pre-T04 code did with its IsOpen/Connected guards.
	/// </summary>
	public sealed class CompositeMachineIo : IMachineIo
	{
		private sealed class Backend
		{
			public string Name;
			public IMachineIo Io;
			public Func<bool> Enabled;
		}

		private readonly List<Backend> _backends = new List<Backend>();

		public CompositeMachineIo Add(string name, IMachineIo io, Func<bool> enabled)
		{
			if(io == null) throw new ArgumentNullException("io");
			if(enabled == null) throw new ArgumentNullException("enabled");
			_backends.Add(new Backend { Name = name, Io = io, Enabled = enabled });
			return this;
		}

		private static bool IsActive(Backend b)
		{
			try { return b.Enabled() && b.Io.IsConnected; }
			catch(Exception) { return false; }
		}

		/// <summary>True when at least one enabled backend is connected.</summary>
		public bool IsConnected
		{
			get
			{
				foreach(Backend b in _backends)
				{
					if(IsActive(b)) return true;
				}
				return false;
			}
		}

		public bool TryWriteOutputs(MachineOutputs outputs, out string error)
		{
			List<string> errors = null;
			foreach(Backend b in _backends)
			{
				if(!IsActive(b)) continue;
				string e;
				if(!b.Io.TryWriteOutputs(outputs, out e))
				{
					if(errors == null) errors = new List<string>();
					errors.Add(e);
				}
			}

			error = errors == null ? null : string.Join("; ", errors);
			return errors == null;
		}

		/// <summary>OR of the inputs of every enabled, connected backend.</summary>
		public bool TryReadInputs(out MachineInputs inputs, out string error)
		{
			inputs = new MachineInputs();
			List<string> errors = null;
			foreach(Backend b in _backends)
			{
				if(!IsActive(b)) continue;
				MachineInputs i;
				string e;
				if(b.Io.TryReadInputs(out i, out e))
				{
					inputs.Start |= i.Start;
					inputs.Reset |= i.Reset;
				}
				else
				{
					if(errors == null) errors = new List<string>();
					errors.Add(b.Name + ": " + e);
				}
			}

			error = errors == null ? null : string.Join("; ", errors);
			return errors == null;
		}

		/// <summary>Best effort on every backend, enabled or not. Never throws.</summary>
		public void ForceSafeState()
		{
			foreach(Backend b in _backends)
			{
				try { b.Io.ForceSafeState(); }
				catch(Exception) { }
			}
		}
	}
}
