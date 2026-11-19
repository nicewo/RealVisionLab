using System;
using System.Drawing;

namespace RealVisionLab
{
	/// <summary>
	/// Latest camera frame per camera index (T06). Written by the AForge camera threads, read by the
	/// inspection cycle. Every stored frame and every returned frame is an independent copy, so no
	/// Bitmap instance is ever shared between threads (GDI+ objects are not thread-safe).
	/// </summary>
	/// <remarks>
	/// Copies are made with <c>new Bitmap(Image)</c>, i.e. Format32bppArgb, exactly as the pre-T06
	/// code produced them (<c>new Bitmap(tempFrame)</c> in New_Frame, <c>new Bitmap(pb_scene.Image)</c>
	/// in Islemler). The pixel data seen by the inspection functions is therefore unchanged.
	/// </remarks>
	public sealed class FrameBuffer : IDisposable
	{
		private readonly object _lock = new object();
		private Bitmap[] _frames;
		private long[] _sequence;
		private bool _disposed;

		public FrameBuffer(int cameraCount)
		{
			if(cameraCount < 0) throw new ArgumentOutOfRangeException("cameraCount");
			_frames = new Bitmap[cameraCount];
			_sequence = new long[cameraCount];
		}

		public int CameraCount
		{
			get { lock(_lock) { return _frames.Length; } }
		}

		/// <summary>
		/// Stores a copy of <paramref name="frame"/> as the latest frame of the camera and returns its
		/// sequence number (1, 2, ...). The caller keeps ownership of <paramref name="frame"/>.
		/// Returns 0 (nothing stored) for an out-of-range index, a null frame or after Dispose.
		/// </summary>
		public long Store(int cameraIndex, Bitmap frame)
		{
			if(frame == null) return 0;
			// Expensive copy outside the lock; only the swap is locked.
			Bitmap copy = new Bitmap(frame);
			Bitmap old = null;
			long seq = 0;
			lock(_lock)
			{
				if(!_disposed && cameraIndex >= 0 && cameraIndex < _frames.Length)
				{
					old = _frames[cameraIndex];
					_frames[cameraIndex] = copy;
					seq = ++_sequence[cameraIndex];
					copy = null;
				}
			}
			// Safe outside the lock: 'old' is no longer reachable through the buffer, and every reader
			// copies under the lock.
			if(old != null) old.Dispose();
			if(copy != null) copy.Dispose();
			return seq;
		}

		/// <summary>Sequence number of the latest stored frame (0 = none yet).</summary>
		public long GetSequence(int cameraIndex)
		{
			lock(_lock)
			{
				if(cameraIndex < 0 || cameraIndex >= _sequence.Length) return 0;
				return _sequence[cameraIndex];
			}
		}

		/// <summary>A new copy of the latest frame (owned by the caller), or null if there is none.</summary>
		public Bitmap GetLatest(int cameraIndex)
		{
			long sequence;
			return GetLatest(cameraIndex, out sequence);
		}

		/// <summary>A new copy of the latest frame (owned by the caller) and its sequence number, or null / 0.</summary>
		public Bitmap GetLatest(int cameraIndex, out long sequence)
		{
			lock(_lock)
			{
				sequence = 0;
				if(_disposed || cameraIndex < 0 || cameraIndex >= _frames.Length) return null;
				Bitmap latest = _frames[cameraIndex];
				if(latest == null) return null;
				sequence = _sequence[cameraIndex];
				return new Bitmap(latest);
			}
		}

		public void Dispose()
		{
			Bitmap[] frames;
			lock(_lock)
			{
				if(_disposed) return;
				_disposed = true;
				frames = _frames;
				_frames = new Bitmap[0];
				_sequence = new long[0];
			}
			foreach(Bitmap b in frames)
			{
				if(b != null) b.Dispose();
			}
		}
	}
}
