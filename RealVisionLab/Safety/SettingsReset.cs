using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using RealVisionLab.Properties;

namespace RealVisionLab.Safety
{
	/// <summary>
	/// Guarded factory reset of Properties.Settings (T05). Only reachable from the Property screen in admin mode.
	/// Order: Yes/No confirmation -> backup of Rect/Cam/Bekleme -> Settings.Default.Reset().
	/// If the backup cannot be written, the reset is not performed.
	/// </summary>
	public static class SettingsReset
	{
		/// <summary>Writes settings-backup-&lt;timestamp&gt;.txt next to the exe. Returns the path.</summary>
		public static string WriteBackup(string directory, DateTime now, string rect, string cam, string bekleme)
		{
			string path = Path.Combine(directory, "settings-backup-" + now.ToString("yyyyMMdd_HHmmss") + ".txt");
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("# RealVisionLab settings backup " + now.ToString("yyyy-MM-dd HH:mm:ss"));
			sb.AppendLine("Rect=" + rect);
			sb.AppendLine("Cam=" + cam);
			sb.AppendLine("Bekleme=" + bekleme);
			File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
			return path;
		}

		/// <summary>Returns true when the settings were reset.</summary>
		public static bool ConfirmBackupAndReset(IWin32Window owner)
		{
			DialogResult answer = MessageBox.Show(owner,
				"Tüm ayarlar (ROI, kamera, PLC/UART, bekleme) varsayılana döndürülecek.\r\nDevam etmek istiyor musunuz?",
				"Ayarları Sıfırla", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
			if(answer != DialogResult.Yes) return false;

			string backupPath;
			try
			{
				backupPath = WriteBackup(AppDomain.CurrentDomain.BaseDirectory, DateTime.Now,
					Settings.Default.Rect, Settings.Default.Cam, Settings.Default.Bekleme);
			}
			catch(Exception ex)
			{
				MessageBox.Show(owner, "Yedek alınamadı, ayarlar sıfırlanmadı.\r\n" + ex.Message,
					"Ayarları Sıfırla", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			Settings.Default.Reset();
			MessageBox.Show(owner, "Ayarlar sıfırlandı. Yedek: " + backupPath + "\r\nUygulamayı yeniden başlatın.",
				"Ayarları Sıfırla", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return true;
		}
	}
}
