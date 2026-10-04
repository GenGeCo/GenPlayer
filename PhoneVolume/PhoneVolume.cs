// PhoneVolume - icona nella barra di Windows per regolare il volume media di un telefono Android via ADB.
// Compilazione: build.cmd (usa il compilatore C# incluso in .NET Framework 4.x)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("PhoneVolume")]
[assembly: System.Reflection.AssemblyDescription("Volume media del telefono Android dalla barra di Windows")]
[assembly: System.Reflection.AssemblyProduct("PhoneVolume")]
[assembly: System.Reflection.AssemblyCompany("GeCo gruppogea")]
[assembly: System.Reflection.AssemblyCopyright("Copyright © 2026 GeCo gruppogea")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

static class Program
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main()
    {
        bool created;
        using (var mutex = new Mutex(true, "PhoneVolume_SingleInstance", out created))
        {
            if (!created) return;
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}

// ---------------------------------------------------------------- ADB

class Adb
{
    public string Path;

    static string ConfigDir { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PhoneVolume"); } }
    static string ConfigFile { get { return System.IO.Path.Combine(ConfigDir, "adb_path.txt"); } }

    public static string Find()
    {
        var candidates = new List<string>();
        try { if (File.Exists(ConfigFile)) candidates.Add(File.ReadAllText(ConfigFile).Trim()); } catch { }
        candidates.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb.exe"));
        candidates.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "platform-tools", "adb.exe"));
        foreach (var env in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
        {
            var v = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrEmpty(v)) candidates.Add(System.IO.Path.Combine(v, "platform-tools", "adb.exe"));
        }
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(';'))
        {
            if (dir.Trim().Length == 0) continue;
            try { candidates.Add(System.IO.Path.Combine(dir.Trim(), "adb.exe")); } catch { }
        }
        candidates.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"));
        foreach (var drive in new[] { "C", "D", "E" })
        {
            candidates.Add(drive + @":\AndroidSdk\platform-tools\adb.exe");
            candidates.Add(drive + @":\Android\Sdk\platform-tools\adb.exe");
            candidates.Add(drive + @":\platform-tools\adb.exe");
        }
        foreach (var c in candidates)
        {
            try { if (!string.IsNullOrEmpty(c) && File.Exists(c)) return c; } catch { }
        }
        return null;
    }

    public static void SaveChosenPath(string p)
    {
        try { Directory.CreateDirectory(ConfigDir); File.WriteAllText(ConfigFile, p); } catch { }
    }

    public string Run(string args, int timeoutMs)
    {
        var psi = new ProcessStartInfo(Path, args);
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        using (var p = Process.Start(psi))
        {
            Task<string> outT = p.StandardOutput.ReadToEndAsync();
            Task<string> errT = p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(); } catch { }
                return "";
            }
            return outT.Result + "\n" + errT.Result;
        }
    }
}

class DeviceInfo
{
    public string Serial;
    public string State;   // device, unauthorized, offline
    public string Model;
    public override string ToString() { return (Model ?? Serial) + (State == "device" ? "" : " (" + State + ")"); }
}

// ---------------------------------------------------------------- Applicazione nella barra

class TrayApp : ApplicationContext
{
    readonly NotifyIcon tray = new NotifyIcon();
    readonly ContextMenuStrip menu = new ContextMenuStrip();
    readonly ToolStripMenuItem devicesMenu = new ToolStripMenuItem("Telefono");
    readonly ToolStripMenuItem autostartItem = new ToolStripMenuItem("Avvia con Windows");
    readonly VolumePopup popup;
    readonly Control ui = new Control();   // per tornare sul thread dell'interfaccia
    readonly Adb adb = new Adb();

    // stato condiviso (protetto da lockObj)
    readonly object lockObj = new object();
    readonly AutoResetEvent wake = new AutoResetEvent(false);
    int pendingSet = -1;
    bool pendingRefresh = true;
    volatile bool quitting;

    // stato letto dal thread UI
    List<DeviceInfo> devices = new List<DeviceInfo>();
    string chosenSerial;          // scelta manuale dell'utente
    volatile string activeSerial; // telefono in uso
    int volume = -1, maxVolume = 15;
    int volumeBeforeMute = -1;
    bool useFallback;             // true se il telefono non supporta "cmd media_session volume"
    IntPtr lastIconHandle = IntPtr.Zero;

    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "PhoneVolume";

    public TrayApp()
    {
        ui.CreateControl();
        var h = ui.Handle; // forza la creazione dell'handle

        adb.Path = Adb.Find();

        popup = new VolumePopup();
        popup.VolumeChosen += v => RequestSet(v);
        popup.MuteClicked += ToggleMute;

        autostartItem.Checked = IsAutostart();
        autostartItem.Click += (s, e) => { SetAutostart(!autostartItem.Checked); autostartItem.Checked = IsAutostart(); };
        var adbItem = new ToolStripMenuItem("Scegli adb.exe...", null, (s, e) => ChooseAdb());
        var exitItem = new ToolStripMenuItem("Esci", null, (s, e) => Quit());
        devicesMenu.DropDownItems.Add(new ToolStripMenuItem("(nessuno)") { Enabled = false });
        menu.Items.AddRange(new ToolStripItem[] { devicesMenu, autostartItem, adbItem, new ToolStripSeparator(), exitItem });

        tray.ContextMenuStrip = menu;
        tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) TogglePopup(); };
        tray.Visible = true;
        UpdateIcon();

        if (adb.Path == null)
        {
            tray.ShowBalloonTip(5000, "PhoneVolume", "adb.exe non trovato. Clic destro sull'icona → \"Scegli adb.exe...\"", ToolTipIcon.Warning);
        }

        var worker = new Thread(WorkerLoop);
        worker.IsBackground = true;
        worker.Start();
    }

    // ---------- azioni dell'utente

    void TogglePopup()
    {
        if (popup.Visible) { popup.Hide(); return; }
        if ((DateTime.Now - popup.LastHidden).TotalMilliseconds < 250) return; // il clic che ha appena chiuso il pannello
        popup.SetState(activeSerial != null, DeviceLabel(), volume, maxVolume);
        popup.ShowNearCursor();
        lock (lockObj) pendingRefresh = true;
        wake.Set();
    }

    void RequestSet(int v)
    {
        lock (lockObj) pendingSet = v;
        volumeBeforeMute = -1;
        volume = v;
        UpdateIcon();
        wake.Set();
    }

    void ToggleMute()
    {
        if (volume > 0)
        {
            int prev = volume;
            RequestSet(0);
            volumeBeforeMute = prev;
        }
        else
        {
            int restore = volumeBeforeMute > 0 ? volumeBeforeMute : Math.Max(1, maxVolume / 3);
            RequestSet(restore);
        }
        popup.SetState(activeSerial != null, DeviceLabel(), volume, maxVolume);
    }

    void ChooseAdb()
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Title = "Seleziona adb.exe";
            dlg.Filter = "adb.exe|adb.exe";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                adb.Path = dlg.FileName;
                Adb.SaveChosenPath(dlg.FileName);
                lock (lockObj) pendingRefresh = true;
                wake.Set();
            }
        }
    }

    void Quit()
    {
        quitting = true;
        wake.Set();
        tray.Visible = false;
        tray.Dispose();
        if (lastIconHandle != IntPtr.Zero) DestroyIcon(lastIconHandle);
        ExitThread();
    }

    // ---------- thread che parla con adb (mai sul thread UI)

    void WorkerLoop()
    {
        string lastSerial = null;
        while (!quitting)
        {
            wake.WaitOne(3000);
            if (quitting) break;
            if (adb.Path == null) { adb.Path = Adb.Find(); if (adb.Path == null) continue; }

            try
            {
                var devs = ListDevices();
                string serial = PickSerial(devs);
                bool changed = serial != lastSerial;
                lastSerial = serial;
                activeSerial = serial;

                int set; bool refresh;
                lock (lockObj)
                {
                    set = pendingSet; pendingSet = -1;
                    refresh = pendingRefresh || changed; pendingRefresh = false;
                }

                if (serial != null && set >= 0)
                {
                    SetVolume(serial, set);
                    // se nel frattempo l'utente ha trascinato ancora, invia subito l'ultimo valore
                    lock (lockObj) { if (pendingSet >= 0) { wake.Set(); } }
                }

                int vol = -1, max = -1;
                bool doRead = serial != null && (refresh || popup.VisibleSafe);
                if (doRead && pendingSet < 0) GetVolume(serial, out vol, out max);

                ui.BeginInvoke((Action)(() => OnWorkerResult(devs, serial, vol, max)));
            }
            catch { }
        }
    }

    List<DeviceInfo> ListDevices()
    {
        var list = new List<DeviceInfo>();
        string outp = adb.Run("devices -l", 5000);
        foreach (var raw in outp.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("List of") || line.StartsWith("*")) continue;
            var parts = Regex.Split(line, @"\s+");
            if (parts.Length < 2) continue;
            var d = new DeviceInfo { Serial = parts[0], State = parts[1] };
            var m = Regex.Match(line, @"model:(\S+)");
            if (m.Success) d.Model = m.Groups[1].Value.Replace('_', '-');
            list.Add(d);
        }
        return list;
    }

    string PickSerial(List<DeviceInfo> devs)
    {
        string chosen = chosenSerial;
        if (chosen != null && devs.Exists(d => d.Serial == chosen && d.State == "device")) return chosen;
        var first = devs.Find(d => d.State == "device");
        return first != null ? first.Serial : null;
    }

    static readonly Regex VolRegex = new Regex(@"volume is (\d+) in range \[(\d+)\.\.(\d+)\]");

    void GetVolume(string serial, out int vol, out int max)
    {
        vol = -1; max = -1;
        if (!useFallback)
        {
            string o = adb.Run("-s " + serial + " shell cmd media_session volume --stream 3 --get", 5000);
            var m = VolRegex.Match(o);
            if (m.Success)
            {
                vol = int.Parse(m.Groups[1].Value);
                max = int.Parse(m.Groups[3].Value);
                return;
            }
            useFallback = true;
        }
        // metodo di riserva: legge da dumpsys audio
        string d = adb.Run("-s " + serial + " shell dumpsys audio", 8000);
        int start = d.IndexOf("- STREAM_MUSIC:");
        if (start < 0) return;
        int end = d.IndexOf("- STREAM_", start + 10);
        string sec = end > 0 ? d.Substring(start, end - start) : d.Substring(start);
        var mx = Regex.Match(sec, @"Max:\s*(\d+)");
        var sv = Regex.Match(sec, @"streamVolume:\s*(\d+)");
        if (!sv.Success) sv = Regex.Match(sec, @"\(speaker\):\s*(\d+)");
        if (mx.Success) max = int.Parse(mx.Groups[1].Value);
        if (sv.Success) vol = int.Parse(sv.Groups[1].Value);
        // alcuni telefoni riportano i valori x10 (indice interno)
        if (max > 0 && vol > max && vol <= max * 10) vol = (vol + 5) / 10;
    }

    void SetVolume(string serial, int target)
    {
        if (!useFallback)
        {
            adb.Run("-s " + serial + " shell cmd media_session volume --stream 3 --set " + target, 5000);
            return;
        }
        // metodo di riserva: simula i tasti volume del telefono
        int cur, max;
        GetVolume(serial, out cur, out max);
        if (cur < 0) return;
        int diff = target - cur;
        if (diff == 0) return;
        string key = diff > 0 ? "24" : "25";
        var keys = new List<string>();
        for (int i = 0; i < Math.Abs(diff); i++) keys.Add(key);
        adb.Run("-s " + serial + " shell input keyevent " + string.Join(" ", keys), 8000);
    }

    // ---------- ritorno sul thread UI

    void OnWorkerResult(List<DeviceInfo> devs, string serial, int vol, int max)
    {
        devices = devs;
        RebuildDevicesMenu();
        if (serial == null) { volume = -1; }
        lock (lockObj)
        {
            if (vol >= 0 && pendingSet < 0) { volume = vol; if (max > 0) maxVolume = max; }
        }
        UpdateIcon();
        if (popup.Visible && !popup.UserIsDragging)
            popup.SetState(serial != null, DeviceLabel(), volume, maxVolume);
    }

    void RebuildDevicesMenu()
    {
        devicesMenu.DropDownItems.Clear();
        if (devices.Count == 0)
        {
            devicesMenu.DropDownItems.Add(new ToolStripMenuItem("(nessun telefono collegato)") { Enabled = false });
            return;
        }
        foreach (var d in devices)
        {
            var dev = d;
            var item = new ToolStripMenuItem(dev.ToString());
            item.Checked = dev.Serial == activeSerial;
            item.Enabled = dev.State == "device";
            item.Click += (s, e) => { chosenSerial = dev.Serial; useFallback = false; lock (lockObj) pendingRefresh = true; wake.Set(); };
            devicesMenu.DropDownItems.Add(item);
        }
    }

    string DeviceLabel()
    {
        var s = activeSerial;
        if (s == null)
        {
            if (devices.Exists(d => d.State == "unauthorized")) return "Autorizza il debug USB sul telefono";
            return adb.Path == null ? "adb.exe non trovato" : "Nessun telefono collegato";
        }
        var d2 = devices.Find(d => d.Serial == s);
        return d2 != null && d2.Model != null ? d2.Model : s;
    }

    // ---------- icona

    void UpdateIcon()
    {
        bool connected = activeSerial != null && volume >= 0;
        string tip;
        if (!connected) tip = "Volume telefono: " + DeviceLabel();
        else tip = "Volume telefono: " + volume + "/" + maxVolume + " (" + (maxVolume > 0 ? volume * 100 / maxVolume : 0) + "%)";
        if (tip.Length > 63) tip = tip.Substring(0, 63);
        tray.Text = tip;

        var newIcon = IconPainter.Draw(connected, connected ? volume : 0, maxVolume);
        var old = lastIconHandle;
        tray.Icon = Icon.FromHandle(newIcon);
        lastIconHandle = newIcon;
        if (old != IntPtr.Zero) DestroyIcon(old);
    }

    // ---------- avvio automatico

    static bool IsAutostart()
    {
        using (var k = Registry.CurrentUser.OpenSubKey(RunKey))
            return k != null && k.GetValue(RunName) != null;
    }

    static void SetAutostart(bool on)
    {
        using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
        {
            if (k == null) return;
            if (on) k.SetValue(RunName, "\"" + Application.ExecutablePath + "\"");
            else k.DeleteValue(RunName, false);
        }
    }
}

// ---------------------------------------------------------------- Disegno dell'icona

static class IconPainter
{
    public static bool TaskbarIsLight()
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object v = k != null ? k.GetValue("SystemUsesLightTheme") : null;
                return v is int && (int)v == 1;
            }
        }
        catch { return false; }
    }

    public static IntPtr Draw(bool connected, int vol, int max)
    {
        int size = SystemInformation.SmallIconSize.Width * 2;
        if (size < 32) size = 32;
        float u = size / 32f;
        using (var bmp = new Bitmap(size, size))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            bool light = TaskbarIsLight();
            Color fg = connected ? (light ? Color.FromArgb(30, 30, 30) : Color.White) : Color.FromArgb(140, 140, 140);
            Color accent = connected ? Color.FromArgb(0, 150, 255) : Color.FromArgb(110, 110, 110);

            // corpo del telefono
            var body = new RectangleF(7 * u, 2 * u, 18 * u, 28 * u);
            using (var path = RoundRect(body, 4 * u))
            using (var pen = new Pen(fg, 2.6f * u))
                g.DrawPath(pen, path);

            // barra del livello dentro lo schermo
            var screen = new RectangleF(10.5f * u, 6 * u, 11 * u, 20 * u);
            if (connected && max > 0 && vol > 0)
            {
                float h = screen.Height * vol / max;
                using (var b = new SolidBrush(accent))
                    g.FillRectangle(b, screen.X, screen.Bottom - h, screen.Width, h);
            }
            if (connected && vol == 0)
            {
                using (var pen = new Pen(Color.FromArgb(230, 60, 60), 3f * u))
                {
                    g.DrawLine(pen, 11 * u, 10 * u, 21 * u, 22 * u);
                    g.DrawLine(pen, 21 * u, 10 * u, 11 * u, 22 * u);
                }
            }
            return bmp.GetHicon();
        }
    }

    public static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

// ---------------------------------------------------------------- Cursore: un livello per ogni scatto della rotella

class StepTrackBar : TrackBar
{
    public event Action<int> WheelStep;
    int accumulated;

    public void ApplyWheel(int delta)
    {
        accumulated += delta;
        while (accumulated >= 120) { accumulated -= 120; if (WheelStep != null) WheelStep(+1); }
        while (accumulated <= -120) { accumulated += 120; if (WheelStep != null) WheelStep(-1); }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x020A) // WM_MOUSEWHEEL: gestito qui invece che dal controllo standard
        {
            ApplyWheel((short)((m.WParam.ToInt64() >> 16) & 0xFFFF));
            return;
        }
        base.WndProc(ref m);
    }
}

// ---------------------------------------------------------------- Pannello con il cursore

class VolumePopup : Form
{
    public event Action<int> VolumeChosen;
    public event Action MuteClicked;
    public DateTime LastHidden = DateTime.MinValue;
    public bool UserIsDragging;
    volatile bool visibleSafe;
    public bool VisibleSafe { get { return visibleSafe; } }

    readonly Label title = new Label();
    readonly Label percent = new Label();
    readonly StepTrackBar slider = new StepTrackBar();
    readonly Button mute = new Button();
    readonly System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer();
    bool updating;
    Color border;

    public VolumePopup()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(320, 108);
        KeyPreview = true;

        bool dark = !AppsLight();
        Color bg = dark ? Color.FromArgb(43, 43, 43) : Color.FromArgb(249, 249, 249);
        Color fg = dark ? Color.White : Color.FromArgb(25, 25, 25);
        border = dark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(200, 200, 200);
        BackColor = bg; ForeColor = fg;

        title.SetBounds(14, 10, 292, 22);
        title.Font = new Font("Segoe UI Semibold", 9.5f);
        title.Text = "Volume media";

        mute.SetBounds(12, 42, 70, 30);
        mute.FlatStyle = FlatStyle.Flat;
        mute.FlatAppearance.BorderColor = border;
        mute.BackColor = dark ? Color.FromArgb(60, 60, 60) : Color.White;
        mute.Text = "Muto";
        mute.Click += (s, e) => { if (MuteClicked != null) MuteClicked(); };

        slider.SetBounds(88, 40, 180, 36);
        slider.TickStyle = TickStyle.None;
        slider.Minimum = 0; slider.Maximum = 15;
        slider.BackColor = bg;
        slider.SmallChange = 1;
        slider.LargeChange = 1;
        slider.Scroll += (s, e) => UserChanged();
        slider.WheelStep += d =>
        {
            int v = Math.Max(slider.Minimum, Math.Min(slider.Maximum, slider.Value + d));
            if (v != slider.Value && slider.Enabled) { slider.Value = v; UserChanged(); }
        };
        slider.MouseUp += (s, e) => Flush();

        percent.SetBounds(270, 46, 46, 22);
        percent.TextAlign = ContentAlignment.MiddleRight;

        var hint = new Label();
        hint.SetBounds(14, 80, 292, 20);
        hint.ForeColor = Color.FromArgb(140, 140, 140);
        hint.Font = new Font("Segoe UI", 8f);
        hint.Text = "Rotella del mouse o frecce per regolare";

        // invia il valore poco dopo l'ultimo movimento, per non mandare decine di comandi
        debounce.Interval = 120;
        debounce.Tick += (s, e) => Flush();

        Controls.AddRange(new Control[] { title, mute, slider, percent, hint });
        Deactivate += (s, e) => HidePopup();
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) HidePopup(); };
        MouseWheel += (s, e) => slider.ApplyWheel(e.Delta);
    }

    void UserChanged()
    {
        if (updating) return;
        UserIsDragging = true;
        percent.Text = Pct(slider.Value, slider.Maximum);
        mute.Text = slider.Value == 0 ? "Riattiva" : "Muto";
        debounce.Stop();
        debounce.Start();
    }

    void Flush()
    {
        debounce.Stop();
        if (UserIsDragging && VolumeChosen != null) VolumeChosen(slider.Value);
        UserIsDragging = false;
    }

    void HidePopup()
    {
        Flush();
        LastHidden = DateTime.Now;
        visibleSafe = false;
        Hide();
    }

    static string Pct(int v, int max) { return (max > 0 ? v * 100 / max : 0) + "%"; }

    static bool AppsLight()
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object v = k != null ? k.GetValue("AppsUseLightTheme") : null;
                return !(v is int) || (int)v == 1;
            }
        }
        catch { return true; }
    }

    public void SetState(bool connected, string label, int vol, int max)
    {
        updating = true;
        title.Text = "Volume media — " + label;
        slider.Enabled = connected && vol >= 0;
        mute.Enabled = slider.Enabled;
        if (max > 0) slider.Maximum = max;
        if (vol >= 0) slider.Value = Math.Min(vol, slider.Maximum);
        percent.Text = vol >= 0 ? Pct(vol, max) : "—";
        mute.Text = vol == 0 ? "Riattiva" : "Muto";
        updating = false;
    }

    public void ShowNearCursor()
    {
        var pt = Cursor.Position;
        var wa = Screen.FromPoint(pt).WorkingArea;
        int x = Math.Min(pt.X - Width / 2, wa.Right - Width - 12);
        int y = Math.Min(pt.Y - Height - 16, wa.Bottom - Height - 12);
        x = Math.Max(x, wa.Left + 12);
        y = Math.Max(y, wa.Top + 12);
        Location = new Point(x, y);
        visibleSafe = true;
        Show();
        Activate();
        slider.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (var pen = new Pen(border))
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; /* CS_DROPSHADOW */ return cp; }
    }
}
