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

        private const string SCHEME_NAME = "Numix-Dark";
        private const string INSTALL_DIR = @"C:\Windows\Cursors\Numix-Dark";
        private const string CURSORS_REG = @"Control Panel\Cursors";
        private const string SCHEMES_REG = @"Control Panel\Cursors\Schemes";

        private static readonly string WinRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private static readonly string SchemeValue;

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
        private CheckBox cbSetActive;
        private Label lblStatus;
        private Button btnApply;

        static MainForm()
        {
            SchemeValue = string.Format(
                @"{0}\default.cur,{0}\help.cur,{0}\progress.ani,{0}\wait.ani,{0}\crosshair.cur,{0}\text.cur,{0}\pencil.cur,{0}\not-allowed.cur,{0}\size_ver.cur,{0}\size_hor.cur,{0}\size_fdiag.cur,{0}\size_bdiag.cur,{0}\fleur.cur,{0}\up-arrow.cur,{0}\pointer.cur",
                INSTALL_DIR
            );
        }

        public MainForm()
        {
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
            catch { /* default icon */ }
        }

        private void InitializeComponent()
        {
            Text = "Numix Cursors Manager";
            Size = new Size(460, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);

            var gbAction = new GroupBox { Text = "Action", Location = new Point(20, 20), Size = new Size(400, 145) };

            rbInstall   = new RadioButton { Text = "Install Numix Dark",              Location = new Point(15, 25),  Checked = true, AutoSize = true };
            rbUninstall = new RadioButton { Text = "Uninstall Numix Dark",            Location = new Point(15, 50),  AutoSize = true };
            rbSetActive = new RadioButton { Text = "Set Numix Dark (Activate Theme)", Location = new Point(15, 75),  AutoSize = true };
            rbRestore   = new RadioButton { Text = "Restore Windows Default",         Location = new Point(15, 100), AutoSize = true };

            rbInstall.CheckedChanged += OnActionChanged;

            gbAction.Controls.AddRange(new Control[] { rbInstall, rbUninstall, rbSetActive, rbRestore });

            var gbOptions = new GroupBox { Text = "Options", Location = new Point(20, 180), Size = new Size(400, 60) };
            cbSetActive = new CheckBox { Text = "Set as active cursor immediately", Location = new Point(15, 25), Checked = true, AutoSize = true };
            gbOptions.Controls.Add(cbSetActive);

            btnApply = new Button
            {
                Text      = "Apply",
                Location  = new Point(20, 255),
                Size      = new Size(120, 35),
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += async (s, e) =>
            {
                btnApply.Enabled = false;
                UseWaitCursor    = true;
                lblStatus.Text   = "Processing...";
                try
                {
                    if (rbInstall.Checked)
                    {
                        if (Directory.Exists(INSTALL_DIR) &&
                            MessageBox.Show("Numix Dark is already installed. Reinstall?", "Confirm",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            ResetUI();
                            return;
                        }
                        lblStatus.Text = "Installing...";
                        await System.Threading.Tasks.Task.Run(() => Install(cbSetActive.Checked));
                    }
                    else if (rbUninstall.Checked)
                    {
                        if (!Directory.Exists(INSTALL_DIR))
                        {
                            MessageBox.Show("Numix Dark is not installed.", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ResetUI();
                            return;
                        }
                        lblStatus.Text = "Uninstalling...";
                        await System.Threading.Tasks.Task.Run(() => Uninstall());
                    }
                    else if (rbSetActive.Checked)
                    {
                        if (!Directory.Exists(INSTALL_DIR))
                        {
                            var result = MessageBox.Show("Numix Dark is not installed. Install now?", "Not Found",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes)
                            {
                                lblStatus.Text = "Installing...";
                                await System.Threading.Tasks.Task.Run(() => Install(true));
                            }
                            ResetUI();
                            return;
                        }
                        lblStatus.Text = "Setting active cursor...";
                        await System.Threading.Tasks.Task.Run(() => SetActiveCursor());
                    }
                    else if (rbRestore.Checked)
                    {
                        if (IsDefaultActive())
                        {
                            MessageBox.Show("Windows default cursor theme is already active.", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ResetUI();
                            return;
                        }
                        lblStatus.Text = "Restoring default...";
                        await System.Threading.Tasks.Task.Run(() => RestoreDefault());
                    }

                    MessageBox.Show("Operation completed successfully.", "Done",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lblStatus.Text = "Ready.";
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Access denied. Run as Administrator.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Error: Access denied.";
                }
                catch (DirectoryNotFoundException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Error: Files missing.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Unexpected error:\n{0}", ex.Message), "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "Error occurred.";
                }
                finally
                {
                    ResetUI();
                }
            };

            lblStatus = new Label
            {
                Text      = "Ready.",
                Location  = new Point(20, 310),
                Size      = new Size(400, 40),
                ForeColor = Color.Gray
            };

            Controls.AddRange(new Control[] { gbAction, gbOptions, btnApply, lblStatus });
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
            lblStatus.Text   = "Ready.";
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

        private void Install(bool setActive)
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            if (exeDir == null) throw new DirectoryNotFoundException("Could not determine application directory.");

            string staticDir   = Path.Combine(exeDir, "cursors", "static");
            string animatedDir = Path.Combine(exeDir, "cursors", "animated");

            if (!Directory.Exists(staticDir))
                throw new DirectoryNotFoundException("Static cursor files not found in 'cursors/static/'.");

            Directory.CreateDirectory(INSTALL_DIR);

            foreach (var file in Directory.EnumerateFiles(staticDir, "*.cur"))
                File.Copy(file, Path.Combine(INSTALL_DIR, Path.GetFileName(file)), true);

            if (Directory.Exists(animatedDir))
            {
                foreach (var file in Directory.EnumerateFiles(animatedDir, "*.ani"))
                    File.Copy(file, Path.Combine(INSTALL_DIR, Path.GetFileName(file)), true);
            }

            using (var key = Registry.CurrentUser.CreateSubKey(SCHEMES_REG))
                key.SetValue(SCHEME_NAME, SchemeValue);

            if (setActive) SetActiveCursor();
        }

        private void Uninstall()
        {
            if (IsNumixActive()) RestoreDefault();

            using (var key = Registry.CurrentUser.OpenSubKey(SCHEMES_REG, true))
                if (key != null) key.DeleteValue(SCHEME_NAME, false);

            if (Directory.Exists(INSTALL_DIR))
                Directory.Delete(INSTALL_DIR, true);
        }

        private void RestoreDefault()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG, true))
            {
                if (key == null) return;
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
            }
            if (!SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
                throw new Exception("Cursor theme was applied to the registry but could not be activated immediately. Changes will take effect after restarting Explorer.");
        }

        private void SetActiveCursor()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG, true))
            {
                if (key == null) return;
                key.SetValue("Arrow",       INSTALL_DIR + @"\default.cur");
                key.SetValue("Help",        INSTALL_DIR + @"\help.cur");
                key.SetValue("AppStarting", INSTALL_DIR + @"\progress.ani");
                key.SetValue("Wait",        INSTALL_DIR + @"\wait.ani");
                key.SetValue("Crosshair",   INSTALL_DIR + @"\crosshair.cur");
                key.SetValue("IBeam",       INSTALL_DIR + @"\text.cur");
                key.SetValue("NWPen",       INSTALL_DIR + @"\pencil.cur");
                key.SetValue("No",          INSTALL_DIR + @"\not-allowed.cur");
                key.SetValue("SizeNS",      INSTALL_DIR + @"\size_ver.cur");
                key.SetValue("SizeWE",      INSTALL_DIR + @"\size_hor.cur");
                key.SetValue("SizeNWSE",    INSTALL_DIR + @"\size_fdiag.cur");
                key.SetValue("SizeNESW",    INSTALL_DIR + @"\size_bdiag.cur");
                key.SetValue("SizeAll",     INSTALL_DIR + @"\fleur.cur");
                key.SetValue("UpArrow",     INSTALL_DIR + @"\up-arrow.cur");
                key.SetValue("Hand",        INSTALL_DIR + @"\pointer.cur");
            }
            if (!SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
                throw new Exception("Cursor theme was applied to the registry but could not be activated immediately. Changes will take effect after restarting Explorer.");
        }

        private bool IsNumixActive()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(CURSORS_REG))
            {
                if (key == null) return false;
                string val = key.GetValue("Arrow") as string;
                return val != null && val.StartsWith(INSTALL_DIR, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
