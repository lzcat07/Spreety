using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Spreety {
 [DataContract] public class Preferences {
  [DataMember] public bool[] Enabled = { true, true, true };
  [DataMember] public bool[] Roam = { true, true, true };
  [DataMember] public int Size = 48;
  [DataMember] public bool Bubbles = true;
  [DataMember] public bool Windows = true;
  [DataMember] public bool Fullscreen = true;
  public static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Spreety");
  public static Preferences Read() {
   try {
    using (var s = File.OpenRead(Path.Combine(Folder, "preferences.json"))) {
     var p = (Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(s);
     if (p.Enabled == null || p.Roam == null || p.Enabled.Length != 3 || p.Roam.Length != 3) return new Preferences();
     p.Size = Math.Max(28, Math.Min(96, p.Size));
     return p;
    }
   } catch { return new Preferences(); }
  }
  public bool Save() {
   try {
    Directory.CreateDirectory(Folder);
    string tmp = Path.Combine(Folder, "preferences.tmp"), dst = Path.Combine(Folder, "preferences.json");
    using (var s = File.Create(tmp)) new DataContractJsonSerializer(typeof(Preferences)).WriteObject(s, this);
    if (File.Exists(dst)) File.Replace(tmp, dst, null); else File.Move(tmp, dst);
    return true;
   } catch (Exception e) { Program.Log(e); return false; }
  }
 }

 public static class Program {
  public static string Root = AppDomain.CurrentDomain.BaseDirectory;
  public static void Log(Exception e) {
   try {
    Directory.CreateDirectory(Preferences.Folder);
    File.AppendAllText(Path.Combine(Preferences.Folder, "errors.log"), DateTime.UtcNow.ToString("o") + " " + e + Environment.NewLine);
   } catch {}
  }
  [STAThread] public static void Main(string[] args) {
   if (args.Contains("--self-test")) {
    try { Tests.Run(); }
    catch (Exception e) {
     File.WriteAllText(Path.Combine(Root, "test-results.txt"), e.ToString());
     Environment.ExitCode = 1;
    }
    return;
   }
   if (args.Contains("--preview")) {
    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    try {
     var engine = new Engine();
     var panel = new ControlRoom(engine);
     engine.Panel = panel;
     panel.Show();
     panel.UpdateLayout();
     var b = new RenderTargetBitmap((int)panel.ActualWidth, (int)panel.ActualHeight, 96, 96, PixelFormats.Pbgra32);
     b.Render(panel);
     var enc = new PngBitmapEncoder();
     enc.Frames.Add(BitmapFrame.Create(b));
     using (var f = File.Create(Path.Combine(Root, "preview.png"))) enc.Save(f);
     panel.Close();
     engine.Dispose();
     return;
    } catch (Exception e) { Log(e); return; }
   }
   bool fresh;
   using (var mutex = new System.Threading.Mutex(true, "Local\\Spreety.Desktop.v1", out fresh)) {
    if (!fresh) { MessageBox.Show("Spreety is already running. Open it from the tray icon.", "Spreety"); return; }
    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    app.DispatcherUnhandledException += (s, e) => {
     Log(e.Exception);
     MessageBox.Show("Spreety hit an error. Details are in %LOCALAPPDATA%\\Spreety\\errors.log", "Spreety");
     app.Shutdown(1);
    };
    try {
     var engine = new Engine();
     var panel = new ControlRoom(engine);
     engine.Panel = panel;
     panel.Show();
     engine.Start();
     app.Run();
     engine.Dispose();
    } catch (Exception e) {
     Log(e);
     MessageBox.Show(e.Message, "Spreety could not start");
    }
   }
  }
 }

 public static class Art {
  // 3 characters, 6 frames each:
  // 0: Idle stand, 1: Walk 1, 2: Walk knee bend 1, 3: Walk 2, 4: Walk knee bend 2, 5: Sit
  static BitmapSource[,] cache = new BitmapSource[3, 6];
  public static readonly double[] BaseHeights = { 218.0, 248.0, 218.0 };
  public static readonly int[] GroundBaseline = { 237, 247, 241 };
  public static readonly int[] SeatingBaseline = { 208, 214, 213 };
  public static readonly int[] SittingTop = { 37, 8, 44 };

  public static double GetSittingTorsoHeight(int who, double scale) {
   return (SeatingBaseline[who] - SittingTop[who]) * scale;
  }

  public static BitmapSource Get(int who, int frame) {
   if (cache[who, frame] != null) return cache[who, frame];
   var source = new BitmapImage();
   source.BeginInit();
   source.CacheOption = BitmapCacheOption.OnLoad;
   source.UriSource = new Uri(Path.Combine(Program.Root, "assets", "characters.png"));
   source.EndInit();

   int totalW = source.PixelWidth;
   int totalH = source.PixelHeight;

   int cellX = frame * totalW / 6;
   int cellW = (frame + 1) * totalW / 6 - cellX;
   int cellY = who * totalH / 3;
   int cellH = (who + 1) * totalH / 3 - cellY;

   var cellCrop = new CroppedBitmap(source, new Int32Rect(cellX, cellY, cellW, cellH));
   var rgba = new FormatConvertedBitmap(cellCrop, PixelFormats.Bgra32, null, 0);
   byte[] pixels = new byte[cellW * cellH * 4];
   rgba.CopyPixels(pixels, cellW * 4, 0);

   int t = cellH, b = 0;
   int cx = cellW / 2;
   int maxDist = 0;
   for (int y = 0; y < cellH; y++) {
    for (int x = 0; x < cellW; x++) {
     if (pixels[(y * cellW + x) * 4 + 3] > 20) {
      if (who == 0 && y >= 250) continue; // ignore bottom divider bleed in sprite atlas
      int dist = Math.Abs(x - cx);
      if (dist > maxDist) maxDist = dist;
      if (y < t) t = y;
      if (y > b) b = y;
     }
    }
   }
   if (maxDist == 0 || b <= t) throw new InvalidDataException("Sprite atlas cell [" + who + "," + frame + "] is empty.");
   int cropL = Math.Max(0, cx - maxDist - 1);
   int cropR = Math.Min(cellW - 1, cx + maxDist + 1);
   int cropW = cropR - cropL + 1;
   int cropTop = t;
   int cropBottom = b;
   if (frame < 5) {
    // For standing & walking cycles, anchor the bottom consistently to GroundBaseline so knee bends don't shift the body
    cropBottom = GroundBaseline[who];
   }
   int cropH = cropBottom - cropTop + 1;
   var result = new CroppedBitmap(cellCrop, new Int32Rect(cropL, cropTop, cropW, cropH));
   result.Freeze();
   cache[who, frame] = result;
   return result;
  }
 }

 public static class Native {
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int L, T, R, B; }
  public delegate bool EnumProc(IntPtr hwnd, IntPtr arg);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc p, IntPtr a);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder name, int max);
  public static bool IsShell(IntPtr h) {
   var name = new System.Text.StringBuilder(256);
   GetClassName(h, name, 256);
   return new[] { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd" }.Contains(name.ToString());
  }
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h, int n, int v);
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
  [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint ga);
  [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint uCmd);
  public static bool IsAbove(IntPtr higher, IntPtr lower) {
   IntPtr cur = lower;
   int depth = 0;
   while ((cur = GetWindow(cur, 3 /* GW_HWNDPREV */)) != IntPtr.Zero && depth++ < 128) {
    if (cur == higher) return true;
   }
   return false;
  }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }
  [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] public static extern int Frame(IntPtr h, int a, out Rect r, int size);
  [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] public static extern int Cloak(IntPtr h, int a, out int r, int size);
  public static bool Bounds(IntPtr h, out Rect r) {
   if (Frame(h, 9, out r, 16) == 0) return true;
   return GetWindowRect(h, out r);
  }
 }

 public class Perch { public IntPtr Handle; public double Left, Right, Y; public bool Desktop; public string Key; }

 public class Engine : IDisposable {
  public Preferences Pref = Preferences.Read();
  public ControlRoom Panel;
  public bool Paused;
  public bool FullscreenHidden;
  public bool SaveFailed;
  public static string[] Names = { "Monkey D. Luffy", "Senku Ishigami", "Rei Ayanami" };
  public static string[] Travel = { "Gum-Gum stretch", "Science-powered jetpack", "EVA flight mode" };
  public static Color[] Colors = { Color.FromRgb(255, 183, 100), Color.FromRgb(143, 226, 181), Color.FromRgb(171, 183, 255) };
  public List<Perch> Perches = new List<Perch>();
  public Buddy[] Buddies = new Buddy[3];
  public Random Random = new Random();
  DispatcherTimer timer;
  Forms.NotifyIcon tray;
  DateTime last = DateTime.UtcNow;
  int frames;

  public void Start() {
   for (int i = 0; i < 3; i++) { Buddies[i] = new Buddy(this, i); Buddies[i].Show(); }
   Refresh();
   for (int i = 0; i < 3; i++) Buddies[i].Home();
   string icoPath = Path.Combine(Program.Root, "assets", "app.ico");
   var icon = File.Exists(icoPath) ? new System.Drawing.Icon(icoPath) : System.Drawing.SystemIcons.Application;
   tray = new Forms.NotifyIcon { Icon = icon, Text = "Spreety · 2.5D pixel desktop crew", Visible = true };
   var menu = new Forms.ContextMenuStrip();
   menu.Items.Add("Open Spreety", null, (s, e) => Open());
   menu.Items.Add("Pause / resume", null, (s, e) => { Paused = !Paused; Panel.RefreshStatus(); });
   menu.Items.Add("Bring everyone home", null, (s, e) => Home());
   menu.Items.Add("Quit Spreety", null, (s, e) => Quit());
   tray.ContextMenuStrip = menu;
   tray.DoubleClick += (s, e) => Open();
   timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
   timer.Tick += (s, e) => Tick();
   timer.Start();
  }

  public void Open() { Panel.Show(); Panel.WindowState = WindowState.Normal; Panel.Activate(); }
  public void Save() { SaveFailed = !Pref.Save(); if (Panel != null) Panel.RefreshStatus(); }
  public void Home() { foreach (var b in Buddies) if (b != null) b.Home(); }
  public void Quit() { Dispose(); Application.Current.Shutdown(); }
  public void Dispose() {
   if (timer != null) timer.Stop();
   if (tray != null) { tray.Visible = false; tray.Dispose(); tray = null; }
   foreach (var b in Buddies) if (b != null) b.Close();
  }

  void Tick() {
   var now = DateTime.UtcNow;
   double dt = Math.Max(0, Math.Min(.08, (now - last).TotalSeconds));
   last = now;
   UpdateTaskbarPerches();
   if (frames++ % 30 == 0) { Refresh(); Panel.RefreshStatus(); }
   for (int i = 0; i < 3; i++) Buddies[i].Step(dt);
  }

  void UpdateTaskbarPerches() {
   if (Panel == null) return;
   var tb = Native.FindWindow("Shell_TrayWnd", null);
   if (tb == IntPtr.Zero) return;
   Native.Rect tr;
   if (!Native.GetWindowRect(tb, out tr) || tr.T <= 0) return;
   var source = PresentationSource.FromVisual(Panel);
   if (source == null || source.CompositionTarget == null) return;
   var tbPt = source.CompositionTarget.TransformFromDevice.Transform(new Point(0, tr.T));
   foreach (var p in Perches) {
    if (p.Desktop) {
     p.Y = tbPt.Y;
    }
   }
  }

  public void Refresh() {
   Perches.Clear();
   var transform = PresentationSource.FromVisual(Panel).CompositionTarget.TransformFromDevice;
   foreach (var screen in Forms.Screen.AllScreens) {
    var wa = screen.WorkingArea;
    var a = transform.Transform(new Point(wa.Left, wa.Top));
    var b = transform.Transform(new Point(wa.Right, wa.Bottom));
    double deskY = b.Y;
    if (screen.Primary) {
     var tb = Native.FindWindow("Shell_TrayWnd", null);
     Native.Rect tr;
     if (tb != IntPtr.Zero && Native.GetWindowRect(tb, out tr) && tr.T > screen.Bounds.Top && tr.T < screen.Bounds.Bottom) {
      var tbPt = transform.Transform(new Point(0, tr.T));
      if (tbPt.Y > 0) deskY = tbPt.Y;
     }
    }
    Perches.Add(new Perch { Left = a.X, Right = b.X, Y = deskY, Desktop = true, Key = screen.DeviceName });
   }
   FullscreenHidden = false;
   var foreground = Native.GetForegroundWindow();
   uint foregroundPid;
   Native.GetWindowThreadProcessId(foreground, out foregroundPid);
   Native.Rect fr;
   if (!Native.IsShell(foreground) && foregroundPid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id && Native.Bounds(foreground, out fr)) {
    foreach (var sc in Forms.Screen.AllScreens) {
     var sb = sc.Bounds;
     if (fr.L <= sb.Left && fr.T <= sb.Top && fr.R >= sb.Right && fr.B >= sb.Bottom) FullscreenHidden = true;
    }
   }
   if (!Pref.Windows) return;
   Native.EnumWindows((h, unused) => {
    uint pid;
    Native.GetWindowThreadProcessId(h, out pid);
    int cloaked;
    Native.Rect r;
    if (Native.IsShell(h) || pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id || !Native.IsWindowVisible(h) || Native.IsIconic(h) || (Native.GetWindowLong(h, -20) & 0x80) != 0) return true;
    if (Native.Cloak(h, 14, out cloaked, 4) == 0 && cloaked != 0) return true;
    if (!Native.Bounds(h, out r) || r.R - r.L < 240 || r.B - r.T < 160) return true;
    var sc = Forms.Screen.FromHandle(h);
    if (r.T < sc.WorkingArea.Top + Pref.Size || r.T > sc.WorkingArea.Bottom) return true;
    r.L = Math.Max(r.L, sc.WorkingArea.Left);
    r.R = Math.Min(r.R, sc.WorkingArea.Right);
    int start = -1, segment = 0;
    for (int x = r.L + 12; x <= r.R - 12; x += 24) {
     bool visible = Native.GetAncestor(Native.WindowFromPoint(new Native.POINT(x, r.T + 10)), 2) == h;
     if (visible && start < 0) start = x;
     if ((!visible || x + 24 > r.R - 12) && start >= 0) {
      int end = visible ? x : x - 24;
      if (end - start > Pref.Size * 2) {
       var a = transform.Transform(new Point(start, r.T));
       var b = transform.Transform(new Point(end, r.T));
       Perches.Add(new Perch { Handle = h, Left = a.X, Right = b.X, Y = a.Y, Key = h.ToString() + ":" + segment++ });
      }
      start = -1;
     }
    }
    return true;
   }, IntPtr.Zero);
  }

  public Perch Pick(Perch current = null) {
   var pool = Perches.Where(p => current == null || p.Key != current.Key).ToList();
   if (pool.Count == 0) pool = Perches;
   if (pool.Count == 0) return null;
   var windows = pool.Where(p => !p.Desktop).ToArray();
   var homes = pool.Where(p => p.Desktop).ToArray();
   if (windows.Length > 0 && (homes.Length == 0 || Random.NextDouble() < .60)) {
    return windows[Random.Next(windows.Length)];
   }
   return homes.Length > 0 ? homes[Random.Next(homes.Length)] : pool[Random.Next(pool.Count)];
  }
 }

 public enum Activity { Sit, Walk, Wave, Peek, Travel, WindUp }

 public class Buddy : Window {
  public Engine E;
  public int Id;
  public Activity State;
  public int Frame = 0;
  public double X, Y, Elapsed, Duration, FromX, FromY, ToX, ToY;
  public int Direction = 1; // 1 = facing right (natural sprite angle), -1 = facing left (flipped)
  public Perch Perch;
  public Perch TargetPerch;
  public string Bubble = "";
  double bubbleTime;
  BuddyVisual visual;
  IntPtr trackedWindow = IntPtr.Zero;
  Native.Rect lastWinRect;

  public Buddy(Engine e, int id) {
   E = e; Id = id;
   WindowStyle = WindowStyle.None;
   ResizeMode = ResizeMode.NoResize;
   AllowsTransparency = true;
   Background = Brushes.Transparent;
   ShowInTaskbar = false;
   ShowActivated = false;
   Topmost = true;
   Focusable = false;
   IsHitTestVisible = false;
   Left = SystemParameters.VirtualScreenLeft;
   Top = SystemParameters.VirtualScreenTop;
   Width = SystemParameters.VirtualScreenWidth;
   Height = SystemParameters.VirtualScreenHeight;
   RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
   RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
   Content = visual = new BuddyVisual(this);
   SourceInitialized += (s, a) => {
    var h = new WindowInteropHelper(this).Handle;
    Native.SetWindowLong(h, -20, Native.GetWindowLong(h, -20) | 0x20 | 0x80 | 0x08000000);
   };
  }

  public void Home() {
   Perch = E.Perches.FirstOrDefault(p => p.Desktop);
   if (Perch == null) return;
   X = Perch.Left + (Perch.Right - Perch.Left) * (.32 + Id * .16);
   Y = Perch.Y;
   State = Activity.Sit;
   Frame = 5;
   Elapsed = 0;
   Duration = 6 + E.Random.NextDouble() * 12;
   Bubble = "";
   trackedWindow = IntPtr.Zero;
   Position();
  }

  public void Travel() {
   StartTravel(E.Pick(Perch));
  }

  public void StartTravel(Perch target, double? customToX = null) {
   if (target == null) return;
   TargetPerch = target;
   FromX = X; FromY = Y;
   double targetX = customToX ?? Clamp(target.Left + (target.Right - target.Left) * E.Random.NextDouble(), target.Left + E.Pref.Size, target.Right - E.Pref.Size);
   if (!customToX.HasValue) {
    double minGap = E.Pref.Size * 1.1;
    for (int attempts = 0; attempts < 10; attempts++) {
     bool blocked = false;
     foreach (var other in E.Buddies) {
      if (other != null && other.Id != Id && other.Perch != null && other.Perch.Key == target.Key) {
       if (Math.Abs(targetX - other.X) < minGap) { blocked = true; break; }
      }
     }
     if (!blocked) break;
     targetX = Clamp(target.Left + (target.Right - target.Left) * E.Random.NextDouble(), target.Left + E.Pref.Size, target.Right - E.Pref.Size);
    }
   }
   ToX = targetX;
   ToY = target.Y;
   Direction = ToX >= FromX ? 1 : -1;
   State = Activity.WindUp;
   Frame = 0;
   Elapsed = 0;
   Duration = 0.5;
  }

  public void LaunchTravel() {
   State = Activity.Travel;
   Frame = 1;
   Elapsed = 0;
   Duration = Id == 0 ? 3.0 : (Id == 1 ? 3.4 : 3.6);
  }

  void OnWindowMoved() {
   var otherWindows = E.Perches.Where(p => !p.Desktop && p.Handle != IntPtr.Zero && p.Handle != Perch.Handle).ToArray();
   Perch target = otherWindows.Length > 0
    ? otherWindows[E.Random.Next(otherWindows.Length)]
    : E.Perches.FirstOrDefault(p => p.Desktop);
   if (target != null) {
    StartTravel(target);
   }
  }

  public static double Clamp(double x, double lo, double hi) { return Math.Max(lo, Math.Min(hi, x)); }
  public static double Ease(double t) { t = Clamp(t, 0, 1); return t * t * (3 - 2 * t); }
  void Position() {
   visual.InvalidateVisual();
  }

  public void Step(double dt) {
   bool hide = !E.Pref.Enabled[Id] || (E.Pref.Fullscreen && E.FullscreenHidden);
   Visibility = hide ? Visibility.Hidden : Visibility.Visible;
   if (hide || E.Paused) return;
   if (Perch == null) { Home(); return; }

   var current = E.Perches.FirstOrDefault(p => p.Key == Perch.Key);
   if (State != Activity.Travel && State != Activity.WindUp) {
    if (current == null || (!E.Pref.Windows && !Perch.Desktop)) {
     var homePerch = E.Perches.FirstOrDefault(p => p.Desktop);
     if (homePerch != null) { StartTravel(homePerch); return; }
    }
   }

   if (State != Activity.Travel && State != Activity.WindUp && !Perch.Desktop && Perch.Handle != IntPtr.Zero) {
    if (trackedWindow != Perch.Handle) {
     trackedWindow = Perch.Handle;
     Native.Bounds(trackedWindow, out lastWinRect);
    } else {
     Native.Rect wr;
     if (Native.Bounds(Perch.Handle, out wr)) {
      if (Math.Abs(wr.L - lastWinRect.L) > 8 || Math.Abs(wr.T - lastWinRect.T) > 8) {
       lastWinRect = wr;
       OnWindowMoved();
      }
     }
    }
   }

   if (State == Activity.Travel || State == Activity.WindUp) {
    var dest = E.Perches.FirstOrDefault(p => p.Key == (TargetPerch != null ? TargetPerch.Key : "")) ?? TargetPerch;
    if (dest != null) {
     ToY = dest.Y;
     ToX = Clamp(ToX, dest.Left + E.Pref.Size, dest.Right - E.Pref.Size);
    }
    if (State == Activity.WindUp) {
     X = FromX;
     if (Perch != null && Perch.Desktop) { Y = Perch.Y; FromY = Y; } else Y = FromY;
    }
   } else {
    if (current != null) {
     Perch = current;
     Y = current.Y;
     X = Clamp(X, current.Left + E.Pref.Size / 2, current.Right - E.Pref.Size / 2);
    }
   }

   if (!E.Pref.Roam[Id] && (State == Activity.Travel || State == Activity.Walk || State == Activity.WindUp)) { Home(); }

   Elapsed += dt;
   bubbleTime -= dt;
   if (bubbleTime <= 0) Bubble = "";

   if (State == Activity.WindUp) {
    Frame = 0;
    if (Elapsed >= Duration) {
     LaunchTravel();
    }
   } else if (State == Activity.Travel) {
    double p = Math.Min(1.0, Elapsed / Duration);
    Frame = 1;
    if (Id == 0) {
     double stretchPhase = 0.35;
     if (p <= stretchPhase) {
      double armT = p / stretchPhase;
      X = FromX - Direction * Math.Sin(armT * Math.PI) * 2;
      Y = FromY;
     } else {
      double pullT = (p - stretchPhase) / (1.0 - stretchPhase);
      double t = Ease(pullT);
      X = FromX + (ToX - FromX) * t;
      Y = FromY + (ToY - FromY) * t - Math.Sin(t * Math.PI) * 40;
     }
    } else if (Id == 1) {
     double t = Ease(p);
     X = FromX + (ToX - FromX) * t;
     Y = FromY + (ToY - FromY) * t - Math.Sin(t * Math.PI) * 140;
    } else {
     double t = Ease(p);
     X = FromX + (ToX - FromX) * t;
     Y = FromY + (ToY - FromY) * t - Math.Sin(t * Math.PI) * 120;
    }

    if (Elapsed >= Duration) {
     X = ToX;
     Y = ToY;
     Perch = TargetPerch ?? Perch;
     State = Activity.Sit;
     Frame = 0;
     Elapsed = 0;
     Duration = 1.2 + E.Random.NextDouble() * 1.5;
     trackedWindow = Perch.Handle;
     if (trackedWindow != IntPtr.Zero) Native.Bounds(trackedWindow, out lastWinRect);
    }
   } else if (State == Activity.Walk) {
    X += Direction * dt * 28;
    if (X < Perch.Left + E.Pref.Size) { X = Perch.Left + E.Pref.Size; Direction = 1; }
    else if (X > Perch.Right - E.Pref.Size) { X = Perch.Right - E.Pref.Size; Direction = -1; }
    foreach (var other in E.Buddies) {
     if (other != null && other.Id != Id && other.Perch != null && other.Perch.Key == Perch.Key && other.State == Activity.Sit) {
      double dist = X - other.X;
      if (Math.Abs(dist) < E.Pref.Size * 0.9 && Math.Sign(dist) != Direction) {
       Direction = -Direction;
       break;
      }
     }
    }
    int[] cycle = { 1, 2, 3, 4 };
    int stepIndex = (int)(Elapsed * 6.5) % 4;
    Frame = cycle[stepIndex];
   } else if (State == Activity.Sit) {
    Frame = 5;
   } else if (State == Activity.Wave) {
    Frame = 0;
   }

   if (State != Activity.Travel && State != Activity.WindUp && Elapsed >= Duration) {
    Elapsed = 0;
    Duration = 5 + E.Random.NextDouble() * 15;
    double choice = E.Random.NextDouble();
    if (E.Pref.Roam[Id] && choice > .80) { StartTravel(E.Pick(Perch)); }
    else if (E.Pref.Roam[Id] && choice > .68) { StartTravel(Perch); }
    else if (E.Pref.Roam[Id] && choice > .45) {
     State = Activity.Walk;
     Direction = E.Random.Next(2) == 0 ? -1 : 1;
     Duration = 2.5 + E.Random.NextDouble() * 4.5;
    }
    else if (choice > .32) { State = Activity.Wave; Duration = 2.8; Say(); }
    else {
     bool spotTaken = E.Buddies.Any(other => other != null && other.Id != Id && other.Perch != null && other.Perch.Key == Perch.Key && other.State == Activity.Sit && Math.Abs(X - other.X) < E.Pref.Size * 1.1);
     if (spotTaken) {
      State = Activity.Walk;
      Direction = -Direction;
      Duration = 2.5 + E.Random.NextDouble() * 3.5;
     } else {
      State = Activity.Sit;
      Frame = 5;
     }
    }
   }
   Position();
  }

  public void Say() {
   string[][] lines = {
    new[] { "Hey! Adventure time!", "Meat break?", "I'm gonna be King of the Pirates!" },
    new[] { "Ten billion percent!", "Science rules everything.", "Hey, lab partner." },
    new[] { "Hello.", "It's peaceful here.", "I'll stay right here with you." }
   };
   Bubble = lines[Id][E.Random.Next(3)];
   bubbleTime = 3.8;
  }
 }

 public class BuddyVisual : FrameworkElement {
  Buddy b;
  public BuddyVisual(Buddy buddy) {
   b = buddy;
   RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
   RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
  }

  protected override void OnRender(DrawingContext d) {
   double targetH = b.E.Pref.Size;
   var img = Art.Get(b.Id, b.Frame);
   double scale = targetH / Art.BaseHeights[b.Id];
   double w = img.PixelWidth * scale;
   double h = img.PixelHeight * scale;

   double contactSink = 1.5;
   double vLeft = SystemParameters.VirtualScreenLeft;
   double vTop = SystemParameters.VirtualScreenTop;
   double xCenter = b.X - vLeft;
   double groundY = (b.Y - vTop) + contactSink;
   double x = xCenter - w / 2;
   double y;

   if (b.Frame == 5) {
    // Sitting: anchor the character's buttocks and palms directly to the perch baseline
    double torsoH = Art.GetSittingTorsoHeight(b.Id, scale);
    y = groundY - torsoH;
   } else {
    // Standing / Walking / Wave: anchor the shoe soles directly to the perch baseline
    // During walk knee-bends (frames 2 and 4), add a subtle 0.5px step compression (never lift feet into air)
    double stepDip = b.State == Activity.Walk && (b.Frame == 2 || b.Frame == 4) ? 0.5 : 0;
    y = groundY - h + stepDip;
   }

   // Item 3: Occlusion when a new window hovers over the perch
   bool pushedOcclusion = false;
   if (b.State != Activity.Travel && b.Perch != null && !b.Perch.Desktop && b.Perch.Handle != IntPtr.Zero) {
    var source = PresentationSource.FromVisual(this);
    if (source != null && source.CompositionTarget != null) {
     var toDevice = source.CompositionTarget.TransformToDevice;
     var fromDevice = source.CompositionTarget.TransformFromDevice;
     IntPtr occluder = IntPtr.Zero;
     foreach (double testY in new[] { b.Y - h * 0.5, b.Y }) {
      Point ptDev = toDevice.Transform(new Point(b.X, testY));
      IntPtr wnd = Native.WindowFromPoint(new Native.POINT((int)ptDev.X, (int)ptDev.Y));
      IntPtr root = Native.GetAncestor(wnd, 2);
      if (root != IntPtr.Zero && root != b.Perch.Handle && !Native.IsShell(root) && Native.IsAbove(root, b.Perch.Handle)) {
       occluder = root;
       break;
      }
     }
     if (occluder != IntPtr.Zero) {
      Native.Rect wr;
      if (Native.Bounds(occluder, out wr)) {
       var tl = fromDevice.Transform(new Point(wr.L, wr.T));
       var br = fromDevice.Transform(new Point(wr.R, wr.B));
       var excludeRect = new Rect(tl.X - vLeft, tl.Y - vTop, Math.Max(0, br.X - tl.X), Math.Max(0, br.Y - tl.Y));
       var combined = new CombinedGeometry(
        GeometryCombineMode.Exclude,
        new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)),
        new RectangleGeometry(excludeRect)
       );
       d.PushClip(combined);
       pushedOcclusion = true;
      }
     }
    }
   }

   var accent = new SolidColorBrush(Engine.Colors[b.Id]);

   // Direction flip: character naturally faces right (+1); flip horizontally when moving left (-1)
   double scaleX = b.Direction;
   double tilt = b.State == Activity.Wave ? Math.Sin(b.Elapsed * 8) * 6 : 0;

   var transformGroup = new TransformGroup();
   if (tilt != 0) transformGroup.Children.Add(new RotateTransform(tilt, xCenter, groundY));
   if (scaleX != 1.0) transformGroup.Children.Add(new ScaleTransform(scaleX, 1.0, xCenter, groundY));

   d.PushTransform(transformGroup);

   // Flight thruster / wing effects
   if (b.State == Activity.Travel) {
    if (b.Id == 1) {
     d.DrawRoundedRectangle(Brushes.DimGray, new Pen(accent, 2), new Rect(x - 6, y + h * .45, 14, h * .35), 4, 4);
     double flame = 14 + Math.Sin(b.Elapsed * 35) * 6;
     d.DrawEllipse(Brushes.Orange, null, new Point(x + 1, y + h * .85 + flame / 2), 5, flame);
     d.DrawEllipse(Brushes.LightCyan, null, new Point(x + 1, y + h * .85), 3, 6);
     for (int s = 1; s <= 2; s++) {
      double sparkY = y + h * .85 + flame + s * 9 + Math.Sin(b.Elapsed * 20 + s) * 3;
      double sparkX = x + 1 + Math.Sin(b.Elapsed * 15 + s * 2) * 5;
      d.DrawEllipse(Brushes.Yellow, null, new Point(sparkX, sparkY), 2, 2);
     }
    } else if (b.Id == 2) {
     d.DrawEllipse(null, new Pen(accent, 2), new Point(xCenter, y - 7), w * .36, 4);
     double wingPulse = Math.Sin(b.Elapsed * 8) * 3;
     for (int k = -1; k <= 1; k += 2) {
      d.DrawLine(new Pen(accent, 3), new Point(xCenter + k * w * .3, y + h * .55), new Point(xCenter + k * (w * .83 + wingPulse), y + h * .25));
      d.DrawLine(new Pen(Brushes.White, 1.5), new Point(xCenter + k * w * .3, y + h * .6), new Point(xCenter + k * (w * .75 + wingPulse), y + h * .39));
     }
     d.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 160, 185, 255)), null, new Point(xCenter, groundY), w * .45, 8);
    }
   }

   d.DrawImage(img, new Rect(x, y, w, h));
   d.Pop();

   // Luffy Rubber Arm stretch, grab, and wobble (drawn outside horizontal mirror)
   if (b.State == Activity.Travel && b.Id == 0) {
    double p = Math.Min(1.0, b.Elapsed / b.Duration);
    double stretchPhase = 0.35;
    Point targetPt = new Point(b.ToX - vLeft, b.ToY - vTop);
    // Shoulder anchor at chest/arm level (0.56 of height), NOT face/mouth
    Point shoulder = new Point(xCenter + b.Direction * w * 0.22, y + h * 0.56);
    Point handPt;
    bool grabbed = p >= stretchPhase;

    if (!grabbed) {
     // Phase 1: Hand extends straight to target edge (clean straight line, zero wobble)
     double armT = Math.Min(1.0, p / stretchPhase);
     handPt = new Point(shoulder.X + (targetPt.X - shoulder.X) * armT, shoulder.Y + (targetPt.Y - shoulder.Y) * armT);
    } else {
     // Phase 2: Hand is firmly anchored to target edge while body flies towards it
     handPt = targetPt;
    }

    double dx = handPt.X - shoulder.X;
    double dy = handPt.Y - shoulder.Y;
    double dist = Math.Sqrt(dx * dx + dy * dy);

    if (dist > 3) {
     var skinBrush = new SolidColorBrush(Color.FromRgb(238, 180, 135));
     var shadowPen = new Pen(new SolidColorBrush(Color.FromRgb(185, 115, 80)), 2.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
     var armPen = new Pen(skinBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

     if (!grabbed || dist > 180) {
      // Clean straight 2.5D pixel rubber arm during grab and long stretch
      d.DrawLine(shadowPen, shoulder, handPt);
      d.DrawLine(armPen, shoulder, handPt);
     } else {
      // Subtle rubber wobble as the arm shortens during pull (max 3px amplitude)
      double nx = -dy / dist;
      double ny = dx / dist;
      var stream = new StreamGeometry();
      using (var ctx = stream.Open()) {
       ctx.BeginFigure(shoulder, false, false);
       int segs = 10;
       for (int k = 1; k <= segs; k++) {
        double s = (double)k / segs;
        double bx = shoulder.X + dx * s;
        double by = shoulder.Y + dy * s;
        double amp = Math.Sin(s * Math.PI) * Math.Sin(s * Math.PI * 3 - b.Elapsed * 24) * Math.Min(3.0, dist * 0.04);
        ctx.LineTo(new Point(bx + nx * amp, by + ny * amp), true, false);
       }
      }
      stream.Freeze();
      d.DrawGeometry(null, shadowPen, stream);
      d.DrawGeometry(null, armPen, stream);
     }

     // Proportionate 2.5D pixel hand / fist at handPt (4x4 pixels)
     d.DrawRoundedRectangle(skinBrush, new Pen(new SolidColorBrush(Color.FromRgb(185, 115, 80)), 1.0), new Rect(handPt.X - 2, handPt.Y - 2, 4, 4), 1, 1);
    }
   }

   // Greeting wave sparkle
   if (b.State == Activity.Wave) {
    var pen = new Pen(accent, 2);
    d.DrawLine(pen, new Point(x + w + 4, y + 10), new Point(x + w + 12, y + 3));
    d.DrawLine(pen, new Point(x + w + 8, y + 20), new Point(x + w + 18, y + 17));
   }

   // Speech bubble
   if (b.E.Pref.Bubbles && b.Bubble.Length > 0) {
    var text = new FormattedText(
     b.Bubble,
     System.Globalization.CultureInfo.InvariantCulture,
     FlowDirection.LeftToRight,
     new Typeface("Segoe UI"),
     11,
     Brushes.White,
     VisualTreeHelper.GetDpi(this).PixelsPerDip
    );
    double bw = Math.Min(208, text.Width + 20), by = Math.Max(0, y - 37);
    d.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(35, 40, 48)), new Pen(accent, 1), new Rect(xCenter - bw / 2, by, bw, 27), 10, 10);
    d.DrawText(text, new Point(xCenter - text.Width / 2, by + 5));
   }

   if (pushedOcclusion) d.Pop();
  }
 }

 public class ControlRoom : Window {
  Engine e;
  TextBlock status;
  Button pause;
  StackPanel cards;
  TextBlock count;
  static Brush Ink = new SolidColorBrush(Color.FromRgb(232, 238, 238));
  static Brush Muted = new SolidColorBrush(Color.FromRgb(143, 156, 164));
  static Brush Mint = new SolidColorBrush(Color.FromRgb(166, 239, 205));

  public ControlRoom(Engine engine) {
   e = engine;
   Title = "Spreety · 2.5D Pixel Desktop Crew";
   Width = 1040; Height = 850;
   MinWidth = 1040; MinHeight = 700;
   WindowStartupLocation = WindowStartupLocation.CenterScreen;
   Background = new SolidColorBrush(Color.FromRgb(15, 20, 24));
   FontFamily = new FontFamily("Segoe UI");
   Foreground = Ink;

   string icoPath = Path.Combine(Program.Root, "assets", "app.ico");
   if (File.Exists(icoPath)) {
    try { Icon = BitmapFrame.Create(new Uri(icoPath)); } catch {}
   }

   var root = new Grid();
   root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(196) });
   root.ColumnDefinitions.Add(new ColumnDefinition());
   Content = root;

   var side = new StackPanel { Margin = new Thickness(23, 32, 20, 24) };
   var sb = new Border { Background = new SolidColorBrush(Color.FromRgb(22, 28, 32)), Child = side };
   root.Children.Add(sb);

   string logoPath = Path.Combine(Program.Root, "assets", "logo.png");
   if (File.Exists(logoPath)) {
    try {
     var logoImg = new Image {
      Source = new BitmapImage(new Uri(logoPath)),
      Width = 84, Height = 84,
      HorizontalAlignment = HorizontalAlignment.Left,
      Margin = new Thickness(0, 0, 0, 14)
     };
     RenderOptions.SetBitmapScalingMode(logoImg, BitmapScalingMode.HighQuality);
     side.Children.Add(logoImg);
    } catch {}
   }

   side.Children.Add(Text("spreety", 28, Ink, true));
   side.Children.Add(Text("Sprite + Pretty", 11, Mint, true, 2));
   side.Children.Add(Text("by lzcat", 11, Muted, false, 2));
   side.Children.Add(Text("YOUR SPACE", 10, Muted, false, 36));
   var crew = Text("◉   Desktop crew", 13, Mint, true, 16);
   side.Children.Add(crew);
   side.Children.Add(Text("3 pixel companions living on your screen edges.", 12, Muted, false, 10));
   side.Children.Add(Text("DEVELOPER", 10, Muted, false, 180));
   side.Children.Add(Text("lzcat", 14, Ink, true, 4));
   side.Children.Add(Text("v1.0 · Official release", 10, Mint, false, 2));

   var main = new StackPanel { Margin = new Thickness(34, 28, 34, 20) };
   var scroll = new ScrollViewer { Content = main, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
   Grid.SetColumn(scroll, 1);
   root.Children.Add(scroll);

   var top = new DockPanel();
   top.Children.Add(Text("SPRITE + PRETTY = SPREETY", 10, Mint, true));
   count = Text("●  3 companions · by lzcat", 11, Mint, false);
   count.HorizontalAlignment = HorizontalAlignment.Right;
   top.Children.Add(count);
   main.Children.Add(top);

   main.Children.Add(Text("Meet your desktop crew.", 28, Ink, true, 20));
   main.Children.Add(Text("Tiny animated companions strolling your window tops and resting above your taskbar.", 13, Muted, false, 8));

   var row = new DockPanel { Margin = new Thickness(0, 24, 0, 14) };
   row.Children.Add(Text("The crew", 18, Ink, true));
   row.Children.Add(Text("   Choose who stays. Let them wander.", 12, Muted, false));
   main.Children.Add(row);

   cards = new StackPanel { Orientation = Orientation.Horizontal };
   main.Children.Add(cards);
   for (int i = 0; i < 3; i++) Card(i);

   var controls = new Border {
    Background = new SolidColorBrush(Color.FromRgb(22, 29, 34)),
    CornerRadius = new CornerRadius(12),
    Padding = new Thickness(18),
    Margin = new Thickness(0, 22, 0, 0)
   };
   var settings = new StackPanel();
   controls.Child = settings;
   main.Children.Add(controls);

   var settingsTop = new DockPanel();
   settingsTop.Children.Add(Text("Make yourself at home", 15, Ink, true));
   var sizePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
   sizePanel.Children.Add(Text("Sprite size  ", 11, Muted, false));
   var size = new ComboBox { Width = 110, Height = 25 };
   size.Items.Add("Tiny · 36");
   size.Items.Add("Cozy · 48");
   size.Items.Add("Classic · 64");
   size.SelectedIndex = e.Pref.Size <= 40 ? 0 : e.Pref.Size >= 60 ? 2 : 1;
   size.SelectionChanged += (s, a) => {
    e.Pref.Size = new[] { 36, 48, 64 }[size.SelectedIndex];
    e.Home();
    e.Save();
   };
   sizePanel.Children.Add(size);
   settingsTop.Children.Add(sizePanel);
   settings.Children.Add(settingsTop);

   var opts = new WrapPanel { Margin = new Thickness(0, 17, 0, 0) };
   opts.Children.Add(Check("Little greetings", e.Pref.Bubbles, v => { e.Pref.Bubbles = v; e.Save(); }));
   opts.Children.Add(Check("Explore window tops", e.Pref.Windows, v => { e.Pref.Windows = v; e.Refresh(); e.Save(); }));
   opts.Children.Add(Check("Hide in fullscreen", e.Pref.Fullscreen, v => { e.Pref.Fullscreen = v; e.Save(); }));
   settings.Children.Add(opts);

   var actions = new DockPanel { Margin = new Thickness(0, 20, 0, 0) };
   var buttons = new StackPanel { Orientation = Orientation.Horizontal };
   pause = Btn("Pause crew", () => { e.Paused = !e.Paused; RefreshStatus(); });
   buttons.Children.Add(pause);
   buttons.Children.Add(Btn("Bring everyone home", () => e.Home()));
   actions.Children.Add(buttons);
   var quit = Btn("Quit", () => e.Quit());
   quit.HorizontalAlignment = HorizontalAlignment.Right;
   actions.Children.Add(quit);
   main.Children.Add(actions);

   status = Text("●  Crew is settling in", 11, Muted, false, 15);
   main.Children.Add(status);
   main.Children.Add(Text("Closing this window keeps the crew around. Find Spreety in the system tray.", 10, Muted, false, 7));

   Closing += (s, a) => { a.Cancel = true; Hide(); };
  }

  static TextBlock Text(string value, double size, Brush color, bool bold, double top = 0) {
   return new TextBlock { Text = value, FontSize = size, Foreground = color, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = new Thickness(0, top, 0, 0), TextWrapping = TextWrapping.Wrap };
  }
  static Button Btn(string label, Action action) {
   var b = new Button { Content = label, Padding = new Thickness(15, 9, 15, 9), Margin = new Thickness(0, 0, 9, 0), Background = new SolidColorBrush(Color.FromRgb(39, 49, 56)), Foreground = Ink, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
   b.Click += (s, a) => action();
   return b;
  }
  static CheckBox Check(string label, bool selected, Action<bool> change) {
   var c = new CheckBox { Content = label, IsChecked = selected, Foreground = Ink, FontSize = 11, Margin = new Thickness(0, 0, 20, 0), VerticalContentAlignment = VerticalAlignment.Center };
   c.Checked += (s, a) => change(true);
   c.Unchecked += (s, a) => change(false);
   return c;
  }

  void Card(int id) {
   var col = new SolidColorBrush(Engine.Colors[id]);
   var b = new Border {
    Width = 246,
    Margin = new Thickness(0, 0, id < 2 ? 14 : 0, 0),
    Background = new SolidColorBrush(Color.FromRgb(22, 28, 33)),
    BorderBrush = new SolidColorBrush(Color.FromArgb(70, col.Color.R, col.Color.G, col.Color.B)),
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(14),
    Padding = new Thickness(16)
   };
   cards.Children.Add(b);
   var stack = new StackPanel();
   b.Child = stack;

   var badge = new Border {
    Background = new SolidColorBrush(Color.FromArgb(28, col.Color.R, col.Color.G, col.Color.B)),
    BorderBrush = new SolidColorBrush(Color.FromArgb(85, col.Color.R, col.Color.G, col.Color.B)),
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(7),
    Padding = new Thickness(8, 3, 8, 3),
    HorizontalAlignment = HorizontalAlignment.Left
   };
   badge.Child = Text(new[] { "Straw Hat Captain", "Kingdom of Science", "Unit-00 Pilot" }[id], 10, col, true);
   stack.Children.Add(badge);

   var art = new Grid { Height = 140, Margin = new Thickness(0, 8, 0, 6) };
   var stage = new Border {
    Background = new RadialGradientBrush(Color.FromArgb(40, col.Color.R, col.Color.G, col.Color.B), Colors.Transparent),
    CornerRadius = new CornerRadius(12)
   };
   art.Children.Add(stage);

   var previewImg = new Image {
    Source = Art.Get(id, 0),
    Height = 118,
    Stretch = Stretch.Uniform
   };
   RenderOptions.SetBitmapScalingMode(previewImg, BitmapScalingMode.NearestNeighbor);
   art.Children.Add(previewImg);
   stack.Children.Add(art);

   var nameRow = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
   nameRow.Children.Add(Text(new[] { "Luffy", "Senku", "Rei" }[id], 20, Ink, true));
   var series = Text(new[] { "One Piece", "Dr. Stone", "Evangelion" }[id], 10, Muted, false);
   series.HorizontalAlignment = HorizontalAlignment.Right;
   series.VerticalAlignment = VerticalAlignment.Bottom;
   nameRow.Children.Add(series);
   stack.Children.Add(nameRow);

   string[] bio = {
    "Loves meat and wild adventures. Stretches rubber arms across window edges.",
    "Ten billion percent exhilarating. Blasts between windows with a rocket jetpack.",
    "Quiet guardian of your screen. Drifts gracefully above your open tasks."
   };
   stack.Children.Add(Text(bio[id], 11, Muted, false, 5));

   string[] moves = { "⚓ Gum-Gum Stretch", "🧪 Science Jetpack", "🕊️ EVA Flight Mode" };
   var movePill = new Border {
    Background = new SolidColorBrush(Color.FromRgb(17, 22, 26)),
    BorderBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(6),
    Padding = new Thickness(8, 4, 8, 4),
    Margin = new Thickness(0, 10, 0, 0),
    HorizontalAlignment = HorizontalAlignment.Left
   };
   movePill.Child = Text(moves[id], 10, col, false);
   stack.Children.Add(movePill);

   var switches = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 18, 0, 0) };
   switches.Children.Add(Check("Show", e.Pref.Enabled[id], v => { e.Pref.Enabled[id] = v; e.Save(); }));
   switches.Children.Add(Check("Roam", e.Pref.Roam[id], v => { e.Pref.Roam[id] = v; e.Save(); }));
   stack.Children.Add(switches);
  }

  public void RefreshStatus() {
   if (status == null) return;
   count.Text = "●  " + e.Pref.Enabled.Count(x => x) + " companions · by lzcat";
   pause.Content = e.Paused ? "Resume crew" : "Pause crew";
   status.Text = e.SaveFailed
    ? "Preferences could not be saved. Check the local error log."
    : e.Paused
     ? "Ⅱ  Crew paused · taking a little breather"
     : e.FullscreenHidden && e.Pref.Fullscreen
      ? "○  Fullscreen detected · the crew is tucked away"
      : "●  Crew is active · strolling edges, resting quietly";
  }
 }

 public static class Tests {
  static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); }
  public static void Run() {
   var results = new List<string>();
   Assert(Buddy.Ease(-1) == 0 && Buddy.Ease(2) == 1 && Buddy.Ease(.5) == .5, "Travel easing endpoints");
   results.Add("PASS travel easing bounds and midpoint");
   Assert(Buddy.Clamp(-20, 10, 100) == 10 && Buddy.Clamp(300, 10, 100) == 100, "Clamp");
   results.Add("PASS perch coordinate clamping");

   // Verify all 18 frames for all 3 characters (3 characters * 6 poses)
   for (int i = 0; i < 3; i++) {
    for (int j = 0; j < 6; j++) {
     var a = Art.Get(i, j);
     Assert(a.PixelWidth > 10 && a.PixelHeight > 10, "Empty sprite");
     var rgba = new FormatConvertedBitmap(a, PixelFormats.Bgra32, null, 0);
     var pixels = new byte[a.PixelWidth * a.PixelHeight * 4];
     rgba.CopyPixels(pixels, a.PixelWidth * 4, 0);
     int transparent = 0;
     for (int k = 3; k < pixels.Length; k += 4) if (pixels[k] == 0) transparent++;
     Assert(transparent > pixels.Length / 4 * .05, "Sprite transparency missing");
     results.Add("PASS sprite " + i + "/" + j + " visible bounds " + a.PixelWidth + "x" + a.PixelHeight + "; transparent pixels " + transparent);
    }
   }

   // Verify ground baselines, standing heights, and sitting pelvic alignment
   for (int i = 0; i < 3; i++) {
    for (int j = 0; j < 5; j++) {
     var a = Art.Get(i, j);
     Assert(a.PixelHeight == (int)Art.BaseHeights[i], "Standing/walking frame height mismatch for companion " + i + " frame " + j);
    }
    double sitTorso = Art.GetSittingTorsoHeight(i, 1.0);
    var sitFrame = Art.Get(i, 5);
    Assert(sitTorso > 50 && sitTorso < sitFrame.PixelHeight, "Sitting torso height out of bounds for companion " + i);
   }
   results.Add("PASS consistent ground baselines and sitting pelvic alignment");

   var engine = new Engine();
   engine.Pref = new Preferences();
   engine.Perches.Add(new Perch { Left = 0, Right = 1920, Y = 1040, Desktop = true, Key = "test" });
   var buddy = new Buddy(engine, 0);
   buddy.Home();
   double before = buddy.X;
   buddy.State = Activity.Walk;
   buddy.Duration = 10;
   buddy.Step(.5);
   Assert(buddy.X != before, "Walking did not move");
   engine.Pref.Roam[0] = false;
   buddy.Step(.1);
   Assert(buddy.State == Activity.Sit, "Stay switch did not stop walking");
   engine.Paused = true;
   double elapsed = buddy.Elapsed;
   buddy.Step(.2);
   Assert(buddy.Elapsed == elapsed, "Paused animation advanced");
   engine.Paused = false;
   engine.Pref.Enabled[0] = false;
   buddy.Step(.1);
   Assert(buddy.Visibility == Visibility.Hidden, "Hidden buddy still visible");
   engine.Pref.Enabled[0] = true;
   engine.Pref.Roam[0] = true;
   buddy.Home();
   buddy.Travel();
   Assert(buddy.State == Activity.WindUp, "Windup anticipation state entered");
   buddy.Step(.6);
   Assert(buddy.State == Activity.Travel, "Flight launched after anticipation wait");
   Assert(buddy.Frame == 1, "Stationary leg flight pose maintained");
   buddy.Step(.5);
   Assert(buddy.Frame == 1, "Stationary leg flight pose continuous");
   buddy.Close();
   results.Add("PASS walking, travel static legs, stay switch, pause and visibility transitions");

   // Verify taskbar auto-hide tracking and window escape logic
   var deskPerch = new Perch { Left = 0, Right = 1920, Y = 1080, Desktop = true, Key = "desk" };
   var win1 = new Perch { Handle = (IntPtr)101, Left = 200, Right = 600, Y = 200, Desktop = false, Key = "win1" };
   var win2 = new Perch { Handle = (IntPtr)102, Left = 700, Right = 1100, Y = 200, Desktop = false, Key = "win2" };
   engine.Perches.Clear();
   engine.Perches.Add(deskPerch);
   engine.Perches.Add(win1);
   engine.Perches.Add(win2);
   var testBuddy = new Buddy(engine, 0);
   testBuddy.Home();
   Assert(testBuddy.Y == 1080, "Buddy Y tracks auto-hidden taskbar");
   deskPerch.Y = 1040;
   testBuddy.Step(.1);
   Assert(testBuddy.Y == 1040, "Buddy Y tracks raised taskbar");
   // Window escape with multiple windows
   testBuddy.Perch = win1;
   testBuddy.StartTravel(win2);
   Assert(testBuddy.TargetPerch == win2 && testBuddy.State == Activity.WindUp, "Escape travels to other open window");
   // Window escape with single window
   engine.Perches.Remove(win2);
   testBuddy.StartTravel(engine.Perches.FirstOrDefault(p => p.Desktop));
   Assert(testBuddy.TargetPerch.Desktop && testBuddy.State == Activity.WindUp, "Escape with single window travels to taskbar");
   // Luffy Gum-Gum stretch anchor vs slingshot pull phases
   testBuddy.Home();
   testBuddy.StartTravel(deskPerch, 1200);
   testBuddy.LaunchTravel();
   double startX = testBuddy.FromX;
   testBuddy.Step(.5); // arm stretch phase
   Assert(Math.Abs(testBuddy.X - startX) < 10, "Luffy body anchored at launch point during arm stretch");
   testBuddy.Step(1.5); // slingshot pull phase
   Assert(Math.Abs(testBuddy.X - startX) > 20, "Luffy slingshots toward target during pull phase");
   testBuddy.Step(1.5); // finish flight duration
   Assert(testBuddy.Frame == 0, "Lands standing on feet on arrival");
   testBuddy.Close();
   results.Add("PASS taskbar auto-hide tracking, window relocation escape, and Luffy rubber arm mechanics");

   // Verify sitting mutual exclusion and walking turnaround
   Assert((int)Art.BaseHeights[0] == 218 && Art.GroundBaseline[0] == 237, "Luffy grounded baseline");
   var buddyA = new Buddy(engine, 0) { Perch = deskPerch, X = 500, State = Activity.Sit, Duration = 10 };
   var buddyB = new Buddy(engine, 1) { Perch = deskPerch, X = 530, State = Activity.Walk, Direction = -1, Duration = 10 };
   engine.Buddies[0] = buddyA;
   engine.Buddies[1] = buddyB;
   buddyB.Step(0.01);
   Assert(buddyB.Direction == 1, "Walking buddy turns around before colliding with sitting buddy");
   buddyB.X = 520;
   buddyB.Elapsed = buddyB.Duration = 10;
   buddyB.Step(0.01);
   Assert(buddyB.State == Activity.Walk, "Cannot sit on top of another companion; redirects to walking");
   buddyA.Close();
   buddyB.Close();
   results.Add("PASS sitting mutual exclusion and obstacle turnaround");

   string original = Preferences.Folder;
   Preferences.Folder = Path.Combine(Program.Root, "test-settings");
   try {
    Directory.CreateDirectory(Preferences.Folder);
    var p = new Preferences();
    p.Enabled[1] = false;
    p.Roam[2] = false;
    p.Size = 64;
    Assert(p.Save(), "Save failed");
    var loaded = Preferences.Read();
    Assert(!loaded.Enabled[1] && !loaded.Roam[2] && loaded.Size == 64, "Round trip");
    results.Add("PASS settings round trip");
    File.WriteAllText(Path.Combine(Preferences.Folder, "preferences.json"), "broken");
    Assert(Preferences.Read().Enabled.All(x => x), "Recovery");
    results.Add("PASS corrupted settings recovery");
   } finally {
    Preferences.Folder = original;
   }

   File.WriteAllLines(Path.Combine(Program.Root, "test-results.txt"), results);
  }
 }
}
