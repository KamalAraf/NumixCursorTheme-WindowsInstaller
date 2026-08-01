using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NumixCursorsManager
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        private const uint SPI_SETCURSORS        = 0x0057;
        private const uint SPI_SETCURSORSIZE     = 0x2029; // undocumented, used by SystemSettings.exe
        private const uint SPIF_UPDATEINIFILE    = 0x01;
        private const uint SPIF_SENDCHANGE       = 0x02;

        private const string CURSORS_REG = @"Control Panel\Cursors";
        private const string SCHEMES_REG = @"Control Panel\Cursors\Schemes";
        private const string ACCESS_REG  = @"Software\Microsoft\Accessibility";

        private static readonly string WinRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        private int origBaseSize = 32, origSliderSize = 1;
        private bool origBaseExists, origSliderExists;

        private string[] variantNames;
        private string[] variantInstallDirs;
        private string[] variantSchemeValues;

        private static readonly string DefArrow      = WinRoot + @"\cursors\aero_arrow.cur";
        private static readonly string DefHelp        = WinRoot + @"\cursors\aero_helpsel.cur";
        private static readonly string DefAppStarting = WinRoot + @"\cursors\aero_working.ani";
        private static readonly string DefWait        = WinRoot + @"\cursors\aero_busy.ani";
        private static readonly string DefCrosshair   = WinRoot + @"\cursors\aero_crosshair.cur";
        private static readonly string DefIBeam       = WinRoot + @"\cursors\aero_ibeam.cur";
        private static readonly string DefNWPen       = WinRoot + @"\cursors\aero_pen.cur";
        private static readonly string DefNo          = WinRoot + @"\cursors\aero_unavail.cur";
        private static readonly string DefSizeNS      = WinRoot + @"\cursors\aero_ns.cur";
        private static readonly string DefSizeWE      = WinRoot + @"\cursors\aero_ew.cur";
        private static readonly string DefSizeNWSE    = WinRoot + @"\cursors\aero_nwse.cur";
        private static readonly string DefSizeNESW    = WinRoot + @"\cursors\aero_nesw.cur";
        private static readonly string DefSizeAll     = WinRoot + @"\cursors\aero_move.cur";
        private static readonly string DefUpArrow     = WinRoot + @"\cursors\aero_up.cur";
        private static readonly string DefHand        = WinRoot + @"\cursors\aero_link.cur";

        private RadioButton rbInstall, rbUninstall, rbSetActive, rbRestore;
        private RadioButton rbVariantDark, rbVariantLight;
        private CheckBox cbSetActive;
        private Label lblStatus, lblSizeValue;
        private TrackBar tbSize;
        private Button btnApply;

        private static readonly string LogFile = Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath) ?? ".", "numix-install.log");

        private static void Log(string msg)
        {
            try { File.AppendAllText(LogFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + msg + Environment.NewLine); }
            catch { }
        }

        public MainForm()
        {
            variantNames = new string[] { "Numix-Cursor", "Numix-Cursor-Light" };
            variantInstallDirs = new string[]
            {
                Path.Combine(WinRoot, "Cursors", variantNames[0]),
                Path.Combine(WinRoot, "Cursors", variantNames[1]),
            };
            variantSchemeValues = new string[]
            {
                string.Format(@"{0}\default.cur,{0}\help.cur,{0}\progress.ani,{0}\wait.ani,{0}\crosshair.cur,{0}\text.cur,{0}\pencil.cur,{0}\not-allowed.cur,{0}\size_ver.cur,{0}\size_hor.cur,{0}\size_fdiag.cur,{0}\size_bdiag.cur,{0}\fleur.cur,{0}\up-arrow.cur,{0}\pointer.cur", variantInstallDirs[0]),
                string.Format(@"{0}\default.cur,{0}\help.cur,{0}\progress.ani,{0}\wait.ani,{0}\crosshair.cur,{0}\text.cur,{0}\pencil.cur,{0}\not-allowed.cur,{0}\size_ver.cur,{0}\size_hor.cur,{0}\size_fdiag.cur,{0}\size_bdiag.cur,{0}\fleur.cur,{0}\up-arrow.cur,{0}\pointer.cur", variantInstallDirs[1]),
            };
            InitializeComponent();
            LoadAppIcon();
            ReadOriginalPointerSize();
        }

        private void ReadOriginalPointerSize()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG))
            {
                if (key == null) return;
                object v = key.GetValue("CursorBaseSize");
                if (v is int) { origBaseSize = (int)v; origBaseExists = true; }
            }
            using (var key = Registry.CurrentUser.OpenSubKey(ACCESS_REG))
            {
                if (key == null) return;
                object v = key.GetValue("CursorSize");
                if (v is int) { origSliderSize = (int)v; origSliderExists = true; }
            }
            Log("ReadOriginalPointerSize: base=" + origBaseSize + " exists=" + origBaseExists +
                " slider=" + origSliderSize + " exists=" + origSliderExists);
        }

        private void ApplyPointerSize(int size)
        {
            int slider = 1 + (size - 32) / 16;
            Log("ApplyPointerSize: base=" + size + " slider=" + slider);
            using (var key = Registry.CurrentUser.CreateSubKey(CURSORS_REG))
            {
                if (key == null) throw new Exception("Failed to open registry key: " + CURSORS_REG);
                key.SetValue("CursorBaseSize", size, RegistryValueKind.DWord);
            }
            using (var key = Registry.CurrentUser.CreateSubKey(ACCESS_REG))
            {
                if (key == null) throw new Exception("Failed to open registry key: " + ACCESS_REG);
                key.SetValue("CursorSize", slider, RegistryValueKind.DWord);
            }
            bool ok = SystemParametersInfo(SPI_SETCURSORSIZE, 0, (IntPtr)size, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            int lastErr = Marshal.GetLastWin32Error();
            Log("ApplyPointerSize: SPI 0x2029 returned " + ok + " lastWin32Error=" + lastErr);
            if (!ok)
                throw new Exception(string.Format("Pointer size was saved to the registry but could not be applied immediately (error {0}). Changes will take effect after restarting Explorer.", lastErr));
        }

        private void RestorePointerSize()
        {
            Log("RestorePointerSize: base=" + origBaseSize + " slider=" + origSliderSize);
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG, true))
            {
                if (key == null) throw new Exception("Failed to open registry key: " + CURSORS_REG);
                if (origBaseExists) key.SetValue("CursorBaseSize", origBaseSize, RegistryValueKind.DWord);
                else key.DeleteValue("CursorBaseSize", false);
            }
            using (var key = Registry.CurrentUser.OpenSubKey(ACCESS_REG, true))
            {
                if (key == null) throw new Exception("Failed to open registry key: " + ACCESS_REG);
                if (origSliderExists) key.SetValue("CursorSize", origSliderSize, RegistryValueKind.DWord);
                else key.DeleteValue("CursorSize", false);
            }
            bool ok = SystemParametersInfo(SPI_SETCURSORSIZE, 0, (IntPtr)origBaseSize, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            int lastErr = Marshal.GetLastWin32Error();
            Log("RestorePointerSize: SPI 0x2029 returned " + ok + " lastWin32Error=" + lastErr);
        }

        private void LoadAppIcon()
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                if (exeDir == null) return;

                string iconPath = Path.Combine(exeDir, "..", "assets", "logo.ico");
                if (File.Exists(iconPath))
                {
                    using (var icon = new Icon(iconPath))
                    this.Icon = (Icon)icon.Clone();
                }
            }
            catch { }
        }

        private int SelectedVariant()
        {
            return rbVariantLight.Checked ? 1 : 0;
        }

        private int SelectedSize()
        {
            return 32 + (tbSize.Value - 1) * 16;
        }

        private string SchemeName()
        {
            return variantNames[SelectedVariant()];
        }

        private string InstallDir()
        {
            return variantInstallDirs[SelectedVariant()];
        }

        private string SchemeValue()
        {
            return variantSchemeValues[SelectedVariant()];
        }

        private string CursorDirName()
        {
            return SelectedVariant() == 0 ? "dark" : "light";
        }

        private void InitializeComponent()
        {
            Text = "Numix Cursors Manager";
            ClientSize = new Size(460, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            var gbVariant = new GroupBox();
            gbVariant.Text = "Cursor Variant";
            gbVariant.Location = new Point(20, 20);
            gbVariant.Size = new Size(400, 70);

            rbVariantDark  = new RadioButton();
            rbVariantDark.Text = "Numix Cursor Dark";
            rbVariantDark.Location = new Point(15, 22);
            rbVariantDark.Checked = true;
            rbVariantDark.AutoSize = true;
            rbVariantDark.CheckedChanged += OnVariantChanged;

            rbVariantLight = new RadioButton();
            rbVariantLight.Text = "Numix Cursor Light";
            rbVariantLight.Location = new Point(15, 44);
            rbVariantLight.AutoSize = true;

            gbVariant.Controls.AddRange(new Control[] { rbVariantDark, rbVariantLight });

            var gbAction = new GroupBox();
            gbAction.Text = "Action";
            gbAction.Location = new Point(20, 105);
            gbAction.Size = new Size(400, 145);

            rbInstall   = new RadioButton();
            rbInstall.Text = "Install";
            rbInstall.Location = new Point(15, 25);
            rbInstall.Checked = true;
            rbInstall.AutoSize = true;

            rbUninstall = new RadioButton();
            rbUninstall.Text = "Uninstall";
            rbUninstall.Location = new Point(15, 50);
            rbUninstall.AutoSize = true;

            rbSetActive = new RadioButton();
            rbSetActive.Text = "Set Active";
            rbSetActive.Location = new Point(15, 75);
            rbSetActive.AutoSize = true;

            rbRestore   = new RadioButton();
            rbRestore.Text = "Restore Windows Default";
            rbRestore.Location = new Point(15, 100);
            rbRestore.AutoSize = true;

            rbInstall.CheckedChanged += OnActionChanged;

            gbAction.Controls.AddRange(new Control[] { rbInstall, rbUninstall, rbSetActive, rbRestore });

            var gbOptions = new GroupBox();
            gbOptions.Text = "Options";
            gbOptions.Location = new Point(20, 265);
            gbOptions.Size = new Size(400, 60);
            cbSetActive = new CheckBox();
            cbSetActive.Text = "Set as active cursor immediately";
            cbSetActive.Location = new Point(15, 22);
            cbSetActive.Checked = true;
            cbSetActive.AutoSize = true;
            gbOptions.Controls.Add(cbSetActive);

            var gbSize = new GroupBox();
            gbSize.Text = "Pointer Size (matches Windows slider 1-15)";
            gbSize.Location = new Point(20, 335);
            gbSize.Size = new Size(400, 70);
            tbSize = new TrackBar();
            tbSize.Minimum = 1;
            tbSize.Maximum = 15;
            tbSize.Value = 1;
            tbSize.SmallChange = 1;
            tbSize.LargeChange = 1;
            tbSize.TickFrequency = 1;
            tbSize.Location = new Point(15, 16);
            tbSize.Size = new Size(300, 45);
            tbSize.ValueChanged += OnSizeChanged;
            lblSizeValue = new Label();
            lblSizeValue.Text = "32 px";
            lblSizeValue.Location = new Point(325, 29);
            lblSizeValue.AutoSize = true;
            gbSize.Controls.AddRange(new Control[] { tbSize, lblSizeValue });

            btnApply = new Button();
            btnApply.Text = "Apply";
            btnApply.Location = new Point(20, 420);
            btnApply.Size = new Size(120, 35);
            btnApply.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnApply.BackColor = Color.FromArgb(0, 120, 215);
            btnApply.ForeColor = Color.White;
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += OnApplyClick;

            lblStatus = new Label();
            lblStatus.Text = "Ready.";
            lblStatus.Location = new Point(20, 470);
            lblStatus.Size = new Size(400, 40);
            lblStatus.ForeColor = Color.Gray;

            Controls.AddRange(new Control[] { gbVariant, gbAction, gbOptions, gbSize, btnApply, lblStatus });
            UpdateStatusLabel();
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            lblSizeValue.Text = SelectedSize() + " px";
            UpdateStatusLabel();
        }

        private void OnVariantChanged(object sender, EventArgs e)
        {
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (rbVariantLight.Checked)
                lblStatus.Text = "Selected: Numix Cursor Light (" + SelectedSize() + " px)";
            else
                lblStatus.Text = "Selected: Numix Cursor Dark (" + SelectedSize() + " px)";
        }

        private void OnActionChanged(object sender, EventArgs e)
        {
            cbSetActive.Enabled = rbInstall.Checked;
            cbSetActive.Checked = rbInstall.Checked;
            tbSize.Enabled = rbInstall.Checked;
        }

        private void ResetUI()
        {
            btnApply.Enabled = true;
            UseWaitCursor    = false;
            UpdateStatusLabel();
        }

        private async void OnApplyClick(object sender, EventArgs e)
        {
            btnApply.Enabled = false;
            UseWaitCursor    = true;
            lblStatus.Text   = "Processing...";
            bool showSuccess = true;
            try
            {
                string installDir = InstallDir();
                string schemeName = SchemeName();
                string schemeValue = SchemeValue();
                string cursorDir = CursorDirName();
                int size = SelectedSize();

                Log("=== Apply clicked: action=" + (rbInstall.Checked ? "Install" : rbUninstall.Checked ? "Uninstall" : rbSetActive.Checked ? "SetActive" : "Restore") +
                    " variant=" + (rbVariantLight.Checked ? "light" : "dark") +
                    " size=" + size +
                    " installDir=" + installDir + " schemeName=" + schemeName);

                if (rbInstall.Checked)
                {
                    if (Directory.Exists(installDir) &&
                        MessageBox.Show(schemeName + " is already installed. Reinstall?", "Confirm",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        showSuccess = false;
                        return;
                    }
                    lblStatus.Text = "Installing...";
                    bool setActive = cbSetActive.Checked;
                    string dir = cursorDir;
                    await System.Threading.Tasks.Task.Run(() => Install(setActive, dir, installDir, schemeName, schemeValue, size));
                }
                else if (rbUninstall.Checked)
                {
                    if (!Directory.Exists(installDir))
                    {
                        MessageBox.Show(schemeName + " is not installed.", "Info",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        showSuccess = false;
                        return;
                    }
                    lblStatus.Text = "Uninstalling...";
                    await System.Threading.Tasks.Task.Run(() => Uninstall(installDir, schemeName));
                }
                else if (rbSetActive.Checked)
                {
                    if (!Directory.Exists(installDir))
                    {
                        var result = MessageBox.Show(schemeName + " is not installed. Install now?", "Not Found",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            lblStatus.Text = "Installing...";
                            string dir = cursorDir;
                            await System.Threading.Tasks.Task.Run(() => Install(true, dir, installDir, schemeName, schemeValue, size));
                            MessageBox.Show("Operation completed successfully.", "Done",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            lblStatus.Text = "Ready.";
                        }
                        showSuccess = false;
                        return;
                    }
                    lblStatus.Text = "Setting active cursor...";
                    await System.Threading.Tasks.Task.Run(() => SetActiveCursor(installDir, SelectedSize()));
                }
                else if (rbRestore.Checked)
                {
                    if (IsDefaultActive())
                    {
                        MessageBox.Show("Windows default cursor theme is already active.", "Info",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        showSuccess = false;
                        return;
                    }
                    lblStatus.Text = "Restoring default...";
                    await System.Threading.Tasks.Task.Run(() => RestoreDefault());
                }

                if (showSuccess)
                {
                    MessageBox.Show("Operation completed successfully.", "Done",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log("ERROR UnauthorizedAccessException: " + ex.Message);
                MessageBox.Show("Access denied. Run as Administrator.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error: Access denied.";
            }
            catch (DirectoryNotFoundException ex)
            {
                Log("ERROR DirectoryNotFoundException: " + ex.Message);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error: Files missing.";
            }
            catch (Exception ex)
            {
                Log("ERROR Exception: " + ex.Message + " | StackTrace: " + ex.StackTrace);
                MessageBox.Show(string.Format("Unexpected error:\n{0}", ex.Message), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error occurred.";
            }
            finally
            {
                ResetUI();
            }
        }

        private bool IsDefaultActive()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG))
            {
                if (key == null) return false;
                string val = key.GetValue("Arrow") as string;
                return val != null && val.Equals(DefArrow, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void Install(bool setActive, string cursorDirName, string installDir, string schemeName, string schemeValue, int size)
        {
            string[] staticNames = new string[]
            {
                "default.cur", "help.cur", "crosshair.cur", "text.cur", "pencil.cur", "not-allowed.cur",
                "size_ver.cur", "size_hor.cur", "size_fdiag.cur", "size_bdiag.cur", "fleur.cur",
                "up-arrow.cur", "pointer.cur"
            };
            string[] animatedNames = new string[] { "progress.ani", "wait.ani" };

            Log("Install: variant=" + cursorDirName + " size=" + size + " installDir=" + installDir);
            Directory.CreateDirectory(installDir);
            Log("Install: created/verified installDir=" + installDir);

            string prefix = "NumixCursors.cursors." + cursorDirName + ".";
            foreach (var name in staticNames)
            {
                WriteScaledCursor(prefix + "static." + name, Path.Combine(installDir, name), size);
                Log("Install: extracted " + name + " (" + size + "px) -> " + installDir);
            }
            foreach (var name in animatedNames)
            {
                WriteScaledCursor(prefix + "animated." + name, Path.Combine(installDir, name), size);
                Log("Install: extracted " + name + " (" + size + "px) -> " + installDir);
            }

            foreach (var f in Directory.EnumerateFiles(installDir))
                Log("Install: verify file in installDir: " + Path.GetFileName(f) + " size=" + new FileInfo(f).Length);

            using (var key = Registry.CurrentUser.CreateSubKey(SCHEMES_REG))
            {
                if (key == null)
                    throw new Exception("Failed to open or create registry key: " + SCHEMES_REG);
                key.SetValue(schemeName, schemeValue);
                Log("Install: scheme written. name='" + schemeName + "' data='" + schemeValue + "'");
            }

            if (setActive) SetActiveCursor(installDir, size);
            Log("Install: completed");
        }

        private void WriteScaledCursor(string resourceName, string destPath, int size)
        {
            byte[] data;
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new DirectoryNotFoundException("Embedded cursor resource missing: " + resourceName + ". Re-download the exe.");
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    data = ms.ToArray();
                }
            }
            byte[] outData = resourceName.EndsWith(".ani") ? ScaleAni(data, size) : ScaleCur(data, size);
            File.WriteAllBytes(destPath, outData);
        }

        private byte[] ScaleCur(byte[] cur, int size)
        {
            int xhot = BitConverter.ToUInt16(cur, 10);
            int yhot = BitConverter.ToUInt16(cur, 12);

            byte[] dib = new byte[cur.Length - 22];
            Array.Copy(cur, 22, dib, 0, dib.Length);

            int oldW = BitConverter.ToInt32(dib, 4);
            int oldH = BitConverter.ToInt32(dib, 8) / 2;

            byte[] newDib = ScaleDib(dib, oldW, oldH, size);
            int newXhot = Clamp((int)Math.Round(xhot * ((double)size / oldW)), 0, size - 1);
            int newYhot = Clamp((int)Math.Round(yhot * ((double)size / oldH)), 0, size - 1);

            byte[] buf = new byte[22 + newDib.Length];
            buf[2] = 2; buf[3] = 0;
            buf[4] = 1; buf[5] = 0;
            buf[6] = (byte)(size < 256 ? size : 0);
            buf[7] = (byte)(size < 256 ? size : 0);
            buf[10] = (byte)(newXhot & 0xFF); buf[11] = (byte)(newXhot >> 8);
            buf[12] = (byte)(newYhot & 0xFF); buf[13] = (byte)(newYhot >> 8);
            BitConverter.GetBytes(newDib.Length).CopyTo(buf, 14);
            BitConverter.GetBytes(22).CopyTo(buf, 18);
            newDib.CopyTo(buf, 22);
            return buf;
        }

        private byte[] ScaleDib(byte[] dib, int oldW, int oldH, int newSize)
        {
            byte[] src = new byte[oldW * oldH * 4];
            Array.Copy(dib, 40, src, 0, src.Length);
            for (int i = 0; i < src.Length; i += 4)
            {
                int a = src[i + 3];
                if (a != 255)
                {
                    src[i]     = (byte)(src[i] * a / 255);
                    src[i + 1] = (byte)(src[i + 1] * a / 255);
                    src[i + 2] = (byte)(src[i + 2] * a / 255);
                }
            }

            byte[] dst = ResampleLanczos(src, oldW, oldH, newSize, newSize);

            for (int i = 0; i < dst.Length; i += 4)
            {
                int a = dst[i + 3];
                if (a == 255) continue;
                if (a > 0)
                {
                    dst[i]     = (byte)Math.Min(255, dst[i] * 255 / a);
                    dst[i + 1] = (byte)Math.Min(255, dst[i + 1] * 255 / a);
                    dst[i + 2] = (byte)Math.Min(255, dst[i + 2] * 255 / a);
                }
            }

            byte[] flipped = new byte[dst.Length];
            for (int y = 0; y < newSize; y++)
                Array.Copy(dst, y * newSize * 4, flipped, (newSize - 1 - y) * newSize * 4, newSize * 4);

            byte[] andMask = MakeAndMask(flipped, newSize);

            byte[] bmih = new byte[40];
            BitConverter.GetBytes(40).CopyTo(bmih, 0);
            BitConverter.GetBytes(newSize).CopyTo(bmih, 4);
            BitConverter.GetBytes(newSize * 2).CopyTo(bmih, 8);
            BitConverter.GetBytes((short)1).CopyTo(bmih, 12);
            BitConverter.GetBytes((short)32).CopyTo(bmih, 14);
            BitConverter.GetBytes(dst.Length + andMask.Length).CopyTo(bmih, 20);

            byte[] outDib = new byte[40 + dst.Length + andMask.Length];
            bmih.CopyTo(outDib, 0);
            dst.CopyTo(outDib, 40);
            andMask.CopyTo(outDib, 40 + dst.Length);
            return outDib;
        }

        private byte[] MakeAndMask(byte[] bgra, int size)
        {
            int rowBytes = (size + 7) / 8;
            byte[] mask = new byte[rowBytes * size];
            for (int y = 0; y < size; y++)
            {
                int row = 0;
                for (int x = 0; x < size; x++)
                {
                    if (bgra[(y * size + x) * 4 + 3] < 128)
                        row |= (0x80 >> (x % 8));
                    if (x % 8 == 7)
                    {
                        mask[y * rowBytes + (x / 8)] = (byte)row;
                        row = 0;
                    }
                }
                if (size % 8 != 0)
                    mask[y * rowBytes + (size / 8)] = (byte)row;
            }
            return mask;
        }

        private byte[] ScaleAni(byte[] ani, int size)
        {
            byte[] anih = null, seq = null, rate = null;
            var icons = new System.Collections.Generic.List<byte[]>();

            int pos = 12;
            while (pos + 8 <= ani.Length)
            {
                string fourcc = System.Text.Encoding.ASCII.GetString(ani, pos, 4);
                int len = BitConverter.ToInt32(ani, pos + 4);
                if (fourcc == "anih")
                {
                    anih = new byte[len];
                    Array.Copy(ani, pos + 8, anih, 0, len);
                }
                else if (fourcc == "LIST")
                {
                    string lstType = System.Text.Encoding.ASCII.GetString(ani, pos + 8, 4);
                    if (lstType == "fram")
                    {
                        int inner = pos + 12;
                        int innerEnd = pos + 8 + len;
                        while (inner + 8 <= innerEnd)
                        {
                            string c4 = System.Text.Encoding.ASCII.GetString(ani, inner, 4);
                            int clen = BitConverter.ToInt32(ani, inner + 4);
                            if (c4 == "icon")
                            {
                                byte[] icon = new byte[clen];
                                Array.Copy(ani, inner + 8, icon, 0, clen);
                                icons.Add(icon);
                            }
                            inner += 8 + clen + (clen % 2);
                        }
                    }
                }
                else if (fourcc == "seq ")
                {
                    seq = new byte[len];
                    Array.Copy(ani, pos + 8, seq, 0, len);
                }
                else if (fourcc == "rate")
                {
                    rate = new byte[len];
                    Array.Copy(ani, pos + 8, rate, 0, len);
                }
                pos += 8 + len + (len % 2);
            }

            byte[] newAnih = (byte[])anih.Clone();
            if (newAnih.Length >= 36)
            {
                BitConverter.GetBytes(size).CopyTo(newAnih, 12);
                BitConverter.GetBytes(size).CopyTo(newAnih, 16);
                BitConverter.GetBytes(32).CopyTo(newAnih, 20);
                BitConverter.GetBytes(1).CopyTo(newAnih, 24);
            }

            var content = new System.Collections.Generic.List<byte[]>();
            content.Add(RiffChunk("anih", newAnih));
            var framBody = new System.Collections.Generic.List<byte[]>();
            foreach (var icon in icons)
                framBody.Add(RiffChunk("icon", ScaleCur(icon, size)));
            content.Add(RiffList("fram", framBody.ToArray()));
            content.Add(RiffChunk("seq ", seq));
            content.Add(RiffChunk("rate", rate));

            var outBytes = new System.Collections.Generic.List<byte[]>();
            outBytes.Add(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            outBytes.Add(BitConverter.GetBytes(SumLength(content) + 4));
            outBytes.Add(System.Text.Encoding.ASCII.GetBytes("ACON"));
            outBytes.AddRange(content);
            return ConcatAll(outBytes);
        }

        private static byte[] RiffChunk(string fourcc, byte[] data)
        {
            byte[] c = new byte[8 + data.Length + (data.Length % 2)];
            System.Text.Encoding.ASCII.GetBytes(fourcc, 0, 4, c, 0);
            BitConverter.GetBytes(data.Length).CopyTo(c, 4);
            data.CopyTo(c, 8);
            if (data.Length % 2 == 1) c[8 + data.Length] = 0;
            return c;
        }

        private static byte[] RiffList(string fourcc, byte[][] chunks)
        {
            var inner = new System.Collections.Generic.List<byte[]>();
            inner.Add(System.Text.Encoding.ASCII.GetBytes(fourcc));
            inner.AddRange(chunks);
            byte[] body = ConcatAll(inner);
            byte[] c = new byte[8 + body.Length + (body.Length % 2)];
            System.Text.Encoding.ASCII.GetBytes("LIST", 0, 4, c, 0);
            BitConverter.GetBytes(body.Length).CopyTo(c, 4);
            body.CopyTo(c, 8);
            if (body.Length % 2 == 1) c[8 + body.Length] = 0;
            return c;
        }

        private static byte[] ConcatAll(System.Collections.Generic.List<byte[]> parts)
        {
            int total = 0;
            foreach (var p in parts) total += p.Length;
            byte[] buf = new byte[total];
            int pos = 0;
            foreach (var p in parts)
            {
                p.CopyTo(buf, pos);
                pos += p.Length;
            }
            return buf;
        }

        private static int SumLength(System.Collections.Generic.List<byte[]> parts)
        {
            int total = 0;
            foreach (var p in parts) total += p.Length;
            return total;
        }

        private static double LanczosKernel(double x)
        {
            if (x == 0) return 1;
            double ax = Math.Abs(x);
            if (ax >= 3) return 0;
            double px = Math.PI * x;
            return 3 * Math.Sin(px) * Math.Sin(px / 3) / (px * px);
        }

        private static void BuildTaps(int dst, int src, out int[][] idx, out double[][] wt, out int[] cnt)
        {
            idx = new int[dst][];
            wt = new double[dst][];
            cnt = new int[dst];
            for (int d = 0; d < dst; d++)
            {
                double center = (d + 0.5) * src / dst - 0.5;
                int start = (int)Math.Ceiling(center - 3.0);
                int end = (int)Math.Floor(center + 3.0);
                int[] ii = new int[end - start + 1];
                double[] ww = new double[end - start + 1];
                int m = 0;
                double wsum = 0;
                for (int i = start; i <= end; i++)
                {
                    double w = LanczosKernel(center - i);
                    if (w < 0) w = 0;
                    if (i >= 0 && i <= src - 1)
                    {
                        ii[m] = i;
                        ww[m] = w;
                        wsum += w;
                        m++;
                    }
                }
                if (wsum != 0)
                    for (int t = 0; t < m; t++) ww[t] /= wsum;
                idx[d] = ii;
                wt[d] = ww;
                cnt[d] = m;
            }
        }

        private static void ClampPremultiplied(byte[] buf)
        {
            for (int i = 0; i < buf.Length; i += 4)
            {
                int a = (int)(buf[i + 3] + 0.5);
                if (a < 0) a = 0;
                else if (a > 255) a = 255;
                buf[i + 3] = (byte)a;
                for (int c = 0; c < 3; c++)
                {
                    int v = (int)(buf[i + c] + 0.5);
                    if (v < 0) v = 0;
                    else if (v > a) v = a;
                    buf[i + c] = (byte)v;
                }
            }
        }

        private static byte[] ResampleLanczos(byte[] src, int srcW, int srcH, int dstW, int dstH)
        {
            if (srcW == dstW && srcH == dstH) return (byte[])src.Clone();

            int[][] hIdx, vIdx;
            double[][] hWt, vWt;
            int[] hN, vN;
            BuildTaps(dstW, srcW, out hIdx, out hWt, out hN);
            BuildTaps(dstH, srcH, out vIdx, out vWt, out vN);

            byte[] tmp = new byte[dstW * srcH * 4];
            for (int y = 0; y < srcH; y++)
            {
                int sRow = y * srcW * 4;
                int tRow = y * dstW * 4;
                for (int x = 0; x < dstW; x++)
                {
                    int[] taps = hIdx[x];
                    double[] wts = hWt[x];
                    double b = 0, g = 0, r = 0, a = 0;
                    for (int t = 0; t < hN[x]; t++)
                    {
                        int p = sRow + taps[t] * 4;
                        double w = wts[t];
                        b += src[p] * w;
                        g += src[p + 1] * w;
                        r += src[p + 2] * w;
                        a += src[p + 3] * w;
                    }
                    int dp = tRow + x * 4;
                    tmp[dp]     = (byte)Clamp255(b + 0.5);
                    tmp[dp + 1] = (byte)Clamp255(g + 0.5);
                    tmp[dp + 2] = (byte)Clamp255(r + 0.5);
                    tmp[dp + 3] = (byte)Clamp255(a + 0.5);
                }
            }
            ClampPremultiplied(tmp);

            byte[] dst = new byte[dstW * dstH * 4];
            for (int y = 0; y < dstH; y++)
            {
                int[] taps = vIdx[y];
                double[] wts = vWt[y];
                int dRow = y * dstW * 4;
                for (int x = 0; x < dstW; x++)
                {
                    double b = 0, g = 0, r = 0, a = 0;
                    for (int t = 0; t < vN[y]; t++)
                    {
                        int p = taps[t] * dstW * 4 + x * 4;
                        double w = wts[t];
                        b += tmp[p] * w;
                        g += tmp[p + 1] * w;
                        r += tmp[p + 2] * w;
                        a += tmp[p + 3] * w;
                    }
                    int dp = dRow + x * 4;
                    dst[dp]     = (byte)Clamp255(b + 0.5);
                    dst[dp + 1] = (byte)Clamp255(g + 0.5);
                    dst[dp + 2] = (byte)Clamp255(r + 0.5);
                    dst[dp + 3] = (byte)Clamp255(a + 0.5);
                }
            }
            ClampPremultiplied(dst);
            return dst;
        }

        private static int Clamp255(double v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (int)v;
        }

        private static int Clamp(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }

        private void Uninstall(string installDir, string schemeName)
        {
            Log("Uninstall: installDir=" + installDir + " schemeName=" + schemeName);
            if (IsNumixActive(installDir)) RestoreDefault();

            using (var key = Registry.CurrentUser.OpenSubKey(SCHEMES_REG, true))
                if (key != null) key.DeleteValue(schemeName, false);

            if (Directory.Exists(installDir))
                Directory.Delete(installDir, true);
            Log("Uninstall: completed");
        }

        private void RestoreDefault()
        {
            Log("RestoreDefault: starting");
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG, true))
            {
                if (key == null)
                {
                    Log("RestoreDefault: FAILED to open registry key " + CURSORS_REG);
                    throw new Exception("Failed to open registry key: " + CURSORS_REG);
                }
                key.SetValue("Arrow",       DefArrow);
                key.SetValue("Help",        DefHelp);
                key.SetValue("AppStarting", DefAppStarting);
                key.SetValue("Wait",        DefWait);
                key.SetValue("Crosshair",   DefCrosshair);
                key.SetValue("IBeam",       DefIBeam);
                key.SetValue("NWPen",       DefNWPen);
                key.SetValue("No",          DefNo);
                key.SetValue("SizeNS",      DefSizeNS);
                key.SetValue("SizeWE",      DefSizeWE);
                key.SetValue("SizeNWSE",    DefSizeNWSE);
                key.SetValue("SizeNESW",    DefSizeNESW);
                key.SetValue("SizeAll",     DefSizeAll);
                key.SetValue("UpArrow",     DefUpArrow);
                key.SetValue("Hand",        DefHand);
                Log("RestoreDefault: registry values written (default aero cursors)");
            }
            bool ok = SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);
            int lastErr = Marshal.GetLastWin32Error();
            Log("RestoreDefault: SystemParametersInfo returned " + ok + " lastWin32Error=" + lastErr);
            if (!ok)
                throw new Exception(string.Format("Cursor theme was applied to the registry but could not be activated immediately (error {0}). Changes will take effect after restarting Explorer.", lastErr));
            RestorePointerSize();
        }

        private string[] EnsureCursorFilesExist(string installDir)
        {
            string[] required = new string[]
            {
                installDir + @"\default.cur",
                installDir + @"\help.cur",
                installDir + @"\progress.ani",
                installDir + @"\wait.ani",
                installDir + @"\crosshair.cur",
                installDir + @"\text.cur",
                installDir + @"\pencil.cur",
                installDir + @"\not-allowed.cur",
                installDir + @"\size_ver.cur",
                installDir + @"\size_hor.cur",
                installDir + @"\size_fdiag.cur",
                installDir + @"\size_bdiag.cur",
                installDir + @"\fleur.cur",
                installDir + @"\up-arrow.cur",
                installDir + @"\pointer.cur",
            };

            bool allExist = true;
            foreach (var path in required)
            {
                bool exists = File.Exists(path);
                if (!exists) allExist = false;
                Log("SetActiveCursor: file " + (exists ? "OK  " : "MISS") + " " + path);
            }
            Log("SetActiveCursor: all required files exist=" + allExist);

            if (!allExist)
            {
                string missing = "";
                foreach (var path in required)
                    if (!File.Exists(path)) missing += (missing.Length > 0 ? ", " : "") + Path.GetFileName(path);
                Log("SetActiveCursor: THROW missing files: " + missing);
                throw new DirectoryNotFoundException("Cursor files missing, cannot activate theme: " + missing + ". Reinstall the theme.");
            }
            return required;
        }

        private void SetActiveCursor(string installDir, int size)
        {
            Log("SetActiveCursor: installDir=" + installDir + " size=" + size);

            string[] required = EnsureCursorFilesExist(installDir);

            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG, true))
            {
                if (key == null)
                {
                    Log("SetActiveCursor: FAILED to open registry key " + CURSORS_REG);
                    throw new Exception("Failed to open registry key: " + CURSORS_REG);
                }
                string[] names = new string[] { "Arrow", "Help", "AppStarting", "Wait", "Crosshair", "IBeam", "NWPen", "No", "SizeNS", "SizeWE", "SizeNWSE", "SizeNESW", "SizeAll", "UpArrow", "Hand" };
                for (int i = 0; i < names.Length; i++)
                {
                    key.SetValue(names[i], required[i]);
                    Log("SetActiveCursor: registry " + names[i] + " = " + required[i]);
                }
            }

            Log("SetActiveCursor: calling SystemParametersInfo(SPI_SETCURSORS)...");
            bool ok = SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);
            int lastErr = Marshal.GetLastWin32Error();
            Log("SetActiveCursor: SystemParametersInfo returned " + ok + " lastWin32Error=" + lastErr);
            if (!ok)
                throw new Exception(string.Format("Cursor theme was applied to the registry but could not be activated immediately (error {0}). Changes will take effect after restarting Explorer.", lastErr));
            ApplyPointerSize(size);
        }

        private bool IsNumixActive(string installDir)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG))
            {
                if (key == null) return false;
                string val = key.GetValue("Arrow") as string;
                return val != null && val.StartsWith(installDir + "\\", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
