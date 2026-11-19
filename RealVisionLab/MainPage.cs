using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using DirectShowLib;
using RealVisionLab.Io;
using RealVisionLab.Properties;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using ZXing;





namespace RealVisionLab
{
	public partial class MainPage : Form
	{

#if DEBUG
		bool debugMode = true;
#else
        bool debugMode = false;
#endif

		//---------- Değişken Tanımlamaları Başlangıç ------------------------------------------------------
		private Property set;

		// Machine I/O (T04). All PLC/UART output writes and input reads go through these.
		// io      : composite of the enabled backends, or the simulator when Settings.SimulateIo.
		// plcIo   : S7 backend only (or simulator channel "PLC") for PLC-only writes kept from pre-T04 code.
		// uartIo  : UART backend only (or simulator channel "UART").
		private S7MachineIo s7Io;
		private UartMachineIo uartPort;
		private SimulatedMachineIo simIo;
		private IMachineIo io, plcIo, uartIo;

		// Thread safety (T06). Camera threads store frames here; the inspection cycle reads copies.
		// pb_scene.Image is display only and is set/disposed on the UI thread only (ShowScene).
		private FrameBuffer frameBuffer;
		private readonly int uiThreadId;
		// Frame size of the camera image under inspection (set by Islemler before each tarama function).
		private Size scanFrameSize;

		public static VideoCaptureDevice[]
			cam;

		public static FilterInfoCollection
			devices = new FilterInfoCollection(AForge.Video.DirectShow.FilterCategory.VideoInputDevice);

		public static Label[]
		   artcle;

		public static Label[,]
			lbl;

		public static PictureBox[]
			pbox;

		public static Color
			blk = Color.FromArgb(255, 0, 0, 0),
			wht = Color.FromArgb(255, 255, 255, 255),
			gry = Color.FromArgb(255, 128, 128, 128),
			red = Color.FromArgb(255, 255, 0, 0),
			org = Color.FromArgb(255, 255, 165, 0),
			grn = Color.FromArgb(255, 0, 255, 0),
			ylw = Color.FromArgb(255, 255, 255, 0);

		public static Bitmap[]
			bmp;
		public static Rectangle[]
			Rect;


		public static bool
			capture = false,
			buton = false,
			rst_buton = false,
			alarm = false,
			capturing = true,
			islem = false,
			rotuzunlukvisible = false,
			gThrs = false,
			qr = false,
		public static bool[]
			cam_var;
		public static bool[,]
			hata;
		public static int
			tab_index = 0,
			alarmtime = 0,
			capture_count = 0,
			tarama_count = 0,
			caming_id = 0,
			gthresint = 128,
			renk = 0,
			newFileIndex,
			uartCount = 0;
		public static int[]
			Cam_no,
			Cam_res,
			Thrs;
		public static int[,]
			Ofset;
		public static float[,]
			oran;
		public static string
			reportold,
			report,
			txtReport,
			debug,
			folderPath = "Values",
			filePath = "",
			qrCodeText;
		public static string[]
			rectString,
			thrsString;


		//---------- Değişken Tanımlamaları Bitiş --------------------------------------------------------------------

		public MainPage()
		{
			try
			{
				if(debugMode)
				{
					this.WindowState = FormWindowState.Normal;
				}
				else
				{
					this.WindowState = FormWindowState.Maximized;
				}
				// T06: UI controls are only touched on this thread; other threads go through RunOnUi.
				uiThreadId = Thread.CurrentThread.ManagedThreadId;
				InitializeComponent();
				ApplyCameraSettingsToAll();

				InitMachineIo();

				string camString = Settings.Default.Cam;
				Cam_no = new int[camString.Split('/').Length];
				Cam_res = new int[camString.Split('/').Length];
				cam_var = new bool[camString.Split('/').Length];
				oran = new float[camString.Split('/').Length, 2];
				cam = new VideoCaptureDevice[camString.Split('/').Length];
				// Created before Cam_adjust starts the cameras (New_Frame stores into it).
				frameBuffer = new FrameBuffer(cam.Length);

				visibility(false);
				Cam_adjust();

				Label_adjust(org);

				if(!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				timer.Start();
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}
		private void MainPage_Load(object sender, EventArgs e)
		{
			lbl_productName.Text = Settings.Default.productName;
			filePath = folderPath + "/" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";
			Settings.Default.adminMode = false;
			Settings.Default.Save();
			try
			{
				if(File.Exists(filePath))
				{
					string fileContent = File.ReadAllText(filePath);
					string[] contentString = fileContent.Split('/');
					int id = 1;
					if(fileContent.StartsWith("/"))
					{
						id++;
					}

					if(fileContent.EndsWith("/"))
					{
						id++;
					}

					tarama_count = int.Parse(contentString[contentString.Length - id].Split(',')[0].Split('-')[1]);
					tarama_count++;
				}
				else
				{
					File.Create(filePath).Close();
					tarama_count = 0;
				}
			}
			catch(Exception ex)
			{
				tx_debug.Text = "Load Error: " + ex.Message;
			}
			if(!debugMode)
			{
				Photo.Visible = false;
				sec.Visible = false;
				iptal.Visible = false;
				txt_qr.Visible = false;
				txt_qr2.Visible = false;
			}
		}
		private void ClearControls()
		{
			try
			{
				foreach(Control control in pb_scene.Controls)
				{
					control.Dispose(); // Nesneyi bellekten kaldırma
				}
				pb_scene.Controls.Clear(); // Kontrol listesini temizleme

				// Rect, pbox, bmp, lbl, Ofset dizilerini boşaltma
				Rect = null;
				pbox = null;
				bmp = null;
				lbl = null;
				Ofset = null;
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}
		public void Label_adjust(Color color)
		{
			try
			{
				string[] rectString = Settings.Default.Rect.Split('/');
				lbl_productName.Text = Settings.Default.productName;
				if(Rect == null || rectString.Length != Rect.Length)
				{

					ClearControls();
					hata = new bool[cam.Length, rectString.Length];
					Rect = new Rectangle[rectString.Length];
					pbox = new PictureBox[Rect.Length];
					bmp = new Bitmap[Rect.Length];
					lbl = new Label[Rect.Length, 4];
					Ofset = new int[Rect.Length, 3];
					Thrs = new int[Rect.Length];



					for(int r = 0; r < rectString.Length; r++)
					{
						for(int c = 0; c < cam.Length; c++)
						{
							hata[c, r] = false;
						}
						lbl[r, 0] = new Label();
						lbl[r, 1] = new Label();
						lbl[r, 2] = new Label();
						lbl[r, 3] = new Label();
						pbox[r] = new PictureBox();
						//top
						lbl[r, 0].Height = 2;
						lbl[r, 0].BackColor = color;
						pb_scene.Controls.Add(lbl[r, 0]);
						//right
						lbl[r, 1].Width = 2;
						lbl[r, 1].BackColor = color;
						pb_scene.Controls.Add(lbl[r, 1]);
						//bottom
						lbl[r, 2].Height = 2;
						lbl[r, 2].BackColor = color;
						pb_scene.Controls.Add(lbl[r, 2]);
						//left
						lbl[r, 3].Width = 2;
						lbl[r, 3].BackColor = color;
						pb_scene.Controls.Add(lbl[r, 3]);

						pbox[r].SizeMode = PictureBoxSizeMode.StretchImage;
						pbox[r].BackColor = color;
						pbox[r].MouseClick += pb_scene_MouseClick;
						pb_scene.Controls.Add(pbox[r]);
					}
				}
				int rect_cam;
				for(int i = 0; i < rectString.Length; i++)
				{
					Thrs[i] = int.Parse(rectString[i].Split(',')[8]);
					Rect[i].X = int.Parse(rectString[i].Split(',')[0]);
					Rect[i].Y = int.Parse(rectString[i].Split(',')[1]);
					Rect[i].Width = int.Parse(rectString[i].Split(',')[2]);
					Rect[i].Height = int.Parse(rectString[i].Split(',')[3]);
					Ofset[i, 0] = int.Parse(rectString[i].Split(',')[4]);
					Ofset[i, 2] = int.Parse(rectString[i].Split(',')[5]);
					rect_cam = int.Parse(rectString[i].Split(',')[6]);
					//top
					lbl[i, 0].Left = (int)(Rect[i].X / oran[rect_cam, 1]);
					lbl[i, 0].Top = (int)(Rect[i].Y / oran[rect_cam, 0]);
					lbl[i, 0].Width = (int)(Rect[i].Width / oran[rect_cam, 1]);
					lbl[i, 0].BringToFront();
					//right
					lbl[i, 1].Left = lbl[i, 0].Left + lbl[i, 0].Width;
					lbl[i, 1].Top = lbl[i, 0].Top;
					lbl[i, 1].Height = (int)(Rect[i].Height / oran[rect_cam, 0]);
					lbl[i, 1].BringToFront();
					//bottom
					lbl[i, 2].Left = lbl[i, 0].Left;
					lbl[i, 2].Top = lbl[i, 0].Top + lbl[i, 1].Height;
					lbl[i, 2].Width = lbl[i, 0].Width + 2;
					lbl[i, 2].BringToFront();
					//left
					lbl[i, 3].Left = lbl[i, 0].Left;
					lbl[i, 3].Top = lbl[i, 0].Top;
					lbl[i, 3].Height = lbl[i, 1].Height;
					lbl[i, 3].BringToFront();
					//Mini PictureBox
					pbox[i].Left = lbl[i, 0].Left + 2;
					pbox[i].Top = lbl[i, 0].Top + 2;
					pbox[i].Width = lbl[i, 0].Width - 2;
					pbox[i].Height = lbl[i, 1].Height - 2;
					pbox[i].BringToFront();
				}

				if(artcle == null)
				{
					artcle = new Label[2];

					artcle[0] = new Label();
					artcle[1] = new Label();
					artcle[0].BackColor = org;
					artcle[1].BackColor = org;

					artcle[1].Width = 60;
					artcle[1].Height = 4;
					artcle[0].Width = 4;
					artcle[0].Height = 60;

					pb_scene.Controls.Add(artcle[0]);
					pb_scene.Controls.Add(artcle[1]);

				}

				artcle[0].Location = new Point((pb_scene.Width / 2) - 2, (pb_scene.Height / 2) - 30);
				artcle[1].Location = new Point((pb_scene.Width / 2) - 30, (pb_scene.Height / 2) - 2);

				artcle[0].BringToFront();
				artcle[1].BringToFront();


			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}
		public void Cam_adjust()
		{
			try
			{
				string camString = Settings.Default.Cam;

				for(int i = 0; i < camString.Split('/').Length; i++)
				{
					Cam_no[i] = int.Parse(camString.Split('/')[i].Split(',')[0]);
					Cam_res[i] = int.Parse(camString.Split('/')[i].Split(',')[1]);
					if(Cam_no[i] < 0) Cam_no[i] = 0;
					if(Cam_res[i] < 0) Cam_res[i] = 0;
					capture = false;

					if(cam[i] != null && cam[i].IsRunning)
					{
						cam[i].SignalToStop();
						cam[i].WaitForStop();
					}
				}
				for(int i = 0; i < cam_var.Length; i++)
				{
					if(devices.Count > Cam_no[i]) cam_var[i] = true;
					else cam_var[i] = false;
				}


				for(int i = 0; i < cam_var.Length; i++)
				{

					if(cam_var[i])
					{
						try
						{
							// Kamera 1 Başlat 
							cam[i] = new VideoCaptureDevice(devices[Cam_no[i]].MonikerString);
							if(cam[i].VideoCapabilities.Length - 1 < Cam_res[i]) { Cam_res[i] = cam[i].VideoCapabilities.Length - 1; }
							cam[i].VideoResolution = cam[i].VideoCapabilities[Cam_res[i]];
							cam[i].NewFrame += New_Frame;
							cam[i].Start();

							oran[i, 0] = (float)cam[i].VideoResolution.FrameSize.Height / (float)pb_scene.Height;
							pb_scene.Width = (int)(cam[0].VideoResolution.FrameSize.Width / oran[0, 0]);
							oran[i, 1] = (float)cam[i].VideoResolution.FrameSize.Width / (float)pb_scene.Width;
							p_scene.Width = pb_scene.Width + 20;
							p_scene.Left = (Width - p_scene.Width) / 2;
							p_scene.Top = ((Height - (p_scene.Height + lbl_productName.Height + panel2.Height)) / 2) + lbl_productName.Height;
						}
						catch(Exception ex)
						{
							UiLog(ex.ToString() + "\r\n");
						}
					}
					if(cam[i] != null && cam[i].IsRunning)
					{
						lbl_alarm.ForeColor = org;
						lbl_alarm.Text = "HAZIR";
						p_scene.BackColor = org;
					}
					if(!cam_var[i])
					{
						lbl_alarm.ForeColor = red;
						lbl_alarm.Text = "Kamera " + (i + 1).ToString() + " Bulunamadı";
						p_scene.BackColor = red;
					}
				}
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}
		private async void Reset()
		{
			try
			{
				qrGelen.Visible = false;
				qrGelen.Text = "";

				string[] rectString = Settings.Default.Rect.Split('/');
				buton = false;
				capturing = true;
				islem = false;

				for(int c = 0; c < cam.Length; c++)
				{
					for(int i = 0; i < rectString.Length; i++)
					{
						hata[c, i] = false;
					}
				}

				visibility(false);

				// All outputs off on every connected backend; reconnect the ones that are down (as before T04).
				WriteOutputs(io, MachineOutputs.AllOff);
				if(simIo == null)
				{
					if(!uartPort.IsConnected) Uart_connect();
					if(!s7Io.IsConnected) Uart_connect();
				}

				timer.Start();
				await Task.Delay(int.Parse(Settings.Default.Bekleme.Split('-')[10]));
				lbl_alarm.Text = "HAZIR";
				lbl_alarm.ForeColor = org;
				p_scene.BackColor = org;
				caming_id = 0;
			}
			catch(Exception exception)
			{
				UiLog(exception.ToString());
			}
		}
		// T07: connect to the configured port with timeouts. Nothing is written unless Open() succeeded;
		// no UI-thread sleep. UartPort empty = legacy behaviour (last COM port, with a log warning).
		private void UartConnectCore()
		{
			if(Uart_to_IO.IsOpen)
			{
				// Already open: connect probe (0x00 = all outputs off); throws on failure -> caller logs.
				uartPort.WriteConnectProbe();
				port_text.Text = "Çıkış Cihazı Bağlandı (UART)";
				port_text.ForeColor = grn;
				return;
			}

			string[] ports = SerialPort.GetPortNames();
			if(ports.Length == 0)
			{
				lbl_alarm.Text = "Cihaz bulunamadı";
				lbl_alarm.ForeColor = red;
				port_text.Text = "Çıkış Cihazı Bağlanamadı";
				port_text.ForeColor = red;
				TryLaunchUartResetTool("COM port yok");
				return;
			}

			string portName;
			string configured = (Settings.Default.UartPort ?? "").Trim();
			if(configured.Length == 0)
			{
				portName = ports[ports.Length - 1];
				UiLog("UART uyarı: UartPort ayarı boş, eski davranış: son COM port (" + portName + ") kullanılıyor.");
			}
			else
			{
				portName = ports.FirstOrDefault(x => string.Equals(x, configured, StringComparison.OrdinalIgnoreCase));
				if(portName == null)
				{
					lbl_alarm.Text = "Cihaz bulunamadı";
					lbl_alarm.ForeColor = red;
					port_text.Text = "Çıkış Cihazı Bağlanamadı (" + configured + " yok)";
					port_text.ForeColor = red;
					return;
				}
			}

			port_text.Text = "Çıkış Cihazı Bağlanamadı";
			port_text.ForeColor = red;
			lbl_alarm.Text = "Bağlanıyor...";
			lbl_alarm.ForeColor = org;
			Uart_to_IO.DataReceived -= Uart_to_IO_DataReceived;
			Uart_to_IO.PortName = portName;
			Uart_to_IO.BaudRate = Settings.Default.baud;
			Uart_to_IO.ReadTimeout = 500;
			Uart_to_IO.WriteTimeout = 500;

			try
			{
				Uart_to_IO.Open();
			}
			catch(Exception ex)
			{
				// Disconnected: no probe/write, no handler attached.
				UiLog("UART açılamadı (" + portName + "): " + ex.Message);
				TryLaunchUartResetTool("Open() başarısız: " + portName);
				return;
			}

			try
			{
				Uart_to_IO.DataReceived += Uart_to_IO_DataReceived;
				uartPort.WriteConnectProbe();
				port_text.Text = "Çıkış Cihazı Bağlandı";
				port_text.ForeColor = grn;
			}
			catch(Exception ex)
			{
				UiLog("UART bağlantı yazımı başarısız (" + portName + "): " + ex.Message);
				Uart_to_IO.DataReceived -= Uart_to_IO_DataReceived;
				try { Uart_to_IO.Close(); } catch(Exception) { }
				port_text.Text = "Çıkış Cihazı Bağlanamadı";
				port_text.ForeColor = red;
			}
		}

		// T07: external reset tool only when explicitly enabled and present; every launch is logged.
		private void TryLaunchUartResetTool(string reason)
		{
			if(!Settings.Default.UartResetToolEnabled) return;
			try
			{
				string path = Settings.Default.UartResetToolPath;
				if(string.IsNullOrWhiteSpace(path) || !File.Exists(path))
				{
					UiLog("UART reset aracı bulunamadı: " + path);
					return;
				}
				if(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)).Length > 0) return;
				UiLog("UART reset aracı başlatılıyor (" + reason + "): " + path);
				ProcessStartInfo psi = new ProcessStartInfo();
				psi.FileName = path;
				psi.UseShellExecute = true;
				psi.Verb = "runas";
				Process.Start(psi);
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "
");
			}
		}
		private void Uart_connect()
		{
			if(simIo != null)
			{
				// Settings.SimulateIo: no real UART/PLC connection is attempted.
				port_text.Text = "SİMÜLASYON";
				return;
			}
			try
			{
				if(Settings.Default.UartEnable) UartConnectCore();
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
			if(Settings.Default.PlcEnable)
			{
				int connectResult;
				string plcError;
				s7Io.TryConnect(Settings.Default.PIp, Settings.Default.PRack, Settings.Default.PSlot, out connectResult, out plcError);
				port_text.Text = connectResult.ToString();
				if(connectResult == 0)
				{
					if(!s7Io.TryWriteOutputs(MachineOutputs.AllOff, out plcError)) port_text.Text = plcError;
					else
					{
						port_text.Text = "Çıkış Cihazı Bağlandı (ETH)";
						port_text.ForeColor = grn;
					}
				}
				else
				{
					port_text.Text = plcError;
				}
			}


		}
		public void visibility_set(int tab)
		{
			try
			{
				Label_adjust(org);
				string[] rectStringv = Settings.Default.Rect.Split('/');
				for(int i = 0; i < rectStringv.Length; i++)
				{
					if(int.Parse(rectStringv[i].Split(',')[6]) == tab)
					{
						for(int j = 0; j < 4; j++)
						{
							lbl[i, j].Visible = true;
						}
						if(pbox[i].Image != null)
							pbox[i].Visible = true;
					}
					else
					{
						for(int j = 0; j < 4; j++)
						{
							lbl[i, j].Visible = false;
						}
						pbox[i].Visible = false;
					}
				}
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}
		public void visibility(bool vis)
		{
			try
			{
				Label_adjust(org);
				string[] rectStringv = Settings.Default.Rect.Split('/');
				for(int i = 0; i < rectStringv.Length; i++)
				{
					for(int j = 0; j < 4; j++)
					{
						lbl[i, j].Visible = vis;
					}
					pbox[i].Visible = debugMode;
				}
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}
		// Runs on the AForge camera thread (one per camera). Never touches UI controls directly (T06).
		private void New_Frame(object sender, NewFrameEventArgs e)
		{
			try
			{
				VideoCaptureDevice[] cams = cam;
				int camIndex = cams == null ? -1 : Array.IndexOf(cams, sender as VideoCaptureDevice);
				if(camIndex < 0) return;

				// Inspection input: latest frame of this camera. With a test photo (debug 'sec' button)
				// the photo replaces the camera image, as before T06.
				Bitmap photo = takePhoto;
				if(photo == null)
				{
					frameBuffer.Store(camIndex, e.Frame);
				}
				else
				{
					// Several camera threads may copy the same photo; GDI+ bitmaps are not thread-safe.
					lock(takePhotoLock)
					{
						frameBuffer.Store(camIndex, photo);
					}
				}

				// Display: one frame per capture request, from the camera being shown (as before T06).
				if(capture && camIndex == caming_id)
				{
					capture = false;
					Bitmap display = frameBuffer.GetLatest(camIndex);
					if(display != null)
					{
						if(gThrs)
						{
							// Threshold uygulaması (display only)
							Bitmap thresholded = Threshold_Apply(display, gthresint, renk);
							display.Dispose();
							display = thresholded;
						}
						ShowScene(display);
					}
				}
			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}

		private DateTime startTime;
		private void Main_timer_Tick(object sender, EventArgs e)
		{
			try
			{
				if(!rotuzunlukvisible)
				{
					startTime = DateTime.Now;
					RotUzunluk.Visible = false;
					RotUzunluk.Text = "";
				}
				else
				{
					RotUzunluk.Visible = true;
					if((DateTime.Now - startTime).TotalSeconds >= 10)
					{
						rotuzunlukvisible = false;
					}
				}
				txt_qr2.Text = "Reset:" + rst_buton + " - start:" + buton;
				if(rst_buton)
				{
					rst_buton = false;
					Reset();
				}

				if(qrGelen.Visible) { qrGelen.Visible = false; }
				if(buton)
				{
					buton = false;
					string blockReason;
					if(islem || CanStartCycle(out blockReason))
					{
						// islem: start while a cycle is pending/running keeps the pre-T05 behavior.
						capturing = false;
						capture_count = 0;
						islem = true;
						lastStartBlockLogged = null;
					}
					else
					{
						// T05: start refused (camera not running or enabled I/O backend disconnected).
						ForceSafeState();
						lbl_alarm.Text = "Başlatılamadı: " + blockReason;
						lbl_alarm.ForeColor = red;
						p_scene.BackColor = red;
						if(lastStartBlockLogged != blockReason)
						{
							lastStartBlockLogged = blockReason;
							tx_debug.Text = "Çevrim başlatma reddedildi: " + blockReason + " - " + DateTime.Now.ToString("HH:mm:ss.fff");
						}
					}
				}
				else
				{
					capture = true;
				}

				// T05: I/O backend dropped while a cycle is pending/running -> abort as NOK + safe state.
				if(islem)
				{
					string ioReason;
					if(!IoBackendsConnected(out ioReason))
					{
						AbortCycle(ioReason);
						return;
					}
				}

				if(!capturing)
				{
					if(capture_count > 3)
					{
						Islemler();
						alarmtime = 0;
						capturing = true;
					}
					else
					{
						capture = true;
						capture_count++;
					}
				}

				string readyReason;
				if(!islem && !CanStartCycle(out readyReason))
				{
					// T05: never show HAZIR while a start would be refused; show the reason instead.
					if(!lbl_alarm.Text.EndsWith(readyReason))
					{
						lbl_alarm.Text = "Hazır değil: " + readyReason;
						lbl_alarm.ForeColor = red;
						p_scene.BackColor = red;
					}
				}
				else if(lbl_alarm.Text != "HAZIR" && (simIo != null ? simIo.IsConnected : uartPort.IsConnected))
				{
					lbl_alarm.Text = "HAZIR";
					lbl_alarm.ForeColor = org;
					p_scene.BackColor = org;
				}

				lbl_datetime.Text = DateTime.Now.ToString();
				alarm = false;
				if(simIo != null)
				{
					// Simulated inputs: Start behaves like the PLC start bit (level), Reset like the UART reset bit.
					MachineInputs simInputs;
					string simError;
					if(simIo.TryReadInputs(out simInputs, out simError))
					{
						buton = simInputs.Start;
						if(simInputs.Reset) rst_buton = true;
					}
					else
					{
						port_text.Text = simError;
					}
				}
				else
				{
					if(Settings.Default.UartEnable)
					{
						if(!Uart_to_IO.IsOpen)
						{
							uartCount++;
							port_text.Text = "Çıkış Cihazı Bağlanamadı (UART)";
							Uart_to_IO.DataReceived -= Uart_to_IO_DataReceived;
							port_text.ForeColor = red;
						}
						else
						{
							uartCount = 0;
						}

						if(uartCount > 15)
						{
							uartCount = 0;
							Uart_connect();
						}
					}
					if(Settings.Default.PlcEnable)
					{
						if(!s7Io.IsConnected)
						{
							uartCount++;
							port_text.Text = "Çıkış Cihazı Bağlanamadı (ETH)";
							port_text.ForeColor = red;
						}
						else
						{
							uartCount = 0;
							// As before T04: a failed read yields Start = false; the error is not shown.
							MachineInputs plcInputs;
							string plcError;
							s7Io.TryReadInputs(out plcInputs, out plcError);
							buton = plcInputs.Start;
						}

						if(uartCount > 15)
						{
							uartCount = 0;
							Uart_connect();
						}
					}
				}

			}
			catch(Exception ex)
			{
				UiLog(ex.ToString() + "\r\n");
			}
		}

		private async void Islemler()
		{
			DateTime startTime = DateTime.Now;
			bool yon = false, sagMi = false;
			int[] yon_no = new int[2];
			yon_no[0] = 0;
			yon_no[1] = 0;
			try
			{
				cycleAborted = false;
				lbl_alarm.Text = "İşlem Başladı.";
				Bitmap[] bitmap = new Bitmap[cam.Length];
				bool isTrue = false;
				string[] bekleme = Settings.Default.Bekleme.Split('-');
				rectString = Settings.Default.Rect.Split('/');

				// Piston-Lamba => 1 ve 2 çıkış (UART and PLC)
				WriteOutputs(io, new MachineOutputs(lamba: true, piston: true, kase: false, bos: false));

				await Task.Delay(int.Parse(bekleme[0]));
				// T05: the timer runs during bekleme[0] and may have aborted the cycle; I/O may also have dropped.
				if(CycleAbortRequested()) return;

				timer.Stop();
				lbl_alarm.Text = "TARANIYOR";

				for(int c = 0; c < cam.Length; c++)
				{
					caming_id = c;
					int rectbno = 1;

					// Bounded frame wait (fail-closed): never block the cycle forever when no frame arrives.
					// Deadline = max(Bekleme[1] * 5, 5000) ms. On timeout every ROI of this camera is marked NOK below.
					int frameWaitMs = Math.Max(int.Parse(bekleme[1]) * 5, 5000);
					Stopwatch frameWatch = Stopwatch.StartNew();
					// T06: the frame comes from FrameBuffer (not pb_scene.Image) and must have arrived after
					// this request (sequence > requestSeq), i.e. after Lamba/Piston on + Bekleme[0].
					long requestSeq = frameBuffer.GetSequence(c);
					do
					{
						capture = true;
						await Task.Delay(int.Parse(bekleme[1]));
						long frameSeq;
						Bitmap latest = frameBuffer.GetLatest(c, out frameSeq);
						if(latest != null && frameSeq > requestSeq) bitmap[c] = latest;
						else latest?.Dispose();
					}
					while(bitmap[c] == null && frameWatch.ElapsedMilliseconds < frameWaitMs);
					// T05: the timer is stopped while scanning, so the I/O connection is checked at the await points.
					if(CycleAbortRequested()) return;

					bool frameMissing = bitmap[c] == null;
					if(frameMissing)
					{
						lbl_alarm.Text = $"Kamera {c + 1} görüntü yok";
						lbl_alarm.ForeColor = red;
						tx_debug.Text = $"Kamera {c + 1} görüntü yok ({frameWaitMs} ms içinde kare alınamadı) - {DateTime.Now:HH:mm:ss.fff}";
					}
					else
					{
						await Task.Delay(int.Parse(bekleme[2]));
					}

					lbl_islemTime.Text = "Tarama Başladı." + "( " + (DateTime.Now - startTime).TotalMilliseconds + " ms)";
					startTime = DateTime.Now;

					for(int i = 0; i < rectString.Length; i++)
					{
						startTime = DateTime.Now;
						if(caming_id == int.Parse(rectString[i].Split(',')[6]))
						{
							// Fail-closed: every evaluated ROI starts as NOK. Each tarama function clears it
							// (hata = false) only on a successful, in-range result; any exception, missing
							// frame or out-of-frame ROI therefore leaves the ROI NOK.
							// Exceptions: 9 (Cizgi_Ile_Lokasyon_Bul, locator) and 11 (Audi_alttan_uzat, helper)
							// do not measure against Min/Max; they clear hata only when they complete without error.
							// 19 (Btn_bekle, wait step) likewise clears hata on normal completion (pre-existing
							// semantics: a timeout without button press was never a NOK).
							hata[c, i] = true;
							if(rectString[i].Split(',')[10].Contains("yön"))
							{
								yon_no[0] = c;
								yon_no[1] = i;
							}
							if(i > 0)
							{
								if(rectString[i - 1].Split(',')[10].Contains("yön"))
								{
									yon = true;
									sagMi = hata[yon_no[0], yon_no[1]];
									hata[yon_no[0], yon_no[1]] = false;

								}
							}
							int islem_id = int.Parse(rectString[i].Split(',')[7]);
							bmp[i]?.Dispose();
							pbox[i].Image = null;
							bmp[i] = null;

							// ROI bounds check: Bitmap.Clone throws GDI+ OutOfMemoryException for a rectangle
							// that is not fully inside the frame. Such an ROI (or a missing frame) stays NOK
							// and its tarama function is skipped.
							bool roiInFrame = false;
							if(!frameMissing)
							{
								Rectangle frameBounds = new Rectangle(0, 0, bitmap[c].Width, bitmap[c].Height);
								Rectangle roiClip = Rectangle.Intersect(Rect[i], frameBounds);
								if(roiClip.IsEmpty || roiClip != Rect[i])
								{
									hata[c, i] = true;
									tx_debug.Text = $"ROI {i} out of frame (Rect={Rect[i]}, frame={frameBounds.Width}x{frameBounds.Height}) - {DateTime.Now:HH:mm:ss.fff}";
								}
								else
								{
									roiInFrame = true;
								}
							}

							if(roiInFrame)
							{
							bmp[i] = new Bitmap(bitmap[c].Clone(Rect[i], PixelFormat.Format24bppRgb));
							scanFrameSize = bitmap[c].Size;
							await Task.Run(async () =>
							{
								if(yon)
								{
									if((sagMi && (rectString[i].Split(',')[10].Contains("R-") || rectString[i].Split(',')[10].Contains("islem")))
												|| (!sagMi && (rectString[i].Split(',')[10].Contains("L-") || rectString[i].Split(',')[10].Contains("islem"))))
									{
										if(islem_id == 0)
											Kırmızı_say(bmp[i], i, c);
										else if(islem_id == 1)
											Maks_Genislik(bmp[i], i, c);
										else if(islem_id == 2)
											Siyah_say(bmp[i], i, c);
										else if(islem_id == 3)
											Beyaz_say(bmp[i], i, c);
										else if(islem_id == 4)
											Sol_Konum_Kontrol(bmp[i], i, c);
										else if(islem_id == 5)
											Ust_Konum_Kontrol(bmp[i], i, c);
										else if(islem_id == 6)
											Aci_hesapla_yatay(bmp[i], i, c);
										else if(islem_id == 7)
											Aci_hesapla_dikey(bmp[i], i, c);
										else if(islem_id == 8)
											Audi_aci_hesapla(bmp[i], i, c);
										else if(islem_id == 9)
											Cizgi_Ile_Lokasyon_Bul(bmp[i], i, c);
										else if(islem_id == 10)
											Audi_uzunluk_hesapla(bmp[i], i, c);
										else if(islem_id == 11)
										{
											Audi_alttan_uzat(bmp[i], i, c);
											await Task.Delay(int.Parse(bekleme[3]));
											double dx = Ofset[i, 2];
											double dy = (Ofset[i, 1]) - (Ofset[i, 0]);
											double m = dy / dx;
											double b = Ofset[i, 0] - (m * (Rect[i].Y + Rect[i].Height + dx));
											double y1 = (m * (Rect[i].X - 150)) + b;
											double y2 = (m * (Rect[i].X + Rect[i].Width + 150)) + b;
											using(Graphics g = Graphics.FromImage(bitmap[c]))
											{
												Pen p = new Pen(blk, 200);
												g.DrawLine(p, 0, Rect[i].Y + (int)y1 + 100, bitmap[c].Width, Rect[i].Y + (int)y2 + 100);
											}
											ShowScene(new Bitmap(bitmap[c])); // display copy; bitmap[c] stays owned by Islemler (T06)
										}
										else if(islem_id == 12)
											Iki_Siyah_Arasi_Mesafe_Dik(bmp[i], i, c);
										else if(islem_id == 13)
											R_Iki_Siyah_Arasi_Mesafe_Yatay(bmp[i], i, c);
										else if(islem_id == 14)
											L_Iki_Siyah_Arasi_Mesafe_Yatay(bmp[i], i, c);
										else if(islem_id == 15)
											L_Dikey_Aci_Olcme(bmp[i], i, c);
										else if(islem_id == 16)
											R_Dikey_Aci_Olcme(bmp[i], i, c);
										else if(islem_id == 17)
											Iki_method_arasi_fark(bmp[i], i, c);
										else if(islem_id == 18)
											Qr_Okuyucu(bmp[i], i, c);
										else if(islem_id == 19)
											await Btn_bekle(bmp[i], i, c);
										else if(islem_id == 20)
											await Qr_Oku_harici(bmp[i], i, c);

									}
									else hata[c, i] = false;
								}
								else
								{
									if(islem_id == 0)
										Kırmızı_say(bmp[i], i, c);
									else if(islem_id == 1)
										Maks_Genislik(bmp[i], i, c);
									else if(islem_id == 2)
										Siyah_say(bmp[i], i, c);
									else if(islem_id == 3)
										Beyaz_say(bmp[i], i, c);
									else if(islem_id == 4)
										Sol_Konum_Kontrol(bmp[i], i, c);
									else if(islem_id == 5)
										Ust_Konum_Kontrol(bmp[i], i, c);
									else if(islem_id == 6)
										Aci_hesapla_yatay(bmp[i], i, c);
									else if(islem_id == 7)
										Aci_hesapla_dikey(bmp[i], i, c);
									else if(islem_id == 8)
										Audi_aci_hesapla(bmp[i], i, c);
									else if(islem_id == 9)
										Cizgi_Ile_Lokasyon_Bul(bmp[i], i, c);
									else if(islem_id == 10)
										Audi_uzunluk_hesapla(bmp[i], i, c);
									else if(islem_id == 11)
									{
										Audi_alttan_uzat(bmp[i], i, c);
										await Task.Delay(int.Parse(bekleme[3]));
										double dx = Ofset[i, 2];
										double dy = (Ofset[i, 1]) - (Ofset[i, 0]);
										double m = dy / dx;
										double b = Ofset[i, 0] - (m * (Rect[i].Y + Rect[i].Height + dx));
										double y1 = (m * (Rect[i].X - 150)) + b;
										double y2 = (m * (Rect[i].X + Rect[i].Width + 150)) + b;
										using(Graphics g = Graphics.FromImage(bitmap[c]))
										{
											Pen p = new Pen(blk, 200);
											g.DrawLine(p, 0, Rect[i].Y + (int)y1 + 100, bitmap[c].Width, Rect[i].Y + (int)y2 + 100);
										}
										ShowScene(new Bitmap(bitmap[c])); // display copy; bitmap[c] stays owned by Islemler (T06)
									}
									else if(islem_id == 12)
										Iki_Siyah_Arasi_Mesafe_Dik(bmp[i], i, c);
									else if(islem_id == 13)
										R_Iki_Siyah_Arasi_Mesafe_Yatay(bmp[i], i, c);
									else if(islem_id == 14)
										L_Iki_Siyah_Arasi_Mesafe_Yatay(bmp[i], i, c);
									else if(islem_id == 15)
										L_Dikey_Aci_Olcme(bmp[i], i, c);
									else if(islem_id == 16)
										R_Dikey_Aci_Olcme(bmp[i], i, c);
									else if(islem_id == 17)
										Iki_method_arasi_fark(bmp[i], i, c);
									else if(islem_id == 18)
										Qr_Okuyucu(bmp[i], i, c);
									else if(islem_id == 19)
										await Btn_bekle(bmp[i], i, c);
									else if(islem_id == 20)
										await Qr_Oku_harici(bmp[i], i, c);
								}
							});
							} // if(roiInFrame) - dispatch body intentionally not re-indented (T11 refactors the dispatch)
							pbox[i].Image = bmp[i];
							pbox[i].BringToFront();
							pbox[i].Visible = debugMode;
							string time;
							if((DateTime.Now - startTime).TotalMilliseconds.ToString().Split(',').Length > 1)
								time = (DateTime.Now - startTime).TotalMilliseconds.ToString().Split(',')[0];
							else
								time = (DateTime.Now - startTime).TotalMilliseconds.ToString().Split(',')[0];
							lbl_islemTime.Text = Property.list[islem_id] + "( " + time + " ms)";

							txtReport += "Kamera " + (int.Parse(rectString[i].Split(',')[6]) + 1) + " -" + tarama_count + "," + rectbno + "-" + time + "ms" + "," + hata[c, i] + "," + Ofset[i, 0] + "," + Ofset[i, 1] + "," + Ofset[i, 2] + "/";
							rectbno++;

							startTime = DateTime.Now;
						}
						else { pbox[i].Visible = debugMode; }

					}

					bool reseting = false;
					int hataliKamera = 0;
					for(int r = 0; r < rectString.Length; r++)
					{
						if(hata[c, r])
						{
							hataliKamera = c;
							isTrue = true;
							break;
						}
					}
					if(frameMissing)
					{
						// No frame => never ONAYLANDI, even if this camera has no ROI assigned.
						hataliKamera = c;
						isTrue = true;
					}

					if(isTrue)
					{
						lbl_alarm.Text = "";
						Bitmap nokFrame = bitmap[hataliKamera];
						using(Graphics g = nokFrame != null ? Graphics.FromImage(nokFrame) : null)
						{
							Pen pen = new Pen(red);
							for(int i = 0; i < rectString.Length; i++)
							{
								if(hata[hataliKamera, i])
								{
									g?.DrawImage(Resources.carpi, (Rect[i].X + (Rect[i].Width - 75) / 2), (Rect[i].Y + (Rect[i].Height - 75) / 2), 75, 75);
									lbl_alarm.Text += rectString[i].Split(',')[10] + " - ";
									lbl_alarm.ForeColor = red;
									reseting = !bool.Parse(rectString[i].Split(',')[9]);

								}
							}
							lbl_alarm.Text += "Hatası";
							if(frameMissing)
							{
								lbl_alarm.Text = $"Kamera {c + 1} görüntü yok";
								lbl_alarm.ForeColor = red;
							}
							if(nokFrame != null)
								ShowScene(new Bitmap(nokFrame));
							if(reseting)
							{
								await Task.Delay(int.Parse(bekleme[8]));
								Reset();
							}
							else
							{
								islem = false;
								c = cam.Length;
							}

						}
					}
				}

				await Task.Delay(int.Parse(bekleme[4]));
				lbl_islemTime.Text = "Tarama Bitti." + "( " + (DateTime.Now - startTime).TotalMilliseconds + " ms)";
				startTime = DateTime.Now;

				report += txtReport;
				if(reportold != report)
				{
					long maxFileSizeInBytes = 1000000000;

					if(File.Exists(filePath))
					{
						FileInfo fileInfo = new FileInfo(filePath);
						if(fileInfo.Length > maxFileSizeInBytes)
						{
							filePath = $"{folderPath}/{DateTime.Now.ToString("yyyy_MM_dd")}_2.txt";

							if(!File.Exists(filePath))
							{
								File.Create(filePath).Close();
							}
						}
					}
					else
					{
						File.Create(filePath).Close();
					}

					if(reportold != report)
					{
						reportold = report;
						File.AppendAllText(filePath, txtReport);
						tarama_count++;
						set?.Reporting(reportold);
					}
					txtReport = string.Empty;
				}

				visibility(false);

				if(!isTrue)
				{
					lbl_alarm.Text = "ONAYLANDI";
					lbl_alarm.ForeColor = grn;
					await Task.Delay(int.Parse(bekleme[5]));
					// T05: never drive Kaşe after an I/O drop during this cycle.
					if(CycleAbortRequested()) return;

					// Piston-Lamba-Kaşe => 1, 2, 3 ve 4 çıkış (UART and PLC)
					WriteOutputs(io, new MachineOutputs(lamba: true, piston: true, kase: true, bos: true));
					await Task.Delay(int.Parse(bekleme[6]));
					// Kaşe off. Pre-T04 asymmetry kept on purpose: UART clears the boş bit, PLC keeps plc_bos = true.
					if((simIo != null || Settings.Default.UartEnable) && uartIo.IsConnected)
					{
						WriteOutputs(uartIo, new MachineOutputs(lamba: true, piston: true, kase: false, bos: false));
					}
					if((simIo != null || Settings.Default.PlcEnable) && plcIo.IsConnected)
					{
						WriteOutputs(plcIo, new MachineOutputs(lamba: true, piston: true, kase: false, bos: true));
					}



					await Task.Delay(int.Parse(bekleme[7]));
					Reset();
				}

			}
			catch(Exception exception)
			{
				UiLog(exception.ToString());
				txt_qr.Text = "!" + exception.ToString();
				// T05: outputs to the safe state before anything else (Reset() below also writes all-off).
				ForceSafeState();
				timer.Start();
				Reset();
			}
		}

		// Tarama Fonksiyonları Başlangıç --------------------------------------------------------------------

		//0-
		private void Kırmızı_say(Bitmap image, int r_n, int c_n)
		{
			try
			{
				int width = image.Width;
				int height = image.Height;
				int redPixelCount = 0;

				Bitmap result = new Bitmap(width, height);

				BitmapData sourceData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				BitmapData resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

				int bytesPerPixel = Bitmap.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
				int byteCount = sourceData.Stride * height;

				byte[] sourcePixels = new byte[byteCount];
				byte[] resultPixels = new byte[byteCount];

				Marshal.Copy(sourceData.Scan0, sourcePixels, 0, byteCount);

				for(int y = 0; y < height; y++)
				{
					int sourceOffset = y * sourceData.Stride;
					int resultOffset = y * resultData.Stride;

					for(int x = 0; x < width; x++)
					{
						byte b = sourcePixels[sourceOffset + x * bytesPerPixel];
						byte g = sourcePixels[sourceOffset + x * bytesPerPixel + 1];
						byte r = sourcePixels[sourceOffset + x * bytesPerPixel + 2];

						byte color = (r > ((g + b) / 2) + 25) ? (byte)255 : (byte)0;

						resultPixels[resultOffset + x * bytesPerPixel] = 0;
						resultPixels[resultOffset + x * bytesPerPixel + 1] = 0;
						resultPixels[resultOffset + x * bytesPerPixel + 2] = color;

						if(color == 255)
							redPixelCount++;
					}
				}

				Marshal.Copy(resultPixels, 0, resultData.Scan0, byteCount);

				image.UnlockBits(sourceData);
				result.UnlockBits(resultData);

				Ofset[r_n, 1] = redPixelCount;

				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = result;
			}
			catch(Exception ex)
			{
				hata[c_n, r_n] = true;
				UiLog(ex.ToString() + "\r\n");
			}
		}
		//1-
		private void Maks_Genislik(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null) { hata[c_n, r_n] = true; return; }
				int thrs = Thrs[r_n];
				int maxBlobWidth = 0;
				for(int y = 0; y < bitmap.Height; y++)
				{
					for(int x = 0; x < bitmap.Width; x++)
					{
						Color c = bitmap.GetPixel(x, y);
						if((c.R + c.G + c.B) / 3 > thrs)
						{
							int blobWidth = 0;

							for(int i = x; i < bitmap.Width; i++)
							{
								bitmap.SetPixel(x, y, grn);
								Color c2 = bitmap.GetPixel(i, y);
								if((c2.R + c2.G + c2.B) / 3 < thrs)
								{
									break;
								}
								blobWidth++;
							}

							if(blobWidth > maxBlobWidth)
							{
								maxBlobWidth = blobWidth;
							}
						}
					}
				}
				Ofset[r_n, 1] = maxBlobWidth;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//2-
		private void Siyah_say(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true; Ofset[r_n, 1] = 123456;
					return;
				}
				int width = bitmap.Width;
				int height = bitmap.Height;
				int thrs = Thrs[r_n];
				int siyah = 0;
				for(int i = 0; i < width; i++)
				{
					for(int j = 0; j < height; j++)
					{
						Color c = bitmap.GetPixel(i, j);
						if((c.R + c.G + c.B) / 3 < thrs)
						{
							c = blk;
							siyah++;
						}
						else
						{
							c = org;
						}
						bitmap.SetPixel(i, j, c);
					}
				}
				Ofset[r_n, 1] = siyah;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//3-
		private void Beyaz_say(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true; Ofset[r_n, 1] = 123456;
					return;
				}
				int width = bitmap.Width;
				int height = bitmap.Height;
				int thrs = Thrs[r_n];
				int beyaz = 0;
				for(int i = 0; i < width; i++)
				{
					for(int j = 0; j < height; j++)
					{
						Color c = bitmap.GetPixel(i, j);
						if((c.R + c.G + c.B) / 3 < thrs)
						{
							c = blk;
						}
						else
						{
							c = org;
							beyaz++;
						}
						bitmap.SetPixel(i, j, c);
					}
				}
				Ofset[r_n, 1] = beyaz;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//4-
		private void Sol_Konum_Kontrol(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}
				int width = bitmap.Width;
				int j = bitmap.Height / 2;
				int thrs = Thrs[r_n];
				int sol = 0;
				bool black = false;
				for(int i = 0; i < width; i++)
				{
					Color c = bitmap.GetPixel(i, j);
					if((c.R + c.G + c.B) / 3 < thrs) c = blk;
					else c = wht;
					if(i == 0)
					{
						if(c == blk) black = true;
						else black = false;
					}
					if(black)
					{
						bitmap.SetPixel(i, j, red);
						if(c == blk) sol++;
					}
					else
					{
						bitmap.SetPixel(i, j, org);
						if(c == wht) sol++;
					}
				}

				Ofset[r_n, 1] = sol;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//5-
		private void Ust_Konum_Kontrol(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}
				int i = bitmap.Width / 2;
				int height = bitmap.Height;
				int thrs = Thrs[r_n];
				int ust = 0;
				bool black = false;
				for(int j = 0; j < height; j++)
				{
					Color c = bitmap.GetPixel(i, j);
					if((c.R + c.G + c.B) / 3 < thrs) c = blk;
					else c = wht;
					if(j == 0)
					{
						if(c == blk) black = true;
						else black = false;
					}
					if(black)
					{
						bitmap.SetPixel(i, j, red);
						if(c == blk) ust++;
					}
					else
					{
						bitmap.SetPixel(i, j, org);
						if(c == wht) ust++;
					}
				}

				Ofset[r_n, 1] = ust;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//6-
		private void Aci_hesapla_yatay(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}
				int width = bitmap.Width;
				int height = bitmap.Height;
				int thrs = Thrs[r_n];
				int solust = 0, solalt = 0, sagust = 1000, sagalt = 0;
				int i = 10;
				int i2 = width - 10;
				bool kenar = false;
				bool kenar2 = false;

				for(int j = 10; j < height - 10; j++)
				{
					Color c = bitmap.GetPixel(i, j);
					if((c.R + c.G + c.B) / 3 < thrs)
					{
						if(kenar) kenar = false;
						solust = 1000;
					}
					else
					{
						kenar = true;
						bitmap.SetPixel(i, j, red);
						solust++;
					}
					if(!kenar)
					{
						solust = Math.Min(solust, j);
						solalt = Math.Max(solalt, j);
					}
					Color c2 = bitmap.GetPixel(i2, j);
					if((c2.R + c2.G + c2.B) / 3 < thrs)
					{
						if(kenar2) kenar2 = false;
						sagust = 0;
					}
					else
					{
						kenar2 = true;
						bitmap.SetPixel(i2, j, grn);
						sagust++;
					}
					if(!kenar2)
					{
						solust = Math.Min(sagust, j);
						solalt = Math.Max(sagalt, j);
					}
				}
				double a = height - 20;
				double b = Math.Abs((((solalt - solust) / 2) + solust) - (((sagalt - sagust) / 2) + sagust));
				Ofset[r_n, 1] = (int)(Math.Asin(b / Math.Sqrt((a * a) + (b * b))) * 180 / Math.PI);
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//7-
		private void Aci_hesapla_dikey(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}
				int width = bitmap.Width;
				int height = bitmap.Height;
				int thrs = Thrs[r_n];
				int solust = 0, solalt = 0, sagust = 1000, sagalt = 0;
				int i = 10;
				int i2 = height - 10;
				bool kenar = false;
				bool kenar2 = false;
				for(int j = 10; j < width - 10; j++)
				{
					Color c = bitmap.GetPixel(j, i);
					if((c.R + c.G + c.B) / 3 < thrs)
					{
						if(kenar) kenar = false;
						solust = 1000;
					}
					else
					{
						kenar = true;
						bitmap.SetPixel(j, i, red);
						solust++;
					}
					if(!kenar)
					{
						solust = Math.Min(solust, j);
						solalt = Math.Max(solalt, j);
					}
					Color c2 = bitmap.GetPixel(j, i2);
					if((c2.R + c2.G + c2.B) / 3 < thrs)
					{
						if(kenar2) kenar2 = false;
						sagust = 0;
					}
					else
					{
						kenar2 = true;
						bitmap.SetPixel(j, i2, grn);
						sagust++;
					}
					if(!kenar2)
					{
						solust = Math.Min(sagust, j);
						solalt = Math.Max(sagalt, j);
					}
				}
				double a = height - 20;
				double b = Math.Abs((((solalt - solust) / 2) + solust) - (((sagalt - sagust) / 2) + sagust));
				Ofset[r_n, 1] = (int)(Math.Asin(b / Math.Sqrt((a * a) + (b * b))) * 180 / Math.PI);
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//8-
		private void Audi_aci_hesapla(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}
				int aci = 0;
				int yust = bitmap.Height / 3;
				int yalt = yust * 2;
				bool yUst = false;
				bool yAlt = false;
				int ustNokta = 0;
				int altNokta = 0;
				double kenar1 = 0, kenar2 = 0, hip;
				for(int q = 0; q < 2; q++)
				{
					int j;
					if(q == 0) { j = yust; }
					else { j = yalt; }
					for(int i = 0; i < bitmap.Width; i++)
					{
						Color cc = bitmap.GetPixel(i, j);
						if((cc.R + cc.G + cc.B) / 3 < Thrs[r_n]) cc = blk;
						else cc = wht;
						if(cc == wht)
						{
							if(j == yust && !yUst) { ustNokta = i; cc = grn; }
							if(j == yalt && !yAlt) { altNokta = i; cc = grn; }
						}
						else
						{
							if(j == yust) { yUst = true; cc = red; }
							if(j == yalt) { yAlt = true; cc = red; }
						}

						kenar1 = altNokta - ustNokta;
						kenar2 = yalt - yust;
						hip = Math.Sqrt((kenar1 * kenar1) + (kenar2 * kenar2));
						aci = (int)((Math.Asin((kenar1 / hip)) * 180 / Math.PI) * 100);

						bitmap.SetPixel(i, j, cc);
					}
				}
				Ofset[r_n, 1] = Math.Abs(aci);
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//9-
		private void Cizgi_Ile_Lokasyon_Bul(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{

				PointF a2 = new PointF(10, 15); // taramadan gelen sol
				PointF b2 = new PointF(10, 15); // taramadan gelen sağ

				bool sol = false, sag = false;
				int x1 = bitmap.Width / 3;
				int x2 = (bitmap.Width / 3) * 2;
				a2.X = x1;
				b2.X = x2;

				for(int q = 0; q < 2; q++)
				{
					int i;
					if(q == 0) i = x1;
					else i = x2;
					for(int j = 0; j < bitmap.Height; j++)
					{
						Color cc = bitmap.GetPixel(i, j);
						if((cc.R + cc.G + cc.B) / 3 < Thrs[r_n]) cc = blk;
						else cc = wht;
						if(cc == wht)
						{
							if(i == x1 && !sol) { a2.Y = j; cc = grn; }
							if(i == x2 && !sag) { b2.Y = j; cc = grn; }
						}
						else
						{
							if(i == x1) { sol = true; cc = red; }
							if(i == x2) { sag = true; cc = red; }
						}
						bitmap.SetPixel(i, j, cc);
					}
				}


				PointF a1 = new PointF(x1, 110);
				PointF b1 = new PointF(x2, 110);

				for(int i = 0; i < Rect.Length; i++)
				{
					if(i != r_n)
					{
						PointF c1 = new PointF(Rect[i].X, Rect[i].Y);
						PointF c2 = TransformPoint(a1, b1, c1, a2, b2);

						// T06: frame size of the inspected camera image (was pb_scene.Image, which showed that frame).
						Size frameSize = scanFrameSize;
						if(c2.X < 0) c2.X = 0; if(c2.Y < 0) c2.Y = 0; if(c2.X > frameSize.Width) c2.X = frameSize.Width - Rect[i].Width; if(c2.Y > frameSize.Height) c2.Y = frameSize.Height - Rect[i].Height;

						Rect[i].X = (int)c2.X; Rect[i].Y = (int)c2.Y;
					}
				}





				bmp[r_n] = bitmap;
				// Locator: no Min/Max measurement; OK only when the shift completed without error.
				hata[c_n, r_n] = false;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//10-
		private void Audi_uzunluk_hesapla(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}





				int w = bitmap.Width / 2;
				int height = bitmap.Height;
				int leftX = w;
				int rightX = 0;
				int okLeft = 0;
				int nokLeft = 0;
				int okRight = 0;

				int orneklemeSayisi = 15;

				Color[] n = new Color[30];
				int[] np = new int[n.Length];
				bool nb = false;

				int solKenarOrt = 0;
				int sagKenarOrt = bitmap.Height - 10;

				for(int y = 0; y < orneklemeSayisi; y++)
				{
					for(int a = 1; a < w - n.Length; a++)
					{
						int lx = w - a;
						int rx = w + a;
						int right = (bitmap.GetPixel(rx, y).R + bitmap.GetPixel(rx, y).G + bitmap.GetPixel(rx, y).B) / 3;
						int left = (bitmap.GetPixel(lx, y).R + bitmap.GetPixel(lx, y).G + bitmap.GetPixel(lx, y).B) / 3;

						if(left < 127)
						{
							for(int i = 0; i < n.Length; i++)
							{
								n[i] = bitmap.GetPixel((lx - i) - 1, y);
								np[i] = (n[i].R + n[i].G + n[i].B) / 3;
								if(np[i] >= 127) nb = true;
								else { nb = false; break; }
							}
							if(nb)
							{
								bitmap.SetPixel(lx, y, grn);
								leftX = Math.Min(leftX, lx);
							}
						}

						if(right < 127)
						{
							for(int i = 0; i < n.Length; i++)
							{
								n[i] = bitmap.GetPixel((rx + i) + 1, y);
								np[i] = (n[i].R + n[i].G + n[i].B) / 3;
								if(np[i] >= 127) nb = true;
								else { nb = false; break; }
							}
							if(nb)
							{
								bitmap.SetPixel(rx, y, grn);
								rightX = Math.Max(rightX, rx);
							}
						}

					}
				}
				//yukardan aşşa önce beyaz sonra siyahı olandan çentik bul

				for(int y = 0; y < bitmap.Height - 1; y++)
				{
					bitmap.SetPixel(leftX, y, red);
					bitmap.SetPixel(rightX, y, red);
				}
				bool centiks = false;
				int centiksay = 0;
				for(int y = 10; y < bitmap.Height; y++)
				{
					centiksay = 0;
					for(int x = 1; x < 15; x++)
					{
						int thr = (bitmap.GetPixel(leftX + x, y).R + bitmap.GetPixel(leftX + x, y).G + bitmap.GetPixel(leftX + x, y).B) / 3;
						bitmap.SetPixel(leftX + x, y, org);
						if(thr > 127) centiks = true;
						else { centiks = false; break; }
						centiksay++;
					}
					if(centiksay > 13 && centiks)
					{
						solKenarOrt = y;

					}

					for(int x = 1; x < 15; x++)
					{
						int thr = (bitmap.GetPixel(rightX + x, y).R + bitmap.GetPixel(rightX + x, y).G + bitmap.GetPixel(rightX + x, y).B) / 3;
						bitmap.SetPixel(rightX + x, y, org);
						if(thr < 127) centiks = true;
						else { centiks = false; break; }
						centiksay++;
					}
					if(centiksay > 13 && centiks)
					{
						sagKenarOrt = Math.Min(sagKenarOrt, y);
					}
				}

				for(int x = 0; x < bitmap.Width - 1; x++)
				{
					bitmap.SetPixel(x, solKenarOrt, ylw);
					bitmap.SetPixel(x, sagKenarOrt, ylw);
				}
				int hassasiyet = 78;








				int turuncuUst = 116000;

				if(rectString[r_n].Split(',')[10].Split('*').Length > 1) turuncuUst = int.Parse(rectString[r_n].Split(',')[10].Split('*')[1]) * 1000;

				Ofset[r_n, 1] = turuncuUst - ((solKenarOrt - sagKenarOrt) * hassasiyet);
				double ofsettt = (double)Ofset[r_n, 1] / 1000;
				// T06: read-modify-write of RotUzunluk on the UI thread (posts run in call order).
				RunOnUi(() =>
				{
					rotuzunlukvisible = true;
					if(RotUzunluk.Text == "")
						RotUzunluk.Text = "SağKol : " + ofsettt.ToString("0.0");
					else
						RotUzunluk.Text = RotUzunluk.Text + " - SolKol : " + ofsettt.ToString("0.0");
				});

				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//11-
		private void Audi_alttan_uzat(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}
				int solalt = 0, sagalt = 0;
				int x1 = bitmap.Width / 3;
				int x2 = x1 * 2;
				bool solx1 = false, solx2 = false;
				for(int j = bitmap.Height - 1; j > 0; j--)
				{
					Color c1 = bitmap.GetPixel(x1, j);
					Color c2 = bitmap.GetPixel(x2, j);
					if((c1.R + c1.G + c1.B) / 3 < Thrs[r_n]) c1 = org;
					else c1 = grn;
					if((c2.R + c2.G + c2.B) / 3 < Thrs[r_n]) c2 = org;
					else c2 = grn;
					if(c1 == org && !solx1) { solalt = j; solx1 = true; c1 = red; }

					if(c2 == org && !solx2) { sagalt = j; solx2 = true; c2 = red; }

					bitmap.SetPixel(x1, j, c1);
					bitmap.SetPixel(x2, j, c2);
				}
				Ofset[r_n, 0] = solalt;
				Ofset[r_n, 1] = sagalt;
				Ofset[r_n, 2] = x1;
				bmp[r_n] = bitmap;
				// Helper: Ofset holds line geometry, not a Min/Max result; OK only on successful completion.
				hata[c_n, r_n] = false;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//12-
		private void Iki_Siyah_Arasi_Mesafe_Dik(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}

				int mesafe = 10000;
				int mesafe2 = 0;
				for(int x = 0; x < bitmap.Width; x++)
				{
					mesafe2 = 0;
					for(int y = bitmap.Height - 1; y > 0; y--)
					{
						Color c = bitmap.GetPixel(x, y);

						if((c.R + c.G + c.B) / 3 < Thrs[r_n]) c = grn;
						else c = org;

						if(c == org) { mesafe2++; c = red; }

						bitmap.SetPixel(x, y, c);
					}
					if(mesafe2 < mesafe) mesafe = mesafe2;
				}
				double pxFark = 75;
				double mmFark = 17;
				int okumaOfset = 32;
				double carpan = pxFark / mmFark;
				double realmm = 198;
				double realpxl = realmm * carpan;
				mesafe = mesafe - okumaOfset;
				mesafe = (int)(mesafe / carpan);
				mesafe = (int)realmm - mesafe;
				Ofset[r_n, 1] = mesafe;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//13-
		private void L_Iki_Siyah_Arasi_Mesafe_Yatay(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}

				int mesafe = 0;
				bool kenar = false;
				int sagAlt = 0;
				for(int y = 0; y < bitmap.Height; y++)
				{
					Color a = bitmap.GetPixel(bitmap.Width - 1, y);

					if((a.R + a.G + a.B) / 3 < Thrs[r_n] && !kenar) { sagAlt = y; kenar = true; a = red; }
					else a = grn;
					bitmap.SetPixel(bitmap.Width - 1, y, a);
				}
				for(int x = bitmap.Width - 1; x > 0; x--)
				{
					Color c = bitmap.GetPixel(x, sagAlt + 5);

					if((c.R + c.G + c.B) / 3 < Thrs[r_n]) c = red;
					else { mesafe++; c = grn; }

					bitmap.SetPixel(x, sagAlt + 5, c);
				}

				//double pxFark = 43;
				//double mmFark = 17;
				//int okumaOfset = 19;
				//double carpan = pxFark / mmFark;
				//double realmm = 198;
				//double realpxl = realmm * carpan;
				//mesafe = mesafe - okumaOfset;
				//mesafe = (int)(mesafe / carpan);
				//mesafe = (int)realmm - mesafe;

				Ofset[r_n, 1] = mesafe;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//14-
		private void R_Iki_Siyah_Arasi_Mesafe_Yatay(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				if(bitmap == null)
				{
					hata[c_n, r_n] = true;
					return;
				}

				int mesafe = 0;
				bool kenar = false;
				int solAlt = 0;
				for(int y = 0; y < bitmap.Height; y++)
				{
					Color a = bitmap.GetPixel(0, y);

					if((a.R + a.G + a.B) / 3 < Thrs[r_n] && !kenar) { solAlt = y; kenar = true; a = red; }
					else a = grn;
					bitmap.SetPixel(bitmap.Width - 1, y, a);
				}
				for(int x = 0; x < bitmap.Width; x++)
				{
					Color c = bitmap.GetPixel(x, solAlt + 5);

					if((c.R + c.G + c.B) / 3 < Thrs[r_n]) c = red;
					else { mesafe++; c = grn; }

					bitmap.SetPixel(x, solAlt + 5, c);
				}

				//double pxFark = 43;
				//double mmFark = 17;
				//int okumaOfset = 19;
				//double carpan = pxFark / mmFark;
				//double realmm = 198;
				//double realpxl = realmm * carpan;
				//mesafe = mesafe - okumaOfset;
				//mesafe = (int)(mesafe / carpan);
				//mesafe = (int)realmm - mesafe;

				Ofset[r_n, 1] = mesafe;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//15-
		private void L_Dikey_Aci_Olcme(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				bool a = false;
				bool b = false;
				int ust = 0, alt = 0;
				int y1 = bitmap.Height / 4;
				int y2 = (bitmap.Height / 4) * 3;
				for(int x = bitmap.Width - 1; x > 0; x--)
				{
					Color c1 = bitmap.GetPixel(x, y1);
					Color c2 = bitmap.GetPixel(x, y2);

					if((c1.R + c1.G + c1.B) / 3 < Thrs[r_n] && !a) { ust = x; a = true; c1 = red; }
					else c1 = grn;

					if((c2.R + c2.G + c2.B) / 3 < Thrs[r_n] && !b) { alt = x; b = true; c2 = red; }
					else c2 = grn;

					bitmap.SetPixel(x, y1, c1);
					bitmap.SetPixel(x, y2, c2);
				}

				double kenar1 = alt - ust;
				double kenar2 = y1 * 2;
				double hip = Math.Sqrt((kenar2 * kenar2) + (kenar1 * kenar1));
				double angle = Math.Asin(kenar2 / hip) * 180 / Math.PI;

				Ofset[r_n, 1] = (int)angle;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//16-
		private void R_Dikey_Aci_Olcme(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				bool a = false;
				bool b = false;
				int ust = 0, alt = 0;
				int y1 = bitmap.Height / 4;
				int y2 = (bitmap.Height / 4) * 3;
				for(int x = 0; x < bitmap.Width; x++)
				{
					Color c1 = bitmap.GetPixel(x, y1);
					Color c2 = bitmap.GetPixel(x, y2);

					if((c1.R + c1.G + c1.B) / 3 < Thrs[r_n] && !a) { ust = x; a = true; c1 = red; }
					else c1 = grn;

					if((c2.R + c2.G + c2.B) / 3 < Thrs[r_n] && !b) { alt = x; b = true; c2 = red; }
					else c2 = grn;

					bitmap.SetPixel(x, y1, c1);
					bitmap.SetPixel(x, y2, c2);
				}

				double kenar1 = alt - ust;
				double kenar2 = y1 * 2;
				double hip = Math.Sqrt((kenar2 * kenar2) + (kenar1 * kenar1));
				double angle = Math.Asin(kenar2 / hip) * 180 / Math.PI;

				Ofset[r_n, 1] = (int)angle;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//17-
		private void Iki_method_arasi_fark(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				int g1 = bitmap.Width - 1;
				int g2 = bitmap.Height - 1;
				// Reference ROI indices come from this ROI's width/height; reject anything outside Ofset.
				if(g1 < 0 || g2 < 0 || g1 >= Ofset.GetLength(0) || g2 >= Ofset.GetLength(0))
				{
					hata[c_n, r_n] = true;
					UiLog($"ROI {r_n} Iki_method_arasi_fark: referans ROI indeksi geçersiz (g1={g1}, g2={g2}, ROI sayısı={Ofset.GetLength(0)}) - {DateTime.Now:HH:mm:ss.fff}");
					return;
				}
				int m1 = Ofset[g1, 1];
				int m2 = Ofset[g2, 1];
				int fark = Math.Abs(m1 - m2);

				Ofset[r_n, 1] = (int)fark;
				if(Ofset[r_n, 0] <= Ofset[r_n, 1] && Ofset[r_n, 1] <= Ofset[r_n, 2]) hata[c_n, r_n] = false;
				else hata[c_n, r_n] = true;
				bmp[r_n] = bitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//18-
		private void Qr_Okuyucu(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				string find_text = rectString[r_n].Split(',')[10];
				string[] findParts = find_text.Split('-');
				if(findParts.Length < 2 || findParts[1] == "")
				{
					// Part name must be "<isim>-<aranan metin>"; without it nothing can be verified => NOK.
					hata[c_n, r_n] = true;
					SetQrText("QR: parça adında '-' yok");
					UiLog($"ROI {r_n} Qr_Okuyucu: parça adı '{find_text}' içinde '-' sonrası aranan metin yok - {DateTime.Now:HH:mm:ss.fff}");
					return;
				}

				Bitmap bwBitmap = new Bitmap(bitmap.Width, bitmap.Height);

				for(int x = 0; x < bitmap.Width; x++)
				{
					for(int y = 0; y < bitmap.Height; y++)
					{
						Color sourceColor = bitmap.GetPixel(x, y);
						int threshold = Thrs[r_n];
						Color newColor = (sourceColor.R + sourceColor.G + sourceColor.B) / 3 >= threshold ? Color.White : Color.Black;
						bwBitmap.SetPixel(x, y, newColor);
					}
				}

				IBarcodeReader reader = new BarcodeReader();
				var result = reader.Decode(bwBitmap);
				if(result != null)
				{
					hata[c_n, r_n] = result.Text.Contains(findParts[1]) ? false : true;
					SetQrText("QR: " + result.Text + " " + findParts[1]);
				}
				else
				{
					SetQrText("QR: okunamadı " + findParts[1]);
					hata[c_n, r_n] = true;
				}

				bmp[r_n] = bwBitmap;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//19-
		// T06: async (await Task.Delay instead of Thread.Sleep on the thread pool). The unused per-tick copy
		// of pb_scene.Image was removed; capture = true still keeps the live view refreshing while waiting.
		private async Task Btn_bekle(Bitmap bitmap, int r_n, int c_n)
		{
			try
			{
				buton = false;
				SetAlarm("BEKLENİYOR", org);
				if(plcIo.IsConnected)
				{
					// PLC only (UART untouched), as before T04: Lamba, Piston, Lazer(boş) on; Kaşe off.
					WriteOutputs(plcIo, new MachineOutputs(lamba: true, piston: true, kase: false, bos: true));
				}
				bool basildi = false;
				for(int i = 0; i < Thrs[r_n] * 10; i++)
				{
					capture = true;

					if(buton) i = Thrs[r_n] * 10;
					await Task.Delay(100).ConfigureAwait(false);

					if(plcIo.IsConnected)
					{
						// A failed read yields Start = false, as before T04.
						MachineInputs plcInputs;
						string plcError;
						plcIo.TryReadInputs(out plcInputs, out plcError);
						basildi = plcInputs.Start;
					}
					if(basildi)
					{
						if(plcIo.IsConnected)
						{
							// PLC only: Lamba, Piston on; Lazer(boş), Kaşe off.
							WriteOutputs(plcIo, new MachineOutputs(lamba: true, piston: true, kase: false, bos: false));
						}
						buton = true;
					}
				}
				// Wait step, not a measurement: OK on normal completion (pressed or timed out), as before T03.
				hata[c_n, r_n] = false;
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
		}
		//20-
		// T06: the keyboard-wedge input is delivered by password_KeyDown (UI thread) through an
		// ExternalQrWait (TaskCompletionSource) instead of polling txt_qr.Text with Thread.Sleep and a message pump call from the thread pool.
		// Timing kept: Bekleme[9] * 10 ticks of 100 ms; >20 ticks with non-matching input => NOK.
		private async Task Qr_Oku_harici(Bitmap bitmap, int r_n, int c_n)
		{
			ExternalQrWait wait = null;
			try
			{
				string find_text = rectString[r_n].Split(',')[10];
				string[] findParts = find_text.Split('-');
				if(findParts.Length < 2 || findParts[1] == "")
				{
					// Part name must be "<isim>-<aranan metin>"; checked before any output is driven => NOK.
					hata[c_n, r_n] = true;
					UiLog($"ROI {r_n} Qr_Oku_harici: parça adı '{find_text}' içinde '-' sonrası aranan metin yok - {DateTime.Now:HH:mm:ss.fff}");
					return;
				}
				int maxTicks = int.Parse(Settings.Default.Bekleme.Split('-')[9]) * 10;

				// Clear the input and arm the wait in one UI-thread step, so no keystroke falls in between.
				wait = new ExternalQrWait(findParts[1]);
				ExternalQrWait armed = wait;
				if(!RunOnUi(() => { txt_qr.Text = ""; externalQrWait = armed; }))
				{
					hata[c_n, r_n] = true; // UI gone: nothing can be read => NOK
					return;
				}
				SetAlarm("BEKLENİYOR...", org);
				if(plcIo.IsConnected)
				{
					// PLC only (UART untouched), as before T04: Lamba, Piston, Lazer(boş) on; Kaşe off.
					WriteOutputs(plcIo, new MachineOutputs(lamba: true, piston: true, kase: false, bos: true));
				}

				int qrCounter = 0;
				int qrErCount = 0;

				// Fail-closed: OK only when the expected text arrives; timeout or >20 wrong reads stay NOK.
				while(qrCounter < maxTicks)
				{
					capture = true;
					if(wait.Result.Task.IsCompleted) break;
					qrCounter++;
					if(wait.InputSeen) { qrErCount++; }
					if(qrErCount > 20) break;
					await Task.WhenAny(wait.Result.Task, Task.Delay(100)).ConfigureAwait(false);
				}

				// The key handler completes the TCS only with text that contains the expected value.
				string gelen = wait.Result.Task.IsCompleted ? wait.Result.Task.Result : null;
				hata[c_n, r_n] = !(gelen != null && gelen.Contains(findParts[1]));
			}
			catch(Exception exception)
			{
				hata[c_n, r_n] = true;
				UiLog(exception.ToString());
			}
			finally
			{
				if(wait != null)
				{
					ExternalQrWait done = wait;
					RunOnUi(() =>
					{
						if(externalQrWait == done) externalQrWait = null;
						txt_qr.Text = "";
					});
				}
			}
		}

		/// <summary>Pending external (keyboard wedge) QR read. Fields other than Result are set on the UI thread.</summary>
		private sealed class ExternalQrWait
		{
			public ExternalQrWait(string expected) { Expected = expected; }
			public readonly string Expected;
			public readonly TaskCompletionSource<string> Result =
				new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
			// Set when non-matching input arrived (pre-T06: txt_qr.Text != "").
			public volatile bool InputSeen;
		}

		// Armed/disarmed on the UI thread only; read by password_KeyDown (UI thread).
		private ExternalQrWait externalQrWait;

		/// <summary>UI thread: feeds keyboard-wedge text to a pending Qr_Oku_harici wait.</summary>
		private void OnExternalQrInput(string text)
		{
			ExternalQrWait wait = externalQrWait;
			if(wait == null || string.IsNullOrEmpty(text)) return;
			if(text.Contains(wait.Expected)) wait.Result.TrySetResult(text);
			else wait.InputSeen = true;
		}

		// Tarama Fonksiyonları Bitiş ------------------------------------------------------------------------------

		// Machine I/O wiring (T04) ------------------------------------------------------------------------------

		private void InitMachineIo()
		{
			// Bit positions are read from Settings at every call, as the pre-T04 code did.
			s7Io = new S7MachineIo(() => new S7BitMap
			{
				Lamba = Settings.Default.plc_lamba,
				Piston = Settings.Default.plc_piston,
				Kase = Settings.Default.plc_kase,
				Bos = Settings.Default.plc_bos,
				Start = Settings.Default.plc_buton
			});
			uartPort = new UartMachineIo(Uart_to_IO, () => new UartBitMap
			{
				Lamba = Settings.Default.Lamba,
				Piston = Settings.Default.Piston,
				Kase = Settings.Default.Kase,
				Bos = Settings.Default.bosBit,
				Start = Settings.Default.buton,
				Reset = Settings.Default.reset_buton
			});

			if(Settings.Default.SimulateIo)
			{
				simIo = new SimulatedMachineIo();
				io = simIo;
				plcIo = simIo.Channel("PLC");
				uartIo = simIo.Channel("UART");
				port_text.Text = "SİMÜLASYON";
			}
			else
			{
				io = new CompositeMachineIo()
					.Add("UART", uartPort, () => Settings.Default.UartEnable)
					.Add("PLC", s7Io, () => Settings.Default.PlcEnable);
				plcIo = s7Io;
				uartIo = uartPort;
			}
		}

		// Safe state / cycle gating (T05) ------------------------------------------------------------------------

		// Set when a running cycle was aborted because an I/O backend dropped. Checked by Islemler at its await points.
		private bool cycleAborted = false;
		// Last start-refusal reason written to debug.txt (avoids logging the same reason every timer tick).
		private string lastStartBlockLogged = null;

		/// <summary>All outputs off on every backend (enabled or not). Never throws; callable from any thread.</summary>
		public void ForceSafeState()
		{
			try
			{
				IMachineIo target = io;
				if(target != null) target.ForceSafeState();
			}
			catch(Exception) { }
		}

		/// <summary>False with a reason when an enabled I/O backend (or the simulator) is disconnected.</summary>
		private bool IoBackendsConnected(out string reason)
		{
			reason = null;
			if(simIo != null)
			{
				if(!simIo.IsConnected) reason = "SİMÜLASYON I/O bağlı değil";
			}
			else
			{
				if(Settings.Default.UartEnable && (uartPort == null || !uartPort.IsConnected)) reason = "UART bağlantısı yok";
				else if(Settings.Default.PlcEnable && (s7Io == null || !s7Io.IsConnected)) reason = "PLC bağlantısı yok";
			}
			return reason == null;
		}

		/// <summary>Cycle start precondition: every configured camera running and every enabled I/O backend connected.</summary>
		private bool CanStartCycle(out string reason)
		{
			if(cam == null || cam.Length == 0)
			{
				reason = "Kamera tanımlı değil";
				return false;
			}
			for(int i = 0; i < cam.Length; i++)
			{
				if(cam[i] == null || !cam[i].IsRunning)
				{
					reason = "Kamera " + (i + 1).ToString() + " çalışmıyor";
					return false;
				}
			}
			return IoBackendsConnected(out reason);
		}

		/// <summary>
		/// Aborts the running cycle as NOK: outputs safe state, alarm shown, timer stopped.
		/// The operator must Reset (password / reset input) as after a NOK without auto reset.
		/// </summary>
		private void AbortCycle(string reason)
		{
			cycleAborted = true;
			ForceSafeState();
			capturing = true;
			islem = false;
			timer.Stop();
			lbl_alarm.Text = "NOK - " + reason + " (çevrim iptal)";
			lbl_alarm.ForeColor = red;
			p_scene.BackColor = red;
			tx_debug.Text = "Çevrim iptal edildi: " + reason + " - " + DateTime.Now.ToString("HH:mm:ss.fff");
		}

		/// <summary>Used by Islemler after each await: true when the cycle has to stop (already aborted or I/O lost now).</summary>
		private bool CycleAbortRequested()
		{
			if(cycleAborted) return true;
			string reason;
			if(!IoBackendsConnected(out reason))
			{
				AbortCycle(reason);
				return true;
			}
			return false;
		}

		/// <summary>Writes outputs; on failure the error goes to port_text (as before T04). Callable from any thread.</summary>
		private void WriteOutputs(IMachineIo target, MachineOutputs outputs)
		{
			string error;
			if(!target.TryWriteOutputs(outputs, out error))
			{
				RunOnUi(() => port_text.Text = error);
			}
		}

		// UI marshaling (T06) ----------------------------------------------------------------------------------

		/// <summary>
		/// Runs <paramref name="action"/> on the UI thread: directly when already there, otherwise posted
		/// with BeginInvoke (never blocks the caller, so camera threads cannot deadlock with FormClosing).
		/// Returns false when the form has no handle (not yet created / disposed) and nothing was run.
		/// Exceptions inside the action are logged to debug.txt, not raised to Application.ThreadException.
		/// </summary>
		private bool RunOnUi(Action action)
		{
			if(Thread.CurrentThread.ManagedThreadId == uiThreadId)
			{
				RunUiActionSafe(action);
				return true;
			}
			if(IsDisposed || Disposing || !IsHandleCreated) return false;
			try
			{
				BeginInvoke((Action)(() => RunUiActionSafe(action)));
				return true;
			}
			catch(InvalidOperationException) { return false; } // handle destroyed meanwhile
			catch(ObjectDisposedException) { return false; }
		}

		private static void RunUiActionSafe(Action action)
		{
			try
			{
				action();
			}
			catch(Exception ex)
			{
				AppendDebugFile("UI update error: " + ex);
			}
		}

		/// <summary>Writes tx_debug (and therefore debug.txt via Tx_debug_TextChanged) from any thread.</summary>
		private void UiLog(string text)
		{
			if(!RunOnUi(() => tx_debug.Text = text))
			{
				AppendDebugFile(text); // no UI yet/anymore: keep the message
			}
		}

		/// <summary>Sets lbl_alarm text and color from any thread.</summary>
		private void SetAlarm(string text, Color color)
		{
			RunOnUi(() =>
			{
				lbl_alarm.Text = text;
				lbl_alarm.ForeColor = color;
			});
		}

		/// <summary>Sets txt_qr from any thread.</summary>
		private void SetQrText(string text)
		{
			RunOnUi(() => txt_qr.Text = text);
		}

		/// <summary>
		/// Shows <paramref name="image"/> in pb_scene. Takes ownership of <paramref name="image"/>; the previous
		/// display image is disposed on the UI thread only, after it has been replaced. Callable from any thread.
		/// </summary>
		private void ShowScene(Bitmap image)
		{
			if(image == null) return;
			bool posted = RunOnUi(() =>
			{
				if(pb_scene.IsDisposed)
				{
					image.Dispose();
					return;
				}
				System.Drawing.Image old = pb_scene.Image;
				pb_scene.Image = image;
				if(old != null && !ReferenceEquals(old, image)) old.Dispose();
			});
			if(!posted) image.Dispose();
		}

		private static readonly object debugFileLock = new object();

		/// <summary>Appends a line to debug.txt (same format as Tx_debug_TextChanged). Never throws.</summary>
		private static void AppendDebugFile(string text)
		{
			try
			{
				lock(debugFileLock)
				{
					using(StreamWriter writer = File.AppendText("debug.txt"))
					{
						writer.WriteLine($"{DateTime.Now} - Hata Kodu: {text}");
					}
				}
			}
			catch(Exception) { }
		}
		static PointF TransformPoint(PointF a1, PointF b1, PointF c1, PointF a2, PointF b2)
		{
			float angle = (float)Math.Atan2(b2.Y - a2.Y, b2.X - a2.X) - (float)Math.Atan2(b1.Y - a1.Y, b1.X - a1.X);
			float scale = (float)Math.Sqrt(Math.Pow(b2.X - a2.X, 2) + Math.Pow(b2.Y - a2.Y, 2)) / (float)Math.Sqrt(Math.Pow(b1.X - a1.X, 2) + Math.Pow(b1.Y - a1.Y, 2));

			float cosAngle = (float)Math.Cos(angle);
			float sinAngle = (float)Math.Sin(angle);

			float c2X = (c1.X - a1.X) * scale * cosAngle - (c1.Y - a1.Y) * scale * sinAngle + a2.X;
			float c2Y = (c1.X - a1.X) * scale * sinAngle + (c1.Y - a1.Y) * scale * cosAngle + a2.Y;

			return new PointF(c2X, c2Y);
		}
		/// <summary>Returns a thresholded copy of <paramref name="orjbitmaps"/> (owned by the caller). Display only.</summary>
		public Bitmap Threshold_Apply(Bitmap orjbitmaps, int threshold, int renk)
		{

			// Orijinal bitmap'i yeni bir bitmap'e kopyala
			Bitmap bitmap = new Bitmap(orjbitmaps);



			// Bitmap'in boyutlarını al
			int width = bitmap.Width;
			int height = bitmap.Height;

			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

			int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
			int byteCount = bitmapData.Stride * bitmap.Height;
			byte[] pixels = new byte[byteCount];
			IntPtr ptrFirstPixel = bitmapData.Scan0;
			Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

			byte thresholdValue = (byte)threshold;

			for(int i = 0; i < pixels.Length; i += bytesPerPixel)
			{
				if(renk == 0)
				{
					byte gray = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
					byte color = (gray < thresholdValue) ? (byte)0 : (byte)255;

					pixels[i] = color;
					pixels[i + 1] = color;
					pixels[i + 2] = color;
				}
				else if(renk == 1)
				{
					if(pixels[i + 1] > ((pixels[i] + pixels[i + 2]) / 2) + 25)
					{
						pixels[i] = 0;
						pixels[i + 1] = 255;
						pixels[i + 2] = 0;

					}
					else
					{
						byte gray = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
						byte color = (gray < thresholdValue) ? (byte)0 : (byte)255;

						pixels[i] = color;
						pixels[i + 1] = color;
						pixels[i + 2] = color;

					}

				}
				if(renk == 2)
				{
					if(pixels[i + 2] > ((pixels[i + 1] + pixels[i]) / 2) + 25)
					{
						pixels[i] = 0;
						pixels[i + 1] = 0;
						pixels[i + 2] = 255;

					}
					else
					{
						byte gray = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
						byte color = (gray < thresholdValue) ? (byte)0 : (byte)255;

						pixels[i] = color;
						pixels[i + 1] = color;
						pixels[i + 2] = color;

					}
				}
			}

			Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
			bitmap.UnlockBits(bitmapData);

			return bitmap;
		}

		// Pencere İle İlgili Olaylar Başlangıç ---------------------------------------------------------------------

		bool Moved; int Mouse_X, Mouse_Y;
		private void Move_MouseDown(object sender, MouseEventArgs e) { Mouse_X = e.X; Mouse_Y = e.Y; Moved = true; }
		private void Move_MouseMove(object sender, MouseEventArgs e) { if(Moved) SetDesktopLocation(MousePosition.X - Mouse_X, MousePosition.Y - Mouse_Y); }
		private void Move_MouseUp(object sender, MouseEventArgs e) { Moved = false; }
		private void Close_Click(object sender, EventArgs e)
		{
			Close();
		}

		//-------------------------------------------------------------------------------
		private void MainPage_FormClosing(object sender, FormClosingEventArgs e)
		{
			// T05: outputs to the safe state first, before cameras are stopped (WaitForStop may block).
			ForceSafeState();
			for(int i = 0; i < cam.Length; i++)
			{
				if(cam[i] != null && cam[i].IsRunning)
				{
					cam[i].SignalToStop();
					cam[i].WaitForStop();
				}
			}
			// T06: cameras are stopped, no more Store calls.
			frameBuffer?.Dispose();
			SaveAllCameraSettingsToXml();
		}
		private void pb_RealTekno_Click(object sender, EventArgs e)
		{
			// T05: the logo is a manual start only in debug builds or with simulated I/O. In production it is ignored.
			if(debugMode || Settings.Default.SimulateIo) buton = true;
		}
		// T05: the hidden Reset_setting label no longer resets settings (click handler removed).
		// Settings reset: Property screen, admin mode, "ayarsifirla" command -> Safety.SettingsReset (confirmation + backup).
		private void lbl_datetime_Click(object sender, EventArgs e) { caming_id = (caming_id == 0) ? 1 : 0; }
		private void Pb_companyLogo_Click(object sender, EventArgs e)
		{
			try
			{
				if(!debugMode)
				{
					// T08: the settings screen needs the operator password (Debug builds skip).
					if(set != null && !set.IsDisposed) return;
					PasswordForm pasw = new PasswordForm { Owner = this };
					pasw.FormClosed += (s2, a2) =>
					{
						if(pasw.isTrue) OpenSettingsForm();
					};
					pasw.Show();
					return;
				}
				OpenSettingsForm();
			}
			catch(Exception exception)
			{
				UiLog(exception.ToString());
				timer.Start();
			}
		}
		private void OpenSettingsForm()
		{
			try
			{
				visibility(true);
				set = new Property(tab_index);
				set.Owner = this;
				set.FormClosed += (s, args) =>
				{
					visibility(false);
					caming_id = 0;
					set?.Dispose();
				};
				set.Show();

			}
			catch(Exception exception)
			{
				UiLog(exception.ToString());
				timer.Start();
			}
		}
		private void Tx_debug_TextChanged(object sender, EventArgs e)
		{
			// debug.txt dosyasına ekleme (T06: shared writer with a lock, same line format).
			AppendDebugFile(tx_debug.Text);

		}
		private void Uart_to_IO_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				if(Settings.Default.UartEnable)
				{
					// Decode moved to UartMachineIo.TryReceive (same logic, under the UART lock).
					MachineInputs uartInputs;
					bool[] boolDizisi;
					string uartError;
					if(!uartPort.TryReceive(out uartInputs, out boolDizisi, out uartError))
					{
						if(uartError != null) UiLog("DataReceived Error: " + uartError);
						return;
					}
					if(uartInputs.Start && !islem)
					{
						buton = true;
					}
					if(uartInputs.Reset)
					{
						rst_buton = true;
					}
					// Runs on the SerialPort thread: the Property display is updated on the UI thread (T06).
					string de = "";
					for(int i = 0; i < 8; i++)
					{
						if(boolDizisi[i]) de += "1";
						else de += "0";
					}
					RunOnUi(() =>
					{
						if(Application.OpenForms.OfType<Property>().Any() && set != null && !set.IsDisposed)
							set.txt_recData.Text = de;
					});
				}
			}
			catch(Exception ex)
			{
				UiLog("DataReceived Error: " + ex.Message);
			}
		}
		private void password_KeyDown(object sender, KeyEventArgs e)
		{
			if(txt_qr.Text.Length > 75) txt_qr.Text = "QR:";
			if(e.KeyCode == Keys.Enter)
			{
				// T06: the completed line goes to a pending Qr_Oku_harici before the field is reset.
				OnExternalQrInput(txt_qr.Text);
				txt_qr.Text = "QR:";
				if(!islem)
				{
					PasswordForm pasw = new PasswordForm
					{
						Owner = this
					};
					pasw.FormClosed += (s, args) =>
					{
						if(pasw.isTrue)
						{
							Reset();
						}
					};
					pasw.Show();
				}

			}
			else
			{
				txt_qr.Text += ((char)e.KeyValue);
				OnExternalQrInput(txt_qr.Text);
			}
		}
		bool qrExtern = false;
		private void qrGelen_Enter(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				qrExtern = true;
				PasswordForm pasw = new PasswordForm
				{
					Owner = this
				};
				pasw.FormClosed += (s, args) =>
				{
					if(pasw.isTrue)
					{
						qrExtern = false;
						Reset();
					}
				};
				pasw.Show();
			}
		}

		//--------------------------------------------------------------------------------
		public void pb_scene_MouseClick(object sender, MouseEventArgs e)
		{
			if(set != null)
			{
				pb_scene.MouseMove -= new MouseEventHandler(pb_scene_MouseMove);
			}
		}
		public void pb_scene_MouseMove(object sender, MouseEventArgs e)
		{
			if(set != null)
			{
				Point loc1 = new Point((int)(e.Location.X * oran[caming_id, 0]), (int)(e.Location.Y * oran[caming_id, 1]));
				set.UpdateLabel(loc1);
				Label_adjust(org);
			}
		}


		// Pencere İle İlgili Olaylar Bitiş --------------------------------------------------------------------------


















		// Test photo (debug). Read by the camera threads; copied under takePhotoLock.
		volatile Bitmap takePhoto;
		readonly object takePhotoLock = new object();
		private void Photo_Click(object sender, EventArgs e)
		{
			if(pb_scene.Image != null)
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "PNG Dosyaları|*.png",
					Title = "Görseli Kaydet",
					FileName = "image.png"
				};

				if(saveFileDialog.ShowDialog() == DialogResult.OK)
				{
					pb_scene.Image.Save(saveFileDialog.FileName, ImageFormat.Png);
					MessageBox.Show("Görsel başarıyla kaydedildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
					takePhoto = new Bitmap(saveFileDialog.FileName);
				}
			}
			else
			{
				MessageBox.Show("PictureBox içinde bir görsel bulunmuyor!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void sec_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "PNG Dosyaları|*.png",
				Title = "Bir PNG Dosyası Seç"
			};

			if(openFileDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// Seçilen görseli Bitmap olarak yükleme
					takePhoto = new Bitmap(openFileDialog.FileName);
				}
				catch(Exception ex)
				{
					MessageBox.Show($"Görsel yüklenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void iptal_Click(object sender, EventArgs e)
		{
			takePhoto = null;
		}





		private void ApplyCameraSettingsToAll()
		{
			DsDevice[] devices = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
			string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CameraSettings.xml");

			if(!File.Exists(filePath))
			{
				//MessageBox.Show("XML dosyası bulunamadı. Ayarlar yüklenemedi.", "Dosya Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var xmlDocument = XDocument.Load(filePath);

			for(int i = 0; i < devices.Length; i++)
			{
				var cameraElement = xmlDocument.Root.Element($"Camera_{i}");
				if(cameraElement == null) continue;

				IBaseFilter videoInputFilter = null;

				try
				{
					IFilterGraph2 graphBuilder = (IFilterGraph2)new FilterGraph();
					graphBuilder.AddSourceFilterForMoniker(devices[i].Mon, null, devices[i].Name, out videoInputFilter);

					var cameraControl = videoInputFilter as IAMCameraControl;
					var videoProcAmp = videoInputFilter as IAMVideoProcAmp;

					// Kamera Kontrol Ayarları
					var cameraControlElement = cameraElement.Element("CameraControlProperties");
					if(cameraControl != null && cameraControlElement != null)
					{
						foreach(var propertyElement in cameraControlElement.Elements())
						{
							if(Enum.TryParse(propertyElement.Name.ToString(), out DirectShowLib.CameraControlProperty property))
							{
								int value = int.Parse(propertyElement.Attribute("Value").Value);
								var flag = (DirectShowLib.CameraControlFlags)Enum.Parse(typeof(DirectShowLib.CameraControlFlags), propertyElement.Attribute("Flags").Value);
								cameraControl.Set(property, value, flag);
							}
						}
					}

					// Video İşlem Ayarları
					var videoProcAmpElement = cameraElement.Element("VideoProcAmpProperties");
					if(videoProcAmp != null && videoProcAmpElement != null)
					{
						foreach(var propertyElement in videoProcAmpElement.Elements())
						{
							if(Enum.TryParse(propertyElement.Name.ToString(), out VideoProcAmpProperty property))
							{
								int value = int.Parse(propertyElement.Attribute("Value").Value);
								var flag = (VideoProcAmpFlags)Enum.Parse(typeof(VideoProcAmpFlags), propertyElement.Attribute("Flags").Value);
								videoProcAmp.Set(property, value, flag);
							}
						}
					}
				}
				catch(Exception ex)
				{
					//MessageBox.Show($"Kamera {i} ayarları yüklenirken hata oluştu: {ex.Message}");
				}
				finally
				{
					if(videoInputFilter != null)
					{
						Marshal.ReleaseComObject(videoInputFilter);
					}
				}
			}

			//MessageBox.Show("Tüm kameraların ayarları XML'den yüklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}



		private void SaveAllCameraSettingsToXml()
		{
			DsDevice[] devices = DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
			var xmlDocument = new XDocument(new XElement("CameraSettings"));

			for(int i = 0; i < devices.Length; i++)
			{
				IBaseFilter videoInputFilter = null;

				try
				{
					IFilterGraph2 graphBuilder = (IFilterGraph2)new FilterGraph();
					graphBuilder.AddSourceFilterForMoniker(devices[i].Mon, null, devices[i].Name, out videoInputFilter);

					var cameraControl = videoInputFilter as IAMCameraControl;
					var videoProcAmp = videoInputFilter as IAMVideoProcAmp;

					var cameraElement = new XElement($"Camera_{i}", new XAttribute("Name", devices[i].Name));

					// Kamera Kontrol Ayarları
					if(cameraControl != null)
					{
						var cameraControlElement = new XElement("CameraControlProperties");
						foreach(DirectShowLib.CameraControlProperty property in Enum.GetValues(typeof(DirectShowLib.CameraControlProperty)))
						{
							int value;
							DirectShowLib.CameraControlFlags flag;
							if(cameraControl.Get(property, out value, out flag) == 0)
							{
								cameraControlElement.Add(new XElement(property.ToString(),
									new XAttribute("Value", value),
									new XAttribute("Flags", flag)));
							}
						}
						cameraElement.Add(cameraControlElement);
					}

					// Video İşlem Ayarları
					if(videoProcAmp != null)
					{
						var videoProcAmpElement = new XElement("VideoProcAmpProperties");
						foreach(VideoProcAmpProperty property in Enum.GetValues(typeof(VideoProcAmpProperty)))
						{
							int value;
							VideoProcAmpFlags flag;
							if(videoProcAmp.Get(property, out value, out flag) == 0)
							{
								videoProcAmpElement.Add(new XElement(property.ToString(),
									new XAttribute("Value", value),
									new XAttribute("Flags", flag)));
							}
						}
						cameraElement.Add(videoProcAmpElement);
					}

					xmlDocument.Root.Add(cameraElement);
				}
				catch(Exception ex)
				{
					//MessageBox.Show($"Kamera {i} ayarları kaydedilirken hata oluştu: {ex.Message}");
				}
				finally
				{
					if(videoInputFilter != null)
					{
						Marshal.ReleaseComObject(videoInputFilter);
					}
				}
			}

			string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CameraSettings.xml");
			xmlDocument.Save(filePath);
			//MessageBox.Show("Tüm kameraların ayarları XML'e kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}













	}

}
