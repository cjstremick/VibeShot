using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibeShot
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }

    public static class HotkeyManager
    {
        private static int currentId;
        private static Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static readonly HotkeyMessageWindow messageWindow = new HotkeyMessageWindow();

        public static bool RegisterHotKey(Keys key, KeyModifiers modifiers, Action action)
        {
            currentId++;
            bool registered = RegisterHotKey(messageWindow.Handle, currentId, (uint)modifiers, (uint)key);
            if (registered)
            {
                hotkeyActions[currentId] = action;
            }
            return registered;
        }

        public static void UnregisterAllHotKeys()
        {
            foreach (var id in hotkeyActions.Keys)
            {
                UnregisterHotKey(messageWindow.Handle, id);
            }
            hotkeyActions.Clear();
        }

        private class HotkeyMessageWindow : Form
        {
            private const int WM_HOTKEY = 0x0312;

            public HotkeyMessageWindow()
            {
                // Initialize invisible window to receive hotkey messages
                this.Width = 0;
                this.Height = 0;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Visible = true;
                this.Opacity = 0;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    int id = m.WParam.ToInt32();
                    if (hotkeyActions.TryGetValue(id, out Action? action) && action != null)
                    {
                        action.Invoke();
                    }
                }
                base.WndProc(ref m);
            }
        }
    }
}
