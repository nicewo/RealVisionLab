using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace RealVisionLab.Safety
{
	/// <summary>
	/// Limits automatic restarts after an unhandled exception (T05).
	/// Restart timestamps are stored one per line (ISO 8601, UTC) in
	/// %LOCALAPPDATA%\RealVisionLab\restarts.txt. When <see cref="MaxRestarts"/> restarts were already
	/// recorded inside <see cref="Window"/>, no further restart is allowed.
	/// </summary>
	public static class RestartGuard
	{
		public const int MaxRestarts = 3;
		public static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

		public static string DefaultPath
		{
			get
			{
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"RealVisionLab", "restarts.txt");
			}
		}

		/// <summary>
		/// Returns true (and records <paramref name="nowUtc"/>) when a restart is allowed.
		/// Returns false when <see cref="MaxRestarts"/> restarts already happened within <see cref="Window"/>.
		/// If the file cannot be read or written, restarting is refused (fail-safe: no restart loop).
		/// </summary>
		public static bool TryRegisterRestart(string path, DateTime nowUtc)
		{
			try
			{
				List<DateTime> recent = new List<DateTime>();
				if(File.Exists(path))
				{
					foreach(string line in File.ReadAllLines(path))
					{
						DateTime t;
						if(DateTime.TryParse(line.Trim(), CultureInfo.InvariantCulture,
							DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out t)
							&& nowUtc - t < Window && t <= nowUtc)
						{
							recent.Add(t);
						}
					}
				}

				bool allowed = recent.Count < MaxRestarts;
				if(allowed) recent.Add(nowUtc);

				string dir = Path.GetDirectoryName(path);
				if(!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
				List<string> lines = new List<string>();
				foreach(DateTime t in recent) lines.Add(t.ToString("o", CultureInfo.InvariantCulture));
				File.WriteAllLines(path, lines);
				return allowed;
			}
			catch(Exception)
			{
				return false;
			}
		}
	}
}
