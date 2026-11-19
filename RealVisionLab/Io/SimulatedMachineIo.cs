using System;
using System.Collections.Generic;

namespace RealVisionLab.Io
{
	/// <summary>One recorded output write.</summary>
	public struct SimulatedWrite
	{
		public DateTime Timestamp;
		/// <summary>"IO" for writes through the simulator itself, otherwise the channel name (e.g. "PLC", "UART").</summary>
		public string Channel;
		public MachineOutputs Outputs;

		public override string ToString()
		{
			return string.Format("{0:HH:mm:ss.fff} {1} {2}", Timestamp, Channel, Outputs);
		}
	}

	/// <summary>
	/// Hardware-free backend (Settings.SimulateIo). Records every write with a timestamp and
	/// returns inputs injected by tests. Thread-safe.
	/// </summary>
	public sealed class SimulatedMachineIo : IMachineIo
	{
		public const string DefaultChannel = "IO";

		private readonly object _sync = new object();
		private readonly List<SimulatedWrite> _writes = new List<SimulatedWrite>();
		private readonly Func<DateTime> _clock;
		private MachineInputs _inputs;
		private MachineOutputs _current;
		private bool _connected = true;
		private string _writeError;

		public SimulatedMachineIo() : this(() => DateTime.Now) { }

		public SimulatedMachineIo(Func<DateTime> clock)
		{
			if(clock == null) throw new ArgumentNullException("clock");
			_clock = clock;
		}

		public bool IsConnected
		{
			get { lock(_sync) return _connected; }
		}

		/// <summary>Simulates a connection loss (false) or restore (true).</summary>
		public void SetConnected(bool connected)
		{
			lock(_sync) _connected = connected;
		}

		/// <summary>When not null, every write fails with this error (nothing is recorded).</summary>
		public void InjectWriteError(string error)
		{
			lock(_sync) _writeError = error;
		}

		/// <summary>Sets the level of the inputs returned by TryReadInputs.</summary>
		public void InjectInputs(MachineInputs inputs)
		{
			lock(_sync) _inputs = inputs;
		}

		/// <summary>Last successfully written outputs (any channel).</summary>
		public MachineOutputs CurrentOutputs
		{
			get { lock(_sync) return _current; }
		}

		/// <summary>Snapshot of all recorded writes, oldest first.</summary>
		public IList<SimulatedWrite> Writes
		{
			get { lock(_sync) return _writes.ToArray(); }
		}

		public void ClearWrites()
		{
			lock(_sync) _writes.Clear();
		}

		/// <summary>
		/// A view that records into this simulator under <paramref name="channel"/>.
		/// Used where MainPage writes a single backend directly (PLC-only / UART-only writes).
		/// </summary>
		public IMachineIo Channel(string channel)
		{
			return new ChannelView(this, channel);
		}

		public bool TryWriteOutputs(MachineOutputs outputs, out string error)
		{
			return Write(DefaultChannel, outputs, out error);
		}

		public bool TryReadInputs(out MachineInputs inputs, out string error)
		{
			lock(_sync)
			{
				if(!_connected)
				{
					inputs = new MachineInputs();
					error = "SİMÜLASYON: bağlı değil";
					return false;
				}
				inputs = _inputs;
				error = null;
				return true;
			}
		}

		public void ForceSafeState()
		{
			try
			{
				string error;
				Write(DefaultChannel, MachineOutputs.AllOff, out error);
			}
			catch(Exception) { }
		}

		private bool Write(string channel, MachineOutputs outputs, out string error)
		{
			lock(_sync)
			{
				if(!_connected)
				{
					error = "SİMÜLASYON: bağlı değil";
					return false;
				}
				if(_writeError != null)
				{
					error = _writeError;
					return false;
				}
				_writes.Add(new SimulatedWrite { Timestamp = _clock(), Channel = channel, Outputs = outputs });
				_current = outputs;
				error = null;
				return true;
			}
		}

		private sealed class ChannelView : IMachineIo
		{
			private readonly SimulatedMachineIo _owner;
			private readonly string _channel;

			public ChannelView(SimulatedMachineIo owner, string channel)
			{
				_owner = owner;
				_channel = channel;
			}

			public bool IsConnected { get { return _owner.IsConnected; } }

			public bool TryWriteOutputs(MachineOutputs outputs, out string error)
			{
				return _owner.Write(_channel, outputs, out error);
			}

			public bool TryReadInputs(out MachineInputs inputs, out string error)
			{
				return _owner.TryReadInputs(out inputs, out error);
			}

			public void ForceSafeState()
			{
				try
				{
					string error;
					_owner.Write(_channel, MachineOutputs.AllOff, out error);
				}
				catch(Exception) { }
			}
		}
	}
}
