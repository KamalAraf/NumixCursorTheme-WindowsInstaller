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

        private const uint SPI_SETCURSORS     = 0x0057;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE    = 0x02;

        private const string CURSORS_REG = @"Control Panel\Cursors";
        private const string SCHEMES_REG = @"Control Panel\Cursors\Schemes";

        private static readonly string WinRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

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
        private Label lblStatus;
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
            Size = new Size(460, 480);
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
            cbSetActive.Location = new Point(15, 25);
            cbSetActive.Checked = true;
            cbSetActive.AutoSize = true;
            gbOptions.Controls.Add(cbSetActive);

            btnApply = new Button();
            btnApply.Text = "Apply";
            btnApply.Location = new Point(20, 340);
            btnApply.Size = new Size(120, 35);
            btnApply.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnApply.BackColor = Color.FromArgb(0, 120, 215);
            btnApply.ForeColor = Color.White;
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += OnApplyClick;

            lblStatus = new Label();
            lblStatus.Text = "Ready.";
            lblStatus.Location = new Point(20, 395);
            lblStatus.Size = new Size(400, 40);
            lblStatus.ForeColor = Color.Gray;

            Controls.AddRange(new Control[] { gbVariant, gbAction, gbOptions, btnApply, lblStatus });
            UpdateStatusLabel();
        }

        private void OnVariantChanged(object sender, EventArgs e)
        {
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (rbVariantLight.Checked)
                lblStatus.Text = "Selected: Numix Cursor Light";
            else
                lblStatus.Text = "Selected: Numix Cursor Dark";
        }

        private void OnActionChanged(object sender, EventArgs e)
        {
            cbSetActive.Enabled = rbInstall.Checked;
            cbSetActive.Checked = rbInstall.Checked;
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

                Log("=== Apply clicked: action=" + (rbInstall.Checked ? "Install" : rbUninstall.Checked ? "Uninstall" : rbSetActive.Checked ? "SetActive" : "Restore") +
                    " variant=" + (rbVariantLight.Checked ? "light" : "dark") +
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
                    await System.Threading.Tasks.Task.Run(() => Install(setActive, dir, installDir, schemeName, schemeValue));
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
                            await System.Threading.Tasks.Task.Run(() => Install(true, dir, installDir, schemeName, schemeValue));
                            MessageBox.Show("Operation completed successfully.", "Done",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            lblStatus.Text = "Ready.";
                        }
                        showSuccess = false;
                        return;
                    }
                    lblStatus.Text = "Setting active cursor...";
                    await System.Threading.Tasks.Task.Run(() => SetActiveCursor(installDir));
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

        private void Install(bool setActive, string cursorDirName, string installDir, string schemeName, string schemeValue)
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            if (exeDir == null) throw new DirectoryNotFoundException("Could not determine application directory.");

            string staticDir   = Path.Combine(exeDir, "cursors", cursorDirName, "static");
            string animatedDir = Path.Combine(exeDir, "cursors", cursorDirName, "animated");

            Log("Install: exeDir=" + exeDir + " staticDir=" + staticDir + " animatedDir=" + animatedDir);
            Log("Install: staticDir exists=" + Directory.Exists(staticDir) + " animatedDir exists=" + Directory.Exists(animatedDir));

            if (!Directory.Exists(staticDir))
                throw new DirectoryNotFoundException("Cursor files not found in 'cursors/" + cursorDirName + "/static/'.");

            Directory.CreateDirectory(installDir);
            Log("Install: created/verified installDir=" + installDir);

            foreach (var file in Directory.EnumerateFiles(staticDir, "*.cur"))
            {
                File.Copy(file, Path.Combine(installDir, Path.GetFileName(file)), true);
                Log("Install: copied " + Path.GetFileName(file) + " -> " + installDir);
            }

            if (Directory.Exists(animatedDir))
            {
                foreach (var file in Directory.EnumerateFiles(animatedDir, "*.ani"))
                {
                    File.Copy(file, Path.Combine(installDir, Path.GetFileName(file)), true);
                    Log("Install: copied " + Path.GetFileName(file) + " -> " + installDir);
                }
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

            if (setActive) SetActiveCursor(installDir);
            Log("Install: completed");
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

        private void SetActiveCursor(string installDir)
        {
            Log("SetActiveCursor: installDir=" + installDir);

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
