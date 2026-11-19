using System;
using System.IO.Ports;

namespace RealVisionLab.Io
{
	/// <summary>Bit positions in the UART I/O byte (Settings.Lamba/Piston/Kase/bosBit/buton/reset_buton).</summary>
	public struct UartBitMap
	{
		public int Lamba;
		public int Piston;
		public int Kase;
		public int Bos;
		public int Start;
		public int Reset;
	}

	/// <summary>
	/// UART I/O board backend: one byte = 8 outputs (bit i = 1 &lt;&lt; i). Every SerialPort
	/// read/write done through this class is under a private lock.
	/// Port selection/open/reconnect stays in MainPage.Uart_connect (T07).
	/// </summary>
	public sealed class UartMachineIo : IMachineIo
	{
		private readonly object _sync = new object();
		private readonly SerialPort _port;
		private readonly Func<UartBitMap> _bitMap;
		private MachineInputs _lastInputs;

		public UartMachineIo(SerialPort port, Func<UartBitMap> bitMap)
		{
			if(port == null) throw new ArgumentNullException("port");
			if(bitMap == null) throw new ArgumentNullException("bitMap");
			_port = port;
			_bitMap = bitMap;
		}

		public bool IsConnected
		{
			get
			{
				lock(_sync)
				{
					try { return _port.IsOpen; }
					catch(Exception) { return false; }
				}
			}
		}

		/// <summary>Builds the output byte exactly as the pre-T04 code (fresh 8-bit array, assignment order Lamba, Piston, Kase, Bos).</summary>
		public static byte BuildOutputByte(MachineOutputs outputs, UartBitMap map)
		{
			// All-off is 0x00 regardless of the mapping (same byte the old Reset wrote literally).
			if(outputs.Equals(MachineOutputs.AllOff)) return 0x00;

			bool[] bits = new bool[8];
			bits[map.Lamba] = outputs.Lamba;
			bits[map.Piston] = outputs.Piston;
			bits[map.Kase] = outputs.Kase;
			bits[map.Bos] = outputs.Bos;
			return ConvertToByte(bits);
		}

		/// <summary>Moved unchanged from MainPage.ConvertToByte: bit i -> (1 &lt;&lt; i), padded/truncated to 8 bits.</summary>
		public static byte ConvertToByte(bool[] bits)
		{
			if(bits.Length != 8)
			{
				bool[] paddedBits = new bool[8];
				for(int i = 0; i < 8; i++)
				{
					paddedBits[i] = i < bits.Length && bits[i];
				}
				bits = paddedBits;
			}

			byte result = 0;
			for(int i = 0; i < 8; i++)
			{
				if(bits[i])
				{
					result |= (byte)(1 << i);
				}
			}
			return result;
		}

		public bool TryWriteOutputs(MachineOutputs outputs, out string error)
		{
			lock(_sync)
			{
				try
				{
					if(!_port.IsOpen)
					{
						error = "UART portu açık değil";
						return false;
					}

					byte value = BuildOutputByte(outputs, _bitMap());
					_port.Write(new byte[] { value }, 0, 1);
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

		/// <summary>
		/// Connect probe used only by MainPage.Uart_connect: writes 0x00 under the lock and
		/// THROWS on failure, exactly like the former direct SerialPort.Write (the caller's
		/// try/catch/finally flow depends on that; T07 owns that logic).
		/// </summary>
		public void WriteConnectProbe()
		{
			lock(_sync)
			{
				_port.Write(new byte[] { 0x00 }, 0, 1);
			}
		}

		/// <summary>
		/// Decodes a received frame (moved from MainPage.Uart_to_IO_DataReceived, same logic):
		/// first character of ReadExisting() is the input byte, buffers are discarded afterwards.
		/// </summary>
		public bool TryReceive(out MachineInputs inputs, out bool[] bits, out string error)
		{
			inputs = new MachineInputs();
			bits = new bool[8];
			lock(_sync)
			{
				try
				{
					string receiveText = _port.ReadExisting();
					int receiveDec = Convert.ToByte(receiveText[0]);

					for(int i = 0; i < 8; i++)
					{
						bits[i] = (receiveDec & (1 << i)) != 0;
					}
					_port.DiscardInBuffer();
					_port.DiscardOutBuffer();

					UartBitMap map = _bitMap();
					inputs.Start = bits[map.Start];
					inputs.Reset = bits[map.Reset];
					_lastInputs = inputs;
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

		/// <summary>
		/// UART inputs are event driven (DataReceived -> TryReceive). This returns the inputs
		/// decoded from the last received frame; it does not touch the port.
		/// </summary>
		public bool TryReadInputs(out MachineInputs inputs, out string error)
		{
			lock(_sync)
			{
				inputs = _lastInputs;
				error = null;
				return true;
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
