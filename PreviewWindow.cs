using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot
{
    public class PreviewWindow : Form
    {
        private Bitmap capturedImage;
        private PictureBox imageBox = null!;

        public PreviewWindow(Bitmap image)
        {
            capturedImage = image ?? throw new ArgumentNullException(nameof(image));
            InitializeComponents();
            
            // Set reasonable initial window size - Fix for CS8602 warning
            Screen primaryScreen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            int maxWidth = Math.Min(capturedImage.Width + 40, primaryScreen.WorkingArea.Width - 100);
            int maxHeight = Math.Min(capturedImage.Height + 80, primaryScreen.WorkingArea.Height - 100);
            this.ClientSize = new Size(maxWidth, maxHeight);
            
            // Center on screen
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponents()
        {
            this.Text = "VibeShot - Screenshot Preview";
            this.Icon = SystemIcons.Application; // Replace with your custom icon
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.KeyPreview = true;
            
            // Create menu strip with options
            MenuStrip menuStrip = new MenuStrip();
            
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Save As...", null, SaveAs_Click);
            menuStrip.Items.Add(fileMenu);
            
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit");
            editMenu.DropDownItems.Add("Copy", null, Copy_Click);
            menuStrip.Items.Add(editMenu);
            
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            
            // Create picture box to display the image
            imageBox = new PictureBox();
            imageBox.Dock = DockStyle.Fill;
            imageBox.SizeMode = PictureBoxSizeMode.AutoSize;
            imageBox.Image = capturedImage;
            
            // Add picture box to panel with scrollbars
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.Controls.Add(imageBox);
            
            // Add panel to form
            this.Controls.Add(panel);
            
            // Add status strip with image info
            StatusStrip statusStrip = new StatusStrip();
            statusStrip.Items.Add($"Image size: {capturedImage.Width} x {capturedImage.Height}");
            statusStrip.Items.Add("Press Ctrl+C to copy to clipboard");
            this.Controls.Add(statusStrip);
            
            // Set up event handlers
            this.KeyDown += PreviewWindow_KeyDown;
            this.FormClosing += PreviewWindow_FormClosing;
        }

        private void PreviewWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            // Handle Ctrl+C to copy to clipboard
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyToClipboard();
                e.Handled = true;
            }
        }

        private void Copy_Click(object? sender, EventArgs e)
        {
            CopyToClipboard();
        }

        private void SaveAs_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                saveDialog.DefaultExt = "png";
                saveDialog.FileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                    System.Drawing.Imaging.ImageFormat format;
                    
                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            format = System.Drawing.Imaging.ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = System.Drawing.Imaging.ImageFormat.Bmp;
                            break;
                        default:
                            format = System.Drawing.Imaging.ImageFormat.Png;
                            break;
                    }
                    
                    capturedImage.Save(saveDialog.FileName, format);
                }
            }
        }

        private void CopyToClipboard()
        {
            try
            {
                Clipboard.SetImage(capturedImage);
                ShowCopyConfirmation();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to copy image to clipboard: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowCopyConfirmation()
        {
            // Create a temporary label for feedback
            Label label = new Label
            {
                Text = "✓ Copied to clipboard",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 0, 120, 0),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30,
                Width = 200
            };

            this.Controls.Add(label);
            label.BringToFront();
            
            // Position at bottom center
            label.Left = (this.ClientSize.Width - label.Width) / 2;
            label.Top = this.ClientSize.Height - label.Height - 50;

            // Create a timer to hide the label
            Timer timer = new Timer { Interval = 1500 };
            timer.Tick += (s, e) => {
                this.Controls.Remove(label);
                label.Dispose();
                timer.Dispose();
            };
            timer.Start();
        }

        private void PreviewWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (imageBox != null)
            {
                imageBox.Image = null;
            }
            capturedImage?.Dispose();
        }
    }
}
