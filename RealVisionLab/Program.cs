using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RealVisionLab.Safety;

namespace RealVisionLab
{
    internal static class Program
    {
        public static MainPage mainPage;

        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [STAThread]
        static void Main(string[] args)
        {
            // T08: "RealVisionLab.exe --hash-password <value>" prints a hash (for serviceCodeHash) and exits.
            if (args != null && args.Length >= 1 && args[0] == "--hash-password")
            {
                AttachConsole(-1);
                if (args.Length < 2) Console.WriteLine("Kullanim: RealVisionLab.exe --hash-password <deger>");
                else Console.WriteLine(Security.PasswordHasher.Hash(args[1]));
                return;
            }

            // Non-UI thread exceptions (e.g. camera NewFrame thread): log + outputs safe state (T05).
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mainPage = new MainPage();
            Application.ThreadException += Application_ThreadException;
            Application.Run(mainPage);
        }

        private static void WriteCrashLog(string source, Exception ex)
        {
            string newValue = ex == null
                ? "Hata: (bilinmeyen)"
                : "Hata: " + ex.Message + " -- Hata detayları: " + ex.StackTrace;
            try
            {
                using (StreamWriter writer = File.AppendText("debug.txt"))
                {
                    writer.WriteLine($"\r\n--------\r\n {DateTime.Now} - {source} - Hata Kodu: {newValue}\r\n ------------\r\n");
                }
            }
            catch
            {
            }
        }

        /// <summary>All machine outputs off. Never throws.</summary>
        private static void ForceSafeState()
        {
            try
            {
                if (mainPage != null) mainPage.ForceSafeState();
            }
            catch
            {
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ForceSafeState();
            WriteCrashLog("ThreadException", e.Exception);

            if (RestartGuard.TryRegisterRestart(RestartGuard.DefaultPath, DateTime.UtcNow))
            {
                Application.Restart();
                return;
            }

            WriteCrashLog("RestartGuard", new Exception(
                $"{RestartGuard.MaxRestarts} yeniden başlatma / {RestartGuard.Window.TotalSeconds} sn sınırı aşıldı; uygulama kapatılıyor."));
            try
            {
                MessageBox.Show(
                    "Uygulama kısa sürede tekrar tekrar hata verdi ve otomatik olarak yeniden başlatılmayacak.\r\n" +
                    "Çıkışlar kapatıldı. Ayrıntılar debug.txt dosyasında.\r\n\r\n" + e.Exception.Message,
                    "RealVisionLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
            ForceSafeState();
            Environment.Exit(1);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Called on the faulting thread; the process terminates afterwards (IsTerminating).
            // IMachineIo implementations are thread-safe (T04), so the safe state is written from here.
            ForceSafeState();
            WriteCrashLog("UnhandledException (terminating=" + e.IsTerminating + ")", e.ExceptionObject as Exception);
        }
    }
}
