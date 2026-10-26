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
  [DataMember] public bool[] Enabled = {true,true,true};
  [DataMember] public bool[] Roam = {true,true,true};
  [DataMember] public int Size = 76;
  [DataMember] public bool Bubbles = true;
  [DataMember] public bool Windows = true;
  [DataMember] public bool Fullscreen = true;
  public static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Spreety");
  public static Preferences Read() { try { using(var s=File.OpenRead(Path.Combine(Folder,"preferences.json"))) { var p=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(s); if(p.Enabled==null||p.Roam==null||p.Enabled.Length!=3||p.Roam.Length!=3) return new Preferences(); p.Size=Math.Max(48,Math.Min(112,p.Size)); return p; } } catch { return new Preferences(); } }
  public bool Save() { try { Directory.CreateDirectory(Folder); string tmp=Path.Combine(Folder,"preferences.tmp"), dst=Path.Combine(Folder,"preferences.json"); using(var s=File.Create(tmp)) new DataContractJsonSerializer(typeof(Preferences)).WriteObject(s,this); if(File.Exists(dst)) File.Replace(tmp,dst,null); else File.Move(tmp,dst); return true; } catch(Exception e) { Program.Log(e); return false; } }
 }
 public static class Program {
  public static string Root=AppDomain.CurrentDomain.BaseDirectory;
  public static void Log(Exception e) { try { Directory.CreateDirectory(Preferences.Folder); File.AppendAllText(Path.Combine(Preferences.Folder,"errors.log"),DateTime.UtcNow.ToString("o")+" "+e+Environment.NewLine); } catch {} }
  [STAThread] public static void Main(string[] args) {
   if(args.Contains("--self-test")) { try { Tests.Run(); } catch(Exception e) { File.WriteAllText(Path.Combine(Root,"test-results.txt"),e.ToString()); Environment.ExitCode=1; } return; }
   bool fresh; using(var mutex=new System.Threading.Mutex(true,"Local\\Spreety.Desktop.v1",out fresh)) {
    if(!fresh) { MessageBox.Show("Spreety is already running. Open it from the tray icon.","Spreety"); return; }
    var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
    app.DispatcherUnhandledException+=(s,e)=>{Log(e.Exception); MessageBox.Show("Spreety hit an error. Details are in %LOCALAPPDATA%\\Spreety\\errors.log","Spreety"); app.Shutdown(1);};
    try { var engine=new Engine(); var panel=new ControlRoom(engine); engine.Panel=panel; panel.Show(); engine.Start(); if(args.Contains("--preview")) { panel.UpdateLayout(); panel.Dispatcher.BeginInvoke(new Action(()=>{ var b=new RenderTargetBitmap((int)panel.ActualWidth,(int)panel.ActualHeight,96,96,PixelFormats.Pbgra32); b.Render(panel); var enc=new PngBitmapEncoder(); enc.Frames.Add(BitmapFrame.Create(b)); using(var f=File.Create(Path.Combine(Root,"preview.png")))enc.Save(f); engine.Dispose(); app.Shutdown(); }),DispatcherPriority.ApplicationIdle); } app.Run(); engine.Dispose(); } catch(Exception e) {Log(e); MessageBox.Show(e.Message,"Spreety could not start");}
   }
  }
 }
 public static class Art {
  static BitmapSource[,] cache=new BitmapSource[3,2];
  public static BitmapSource Get(int who,bool seated) {
   int pose=seated?1:0; if(cache[who,pose]!=null)return cache[who,pose];
   var source=new BitmapImage(); source.BeginInit(); source.CacheOption=BitmapCacheOption.OnLoad; source.UriSource=new Uri(Path.Combine(Program.Root,"assets","characters.png")); source.EndInit();
   int w=source.PixelWidth/3,split=(int)(source.PixelHeight*.55),h=pose==0?split:source.PixelHeight-split;
   // The generated Luffy hat leaves a disconnected sliver in Senku's column.
   int inset=who==1?40:0;
   var crop=new CroppedBitmap(source,new Int32Rect(who*w+inset,pose==0?0:split,w-inset,h));
   w-=inset;
   var rgba=new FormatConvertedBitmap(crop,PixelFormats.Bgra32,null,0); byte[] pixels=new byte[w*h*4]; rgba.CopyPixels(pixels,w*4,0);
   int l=w,t=h,r=0,b=0; for(int y=0;y<h;y++)for(int x=0;x<w;x++) if(pixels[(y*w+x)*4+3]>24) {l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
   if(r<=l||b<=t)throw new InvalidDataException("Sprite atlas contains an empty cell.");
   var result=new CroppedBitmap(crop,new Int32Rect(l,t,r-l+1,b-t+1)); result.Freeze(); cache[who,pose]=result; return result;
  }
 }
 public static class Native {
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int L,T,R,B; }
  public delegate bool EnumProc(IntPtr hwnd,IntPtr arg);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc p,IntPtr a);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h,System.Text.StringBuilder name,int max);
  public static bool IsShell(IntPtr h){var name=new System.Text.StringBuilder(256);GetClassName(h,name,256);return new[]{"Progman","WorkerW","Shell_TrayWnd","Shell_SecondaryTrayWnd"}.Contains(name.ToString());}
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h,int n,int v);
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
  [StructLayout(LayoutKind.Sequential)] public struct POINT {public int X,Y; public POINT(int x,int y){X=x;Y=y;}}
  [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] public static extern int Frame(IntPtr h,int a,out Rect r,int size);
  [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] public static extern int Cloak(IntPtr h,int a,out int r,int size);
  public static bool Bounds(IntPtr h,out Rect r) {if(Frame(h,9,out r,16)==0)return true;return GetWindowRect(h,out r);}
 }
 public class Perch {public IntPtr Handle; public double Left,Right,Y; public bool Desktop; public string Key;}
 public class Engine : IDisposable {
  public Preferences Pref=Preferences.Read(); public ControlRoom Panel; public bool Paused; public bool FullscreenHidden; public bool SaveFailed;
  public static string[] Names={"Monkey D. Luffy","Senku Ishigami","Rei Ayanami"};
  public static string[] Travel={"Gum-Gum stretch","Science-powered jetpack","EVA flight mode"};
  public static Color[] Colors={Color.FromRgb(255,183,100),Color.FromRgb(143,226,181),Color.FromRgb(171,183,255)};
  public List<Perch> Perches=new List<Perch>(); public Buddy[] Buddies=new Buddy[3]; public Random Random=new Random();
  DispatcherTimer timer; Forms.NotifyIcon tray; DateTime last=DateTime.UtcNow; int frames;
  public void Start(){for(int i=0;i<3;i++){Buddies[i]=new Buddy(this,i);Buddies[i].Show();} Refresh(); for(int i=0;i<3;i++)Buddies[i].Home();
   tray=new Forms.NotifyIcon {Icon=System.Drawing.SystemIcons.Application,Text="Spreety · your tiny desktop crew",Visible=true};
   var menu=new Forms.ContextMenuStrip();menu.Items.Add("Open Spreety",null,(s,e)=>Open());menu.Items.Add("Pause / resume",null,(s,e)=>{Paused=!Paused;Panel.RefreshStatus();});menu.Items.Add("Bring everyone home",null,(s,e)=>Home());menu.Items.Add("Quit Spreety",null,(s,e)=>Quit());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>Open();
   timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(33)};timer.Tick+=(s,e)=>Tick();timer.Start();
  }
  public void Open(){Panel.Show();Panel.WindowState=WindowState.Normal;Panel.Activate();}
  public void Save(){SaveFailed=!Pref.Save();if(Panel!=null)Panel.RefreshStatus();}
  public void Home(){foreach(var b in Buddies)if(b!=null)b.Home();}
  public void Quit(){Dispose();Application.Current.Shutdown();}
  public void Dispose(){if(timer!=null)timer.Stop();if(tray!=null){tray.Visible=false;tray.Dispose();tray=null;}foreach(var b in Buddies)if(b!=null)b.Close();}
  void Tick(){var now=DateTime.UtcNow;double dt=Math.Max(0,Math.Min(.08,(now-last).TotalSeconds));last=now;if(frames++%30==0){Refresh();Panel.RefreshStatus();}for(int i=0;i<3;i++)Buddies[i].Step(dt);}
  public void Refresh(){
   Perches.Clear(); var transform=PresentationSource.FromVisual(Panel).CompositionTarget.TransformFromDevice;
   foreach(var screen in Forms.Screen.AllScreens){var wa=screen.WorkingArea;var a=transform.Transform(new Point(wa.Left,wa.Top));var b=transform.Transform(new Point(wa.Right,wa.Bottom));Perches.Add(new Perch {Left=a.X,Right=b.X,Y=b.Y,Desktop=true,Key=screen.DeviceName});}
   FullscreenHidden=false;var foreground=Native.GetForegroundWindow();uint foregroundPid;Native.GetWindowThreadProcessId(foreground,out foregroundPid);Native.Rect fr;
   if(!Native.IsShell(foreground)&&foregroundPid!=(uint)System.Diagnostics.Process.GetCurrentProcess().Id&&Native.Bounds(foreground,out fr))foreach(var sc in Forms.Screen.AllScreens){var sb=sc.Bounds;if(fr.L<=sb.Left&&fr.T<=sb.Top&&fr.R>=sb.Right&&fr.B>=sb.Bottom)FullscreenHidden=true;}
   if(!Pref.Windows)return;
   Native.EnumWindows((h,unused)=>{uint pid;Native.GetWindowThreadProcessId(h,out pid);int cloaked;Native.Rect r;
    if(Native.IsShell(h)||pid==(uint)System.Diagnostics.Process.GetCurrentProcess().Id||!Native.IsWindowVisible(h)||Native.IsIconic(h)||(Native.GetWindowLong(h,-20)&0x80)!=0)return true;
    if(Native.Cloak(h,14,out cloaked,4)==0&&cloaked!=0)return true;
    if(!Native.Bounds(h,out r)||r.R-r.L<240||r.B-r.T<160)return true;
    // Only keep visible horizontal segments; sample hit-testing against windows above this one.
    var sc=Forms.Screen.FromHandle(h);if(r.T<sc.WorkingArea.Top+Pref.Size||r.T>sc.WorkingArea.Bottom)return true;
    r.L=Math.Max(r.L,sc.WorkingArea.Left);r.R=Math.Min(r.R,sc.WorkingArea.Right);
    int start=-1,segment=0;for(int x=r.L+12;x<=r.R-12;x+=24){bool visible=Native.WindowFromPoint(new Native.POINT(x,r.T+10))==h;
     if(visible&&start<0)start=x;if((!visible||x+24>r.R-12)&&start>=0){int end=visible?x:x-24;if(end-start>Pref.Size*2){var a=transform.Transform(new Point(start,r.T));var b=transform.Transform(new Point(end,r.T));Perches.Add(new Perch{Handle=h,Left=a.X,Right=b.X,Y=a.Y,Key=h.ToString()+":"+segment++});}start=-1;}}
    return true;
   },IntPtr.Zero);
  }
  public Perch Pick(){var homes=Perches.Where(p=>p.Desktop).ToArray();var windows=Perches.Where(p=>!p.Desktop).ToArray();return windows.Length>0&&Random.NextDouble()>.78?windows[Random.Next(windows.Length)]:homes[Random.Next(homes.Length)];}
 }
 public enum Activity {Sit,Walk,Wave,Peek,Travel}
 public class Buddy : Window {
  public Engine E;public int Id; public Activity State; public double X,Y,Elapsed,Duration,FromX,FromY,ToX,ToY;public int Direction=1;public Perch Perch;public string Bubble=""; double bubbleTime;
  BuddyVisual visual;
  public Buddy(Engine e,int id){E=e;Id=id;Width=220;Height=210;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;Focusable=false;IsHitTestVisible=false;Content=visual=new BuddyVisual(this);SourceInitialized+=(s,a)=>{var h=new WindowInteropHelper(this).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x20|0x80|0x08000000);};}
  public void Home(){Perch=E.Perches.FirstOrDefault(p=>p.Desktop);if(Perch==null)return; X=Perch.Left+(Perch.Right-Perch.Left)*(.32+Id*.16);Y=Perch.Y;State=Activity.Sit;Elapsed=0;Duration=6+E.Random.NextDouble()*12;Bubble="";Position();}
  public void Travel(){Perch=E.Pick();FromX=X;FromY=Y;ToX=Clamp(Perch.Left+(Perch.Right-Perch.Left)*E.Random.NextDouble(),Perch.Left+E.Pref.Size,Perch.Right-E.Pref.Size);ToY=Perch.Y;State=Activity.Travel;Elapsed=0;Duration=Id==0?1.4:2.4;}
  public static double Clamp(double x,double lo,double hi){return Math.Max(lo,Math.Min(hi,x));}
  public static double Ease(double t){t=Clamp(t,0,1);return t*t*(3-2*t);}
  void Position(){Left=X-110;Top=Y-175;visual.InvalidateVisual();}
  public void Step(double dt){
   bool hide=!E.Pref.Enabled[Id]||(E.Pref.Fullscreen&&E.FullscreenHidden);Visibility=hide?Visibility.Hidden:Visibility.Visible;if(hide||E.Paused)return;
   if(Perch==null){Home();return;}
   var current=E.Perches.FirstOrDefault(p=>p.Key==Perch.Key);
   if(current==null||(!E.Pref.Windows&&!Perch.Desktop)){Home();return;}
   if(State!=Activity.Travel){Perch=current;Y=current.Y;X=Clamp(X,current.Left+E.Pref.Size/2,current.Right-E.Pref.Size/2);}
   else {ToY=current.Y;ToX=Clamp(ToX,current.Left+E.Pref.Size,current.Right-E.Pref.Size);}
   if(!E.Pref.Roam[Id]&&(State==Activity.Travel||State==Activity.Walk)){Home();}
   Elapsed+=dt;bubbleTime-=dt;if(bubbleTime<=0)Bubble="";
   if(State==Activity.Travel){double t=Ease(Elapsed/Duration);X=FromX+(ToX-FromX)*t;Y=FromY+(ToY-FromY)*t-Math.Sin(t*Math.PI)*(Id==0?90:160);}
   if(State==Activity.Walk){X+=Direction*dt*28;if(X<Perch.Left+E.Pref.Size||X>Perch.Right-E.Pref.Size)Direction=-Direction;}
   if(Elapsed>=Duration){
    Elapsed=0;Duration=5+E.Random.NextDouble()*15;
    double choice=E.Random.NextDouble();if(E.Pref.Roam[Id]&&choice>.84){Travel();}
    else if(E.Pref.Roam[Id]&&choice>.62){State=Activity.Walk;Direction=E.Random.Next(2)==0?-1:1;Duration=2+E.Random.NextDouble()*4;}
    else if(choice>.46){State=Activity.Wave;Duration=2.5;Say();}
    else if(choice>.38&&Perch.Desktop){State=Activity.Peek;Duration=2.8;}
    else State=Activity.Sit;
   }
   Position();
  }
  public void Say(){string[][] lines={new[]{"Hey!","Adventure can wait.","Meat break?"},new[]{"Ten billion percent!","Science break.","Hey, lab partner."},new[]{"Hello.","I'll stay here.","It's peaceful here."}};Bubble=lines[Id][E.Random.Next(3)];bubbleTime=3.5;}
 }
 public class BuddyVisual : FrameworkElement {
  Buddy b;public BuddyVisual(Buddy buddy){b=buddy;}
  protected override void OnRender(DrawingContext d){
   double h=b.E.Pref.Size;bool seated=b.State==Activity.Sit||b.State==Activity.Peek;var img=Art.Get(b.Id,seated);double w=h*img.PixelWidth/img.PixelHeight;double bob=b.State==Activity.Walk?Math.Abs(Math.Sin(b.Elapsed*10))*4:Math.Sin(b.Elapsed*2)*1.3;
   double x=110-w/2,y=175-h-bob;double peek=b.State==Activity.Peek?Math.Sin(Math.Min(1,b.Elapsed/b.Duration)*Math.PI)*h*.65:0;
   var accent=new SolidColorBrush(Engine.Colors[b.Id]);
   if(b.State==Activity.Travel){
    if(b.Id==0){var pen=new Pen(new SolidColorBrush(Color.FromRgb(238,180,135)),7);pen.StartLineCap=pen.EndLineCap=PenLineCap.Round;d.DrawLine(pen,new Point(110,y+h*.55),new Point(110+(b.ToX-b.X)*.22,Math.Min(192,175+(b.ToY-b.Y)*.2)));d.DrawEllipse(accent,null,new Point(110,y+h*.63),7,5);}
    else if(b.Id==1){d.DrawRoundedRectangle(Brushes.DimGray,new Pen(accent,2),new Rect(x-7,y+h*.45,16,h*.35),5,5);double flame=13+Math.Sin(b.Elapsed*35)*6;d.DrawEllipse(Brushes.Orange,null,new Point(x+1,y+h*.85+flame/2),6,flame);d.DrawEllipse(Brushes.LightCyan,null,new Point(x+1,y+h*.85),3,7);}
    else {d.DrawEllipse(null,new Pen(accent,2),new Point(110,y-8),w*.36,5);for(int k=-1;k<=1;k+=2){d.DrawLine(new Pen(accent,3),new Point(110+k*w*.3,y+h*.55),new Point(110+k*w*.83,y+h*.25));d.DrawLine(new Pen(Brushes.White,1),new Point(110+k*w*.3,y+h*.6),new Point(110+k*w*.75,y+h*.39));}d.DrawEllipse(new SolidColorBrush(Color.FromArgb(70,160,185,255)),null,new Point(110,175),w*.45,9);}
   }
   d.PushClip(new RectangleGeometry(new Rect(0,0,220,175)));
   double tilt=b.State==Activity.Wave?Math.Sin(b.Elapsed*8)*7:b.State==Activity.Walk?Math.Sin(b.Elapsed*10)*3:0;
   d.PushTransform(new RotateTransform(tilt,110,175));d.DrawImage(img,new Rect(x,y+peek,w,h));d.Pop();d.Pop();
   if(b.State==Activity.Wave){var pen=new Pen(accent,2);d.DrawLine(pen,new Point(x+w+4,y+10),new Point(x+w+12,y+3));d.DrawLine(pen,new Point(x+w+8,y+20),new Point(x+w+18,y+17));}
   if(b.E.Pref.Bubbles&&b.Bubble.Length>0){var text=new FormattedText(b.Bubble,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,Brushes.White);double bw=Math.Min(208,text.Width+20),by=Math.Max(0,y-37);d.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(35,40,48)),new Pen(accent,1),new Rect(110-bw/2,by,bw,27),10,10);d.DrawText(text,new Point(110-text.Width/2,by+5));}
  }
 }
 public class ControlRoom : Window {
  Engine e;TextBlock status;Button pause;StackPanel cards;TextBlock count;
  static Brush Ink=new SolidColorBrush(Color.FromRgb(232,238,238)),Muted=new SolidColorBrush(Color.FromRgb(143,156,164)),Mint=new SolidColorBrush(Color.FromRgb(166,239,205));
  public ControlRoom(Engine engine){e=engine;Title="Spreety · A little company";Width=1040;Height=850;MinWidth=1040;MinHeight=700;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=new SolidColorBrush(Color.FromRgb(15,20,24));FontFamily=new FontFamily("Segoe UI");Foreground=Ink;
   var root=new Grid();root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(196)});root.ColumnDefinitions.Add(new ColumnDefinition());Content=root;
   var side=new StackPanel{Margin=new Thickness(23,32,20,24)};var sb=new Border{Background=new SolidColorBrush(Color.FromRgb(22,28,32)),Child=side};root.Children.Add(sb);
   side.Children.Add(Text("✦  spreety",25,Ink,true));side.Children.Add(Text("A LITTLE COMPANY",10,Muted,false,10));
   side.Children.Add(Text("YOUR SPACE",10,Muted,false,55));var crew=Text("◉   Desktop crew",13,Mint,true,20);side.Children.Add(crew);side.Children.Add(Text("3 tiny personalities.\nOne happy desktop.",12,Muted,false,20));
   side.Children.Add(Text("VOLUME 01",10,Muted,false,260));side.Children.Add(Text("Made for the edges.\nAnd the in-between.",12,Muted,false,12));
   var main=new StackPanel{Margin=new Thickness(34,28,34,20)};var scroll=new ScrollViewer{Content=main,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetColumn(scroll,1);root.Children.Add(scroll);
   var top=new DockPanel();top.Children.Add(Text("YOUR DESKTOP, A LITTLE MORE ALIVE",10,Muted,true));count=Text("●  3 companions",11,Mint,false);count.HorizontalAlignment=HorizontalAlignment.Right;top.Children.Add(count);main.Children.Add(top);
   main.Children.Add(Text("Meet your edge dwellers.",30,Ink,true,24));main.Children.Add(Text("A tiny crew with nowhere to be. Except right here with you.",13,Muted,false,8));
   var banner=new Border{CornerRadius=new CornerRadius(12),Background=new SolidColorBrush(Color.FromRgb(30,48,43)),Padding=new Thickness(18,13,18,13),Margin=new Thickness(0,22,0,0)};banner.Child=Text("✦   Small adventures. Big personalities.     Most of the day? Just hanging out.",12,Mint,false);main.Children.Add(banner);
   var row=new DockPanel{Margin=new Thickness(0,25,0,14)};row.Children.Add(Text("The crew",17,Ink,true));row.Children.Add(Text("   Choose who stays. Let them wander.",12,Muted,false));main.Children.Add(row);
   cards=new StackPanel{Orientation=Orientation.Horizontal};main.Children.Add(cards);for(int i=0;i<3;i++)Card(i);
   var controls=new Border{Background=new SolidColorBrush(Color.FromRgb(22,29,34)),CornerRadius=new CornerRadius(12),Padding=new Thickness(18),Margin=new Thickness(0,22,0,0)};var settings=new StackPanel();controls.Child=settings;main.Children.Add(controls);
   var settingsTop=new DockPanel();settingsTop.Children.Add(Text("Make yourself at home",15,Ink,true));var sizePanel=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};sizePanel.Children.Add(Text("Sprite size  ",11,Muted,false));var size=new ComboBox{Width=100,Height=25};size.Items.Add("Small · 56");size.Items.Add("Cozy · 76");size.Items.Add("Large · 104");size.SelectedIndex=e.Pref.Size<=60?0:e.Pref.Size>=95?2:1;size.SelectionChanged+=(s,a)=>{e.Pref.Size=new[]{56,76,104}[size.SelectedIndex];e.Home();e.Save();};sizePanel.Children.Add(size);settingsTop.Children.Add(sizePanel);settings.Children.Add(settingsTop);
   var opts=new WrapPanel{Margin=new Thickness(0,17,0,0)};opts.Children.Add(Check("Little greetings",e.Pref.Bubbles,v=>{e.Pref.Bubbles=v;e.Save();}));opts.Children.Add(Check("Explore window tops",e.Pref.Windows,v=>{e.Pref.Windows=v;e.Refresh();e.Save();}));opts.Children.Add(Check("Hide in fullscreen",e.Pref.Fullscreen,v=>{e.Pref.Fullscreen=v;e.Save();}));settings.Children.Add(opts);
   var actions=new DockPanel{Margin=new Thickness(0,20,0,0)};var buttons=new StackPanel{Orientation=Orientation.Horizontal};pause=Btn("Pause crew",()=>{e.Paused=!e.Paused;RefreshStatus();});buttons.Children.Add(pause);buttons.Children.Add(Btn("Bring everyone home",()=>e.Home()));actions.Children.Add(buttons);var quit=Btn("Quit",()=>e.Quit());quit.HorizontalAlignment=HorizontalAlignment.Right;actions.Children.Add(quit);main.Children.Add(actions);
   status=Text("●  Crew is settling in",11,Muted,false,15);main.Children.Add(status);main.Children.Add(Text("Closing this window keeps the crew around. Find Spreety in the system tray.",10,Muted,false,7));
   Closing+=(s,a)=>{a.Cancel=true;Hide();};
  }
  static TextBlock Text(string value,double size,Brush color,bool bold,double top=0){return new TextBlock{Text=value,FontSize=size,Foreground=color,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,Margin=new Thickness(0,top,0,0),TextWrapping=TextWrapping.Wrap};}
  static Button Btn(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(15,9,15,9),Margin=new Thickness(0,0,9,0),Background=new SolidColorBrush(Color.FromRgb(39,49,56)),Foreground=Ink,BorderThickness=new Thickness(0),Cursor=System.Windows.Input.Cursors.Hand};b.Click+=(s,a)=>action();return b;}
  static CheckBox Check(string label,bool selected,Action<bool> change){var c=new CheckBox{Content=label,IsChecked=selected,Foreground=Ink,FontSize=11,Margin=new Thickness(0,0,20,0),VerticalContentAlignment=VerticalAlignment.Center};c.Checked+=(s,a)=>change(true);c.Unchecked+=(s,a)=>change(false);return c;}
  void Card(int id){var col=new SolidColorBrush(Engine.Colors[id]);var b=new Border{Width=242,Margin=new Thickness(0,0,id<2?12:0,0),Background=new SolidColorBrush(Color.FromRgb(24,31,37)),BorderBrush=new SolidColorBrush(Color.FromRgb(43,53,59)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(14),Padding=new Thickness(16)};cards.Children.Add(b);var stack=new StackPanel();b.Child=stack;
   var header=new DockPanel();header.Children.Add(Text("0"+(id+1),10,Muted,true));var tag=Text(new[]{"THE DREAMER","THE INVENTOR","THE QUIET ONE"}[id],9,col,true);tag.HorizontalAlignment=HorizontalAlignment.Right;header.Children.Add(tag);stack.Children.Add(header);
   var art=new Grid{Height=145,Margin=new Thickness(0,8,0,6)};var glow=new System.Windows.Shapes.Ellipse{Width=132,Height=132,Fill=new RadialGradientBrush(Color.FromArgb(40,col.Color.R,col.Color.G,col.Color.B),Colors.Transparent)};art.Children.Add(glow);art.Children.Add(new Image{Source=Art.Get(id,false),Height=136,Stretch=Stretch.Uniform});stack.Children.Add(art);
   stack.Children.Add(Text(new[]{"Luffy","Senku","Rei"}[id],22,Ink,true));stack.Children.Add(Text(new[]{"A little mischief on the horizon.","Good company. Great chemistry.","A quiet moment, just for you."}[id],11,Muted,false,5));stack.Children.Add(Text("↗  "+Engine.Travel[id],10,col,false,12));
   var switches=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,20,0,0)};switches.Children.Add(Check("Show",e.Pref.Enabled[id],v=>{e.Pref.Enabled[id]=v;e.Save();}));switches.Children.Add(Check("Roam",e.Pref.Roam[id],v=>{e.Pref.Roam[id]=v;e.Save();}));stack.Children.Add(switches);
  }
  public void RefreshStatus(){if(status==null)return;count.Text="●  "+e.Pref.Enabled.Count(x=>x)+" companions";pause.Content=e.Paused?"Resume crew":"Pause crew";status.Text=e.SaveFailed?"Preferences could not be saved. Check the local error log.":e.Paused?"Ⅱ  Crew paused · taking a little breather":e.FullscreenHidden&&e.Pref.Fullscreen?"○  Fullscreen detected · the crew is tucked away":"●  Crew is active · mostly hanging out, occasionally going places";}
 }
 public static class Tests {
  static void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
  public static void Run(){var results=new List<string>();
   Assert(Buddy.Ease(-1)==0&&Buddy.Ease(2)==1&&Buddy.Ease(.5)==.5,"Travel easing endpoints");results.Add("PASS travel easing bounds and midpoint");
   Assert(Buddy.Clamp(-20,10,100)==10&&Buddy.Clamp(300,10,100)==100,"Clamp");results.Add("PASS perch coordinate clamping");
   for(int i=0;i<3;i++)for(int j=0;j<2;j++){var a=Art.Get(i,j==1);Assert(a.PixelWidth>10&&a.PixelHeight>10,"Empty sprite");var rgba=new FormatConvertedBitmap(a,PixelFormats.Bgra32,null,0);var pixels=new byte[a.PixelWidth*a.PixelHeight*4];rgba.CopyPixels(pixels,a.PixelWidth*4,0);int transparent=0;for(int k=3;k<pixels.Length;k+=4)if(pixels[k]==0)transparent++;Assert(transparent>pixels.Length/4*.1,"Sprite transparency missing");results.Add("PASS sprite "+i+"/"+j+" visible bounds "+a.PixelWidth+"x"+a.PixelHeight+"; transparent pixels "+transparent);}
   var engine=new Engine();engine.Pref=new Preferences();engine.Perches.Add(new Perch{Left=0,Right=1920,Y=1040,Desktop=true,Key="test"});var buddy=new Buddy(engine,0);buddy.Home();double before=buddy.X;buddy.State=Activity.Walk;buddy.Duration=10;buddy.Step(.5);Assert(buddy.X>before,"Walking did not move");engine.Pref.Roam[0]=false;buddy.Step(.1);Assert(buddy.State==Activity.Sit,"Stay switch did not stop walking");engine.Paused=true;double elapsed=buddy.Elapsed;buddy.Step(.2);Assert(buddy.Elapsed==elapsed,"Paused animation advanced");engine.Pref.Enabled[0]=false;buddy.Step(.1);Assert(buddy.Visibility==Visibility.Hidden,"Hidden buddy still visible");buddy.Close();results.Add("PASS walking, stay switch, pause and visibility transitions");
   string original=Preferences.Folder;Preferences.Folder=Path.Combine(Program.Root,"test-settings");try{Directory.CreateDirectory(Preferences.Folder);var p=new Preferences();p.Enabled[1]=false;p.Roam[2]=false;p.Size=104;Assert(p.Save(),"Save failed");var loaded=Preferences.Read();Assert(!loaded.Enabled[1]&&!loaded.Roam[2]&&loaded.Size==104,"Round trip");results.Add("PASS settings round trip");File.WriteAllText(Path.Combine(Preferences.Folder,"preferences.json"),"broken");Assert(Preferences.Read().Enabled.All(x=>x),"Recovery");results.Add("PASS corrupted settings recovery");}finally{Preferences.Folder=original;}
   File.WriteAllLines(Path.Combine(Program.Root,"test-results.txt"),results);
  }
 }
}
