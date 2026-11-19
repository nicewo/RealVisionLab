using System;
using Sharp7;

namespace RealVisionLab.Io
{
	/// <summary>Bit positions inside DB1.DBB0 (Settings.plc_*).</summary>
	public struct S7BitMap
	{
		public int Lamba;
		public int Piston;
		public int Kase;
		public int Bos;
		public int Start;
	}

	/// <summary>
	/// Siemens S7 backend (Sharp7). Every S7Client call is made under a private lock,
	/// so the UI timer and Task.Run scan functions can no longer use the client concurrently.
	/// </summary>
	/// <remarks>
	/// Output semantics are kept exactly as before T04: a cached image of DB1.DBB0 is modified
	/// (only the four mapped bits) and written as a whole byte. The PLC byte is NOT read back
	/// before writing; doing so would preserve a latched start bit and could re-trigger the cycle.
	/// </remarks>
	public sealed class S7MachineIo : IMachineIo
	{
		public const int DbNumber = 1;
		public const int DbStart = 0;
		public const int DbSize = 1;

		private readonly object _sync = new object();
		private readonly S7Client _client = new S7Client();
		private readonly Func<S7BitMap> _bitMap;
		private readonly byte[] _image = new byte[DbSize];

		public S7MachineIo(Func<S7BitMap> bitMap)
		{
			if(bitMap == null) throw new ArgumentNullException("bitMap");
			_bitMap = bitMap;
		}

		/// <summary>Last Sharp7 result code (0 = OK).</summary>
		public int LastResult { get; private set; }

		public bool IsConnected
		{
			get
			{
				lock(_sync)
				{
					try { return _client.Connected; }
					catch(Exception) { return false; }
				}
			}
		}

		/// <summary>Wraps S7Client.ConnectTo. <paramref name="code"/> is the Sharp7 result (0 = OK, -1 = exception).</summary>
		public bool TryConnect(string ip, int rack, int slot, out int code, out string error)
		{
			lock(_sync)
			{
				try
				{
					code = _client.ConnectTo(ip, rack, slot);
					LastResult = code;
					error = code == 0 ? null : _client.ErrorText(code);
					return code == 0;
				}
				catch(Exception ex)
				{
					code = -1;
					LastResult = code;
					error = ex.Message;
					return false;
				}
			}
		}

		public bool TryWriteOutputs(MachineOutputs outputs, out string error)
		{
			lock(_sync)
			{
				try
				{
					if(!_client.Connected)
					{
						error = "PLC bağlı değil";
						return false;
					}

					S7BitMap map = _bitMap();
					// Same SetBitAt order as the pre-T04 code (lamba, bos, piston, kase).
					S7.SetBitAt(_image, 0, map.Lamba, outputs.Lamba);
					S7.SetBitAt(_image, 0, map.Bos, outputs.Bos);
					S7.SetBitAt(_image, 0, map.Piston, outputs.Piston);
					S7.SetBitAt(_image, 0, map.Kase, outputs.Kase);

					int code = _client.DBWrite(DbNumber, DbStart, DbSize, _image);
					LastResult = code;
					if(code != 0)
					{
						error = _client.ErrorText(code);
						return false;
					}

					error = null;
					return true;
				}
				catch(Exception ex)
				{
					error = ex.Message;
					return false;
				}
			}
		}

		public bool TryReadInputs(out MachineInputs inputs, out string error)
		{
			inputs = new MachineInputs();
			lock(_sync)
			{
				try
				{
					if(!_client.Connected)
					{
						error = "PLC bağlı değil";
						return false;
					}

					byte[] buffer = new byte[DbSize];
					int code = _client.DBRead(DbNumber, DbStart, DbSize, buffer);
					LastResult = code;
					// As before T04: on a failed read the zeroed buffer yields Start = false.
					inputs.Start = S7.GetBitAt(buffer, 0, _bitMap().Start);
					if(code != 0)
					{
						inputs = new MachineInputs();
						error = _client.ErrorText(code);
						return false;
					}

					error = null;
					return true;
				}
				catch(Exception ex)
				{
					inputs = new MachineInputs();
					error = ex.Message;
					return false;
				}
			}
		}

		public void ForceSafeState()
		{
			try
			{
				string error;
				TryWriteOutputs(MachineOutputs.AllOff, out error);
			}
			catch(Exception)
			{
				// best effort, never throws
			}
		}
	}
}
