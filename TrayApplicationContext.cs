using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VibeShot
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ScreenCaptureManager captureManager;

        public TrayApplicationContext()
        {
            // Initialize the capture manager
            captureManager = new ScreenCaptureManager();

            // Get the icon file path
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VibeShot.ico");
            Icon appIcon = File.Exists(iconPath) 
                ? new Icon(iconPath) 
                : SystemIcons.Application; // Fallback to system icon if not found

            // Initialize tray icon
            trayIcon = new NotifyIcon()
            {
                Icon = appIcon,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "VibeShot"
            };

            // Add menu items
            trayIcon.ContextMenuStrip.Items.Add("Capture Screenshot", null, CaptureScreenshot_Click);
            trayIcon.ContextMenuStrip.Items.Add("-"); // Separator
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, Exit_Click);

            // Register global hotkey
            HotkeyManager.RegisterHotKey(Keys.PrintScreen, KeyModifiers.Control, () => 
            {
                CaptureScreenshot_Click(this, EventArgs.Empty);
            });

            trayIcon.DoubleClick += CaptureScreenshot_Click;
        }

        private void CaptureScreenshot_Click(object? sender, EventArgs e)
        {
            captureManager.StartCapture();
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            // Unregister hotkey
            HotkeyManager.UnregisterAllHotKeys();
            
            // Hide tray icon before closing
            trayIcon.Visible = false;
            
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
