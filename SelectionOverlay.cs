using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VibeShot
{
    public class SelectionOverlay : Form
    {
        public event EventHandler<Rectangle>? SelectionCompleted;

        private Point startPoint;
        private Point currentPoint;
        private bool isSelecting = false;
        private Bitmap backgroundImage;
        private Color overlayColor = Color.FromArgb(120, 0, 0, 0);
        private Color rulerColor = Color.FromArgb(255, 0, 120, 215);

        public SelectionOverlay(Bitmap background)
        {
            backgroundImage = background;

            // Set up form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal; // Changed from Maximized
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            
            // Set form bounds to cover all screens
            Rectangle bounds = GetAllScreensBounds();
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(bounds.X, bounds.Y); // Explicitly use bounds location
            this.Size = new Size(bounds.Width, bounds.Height);

            // Handle keyboard events
            this.KeyDown += SelectionOverlay_KeyDown;

            // Handle mouse events
            this.MouseDown += SelectionOverlay_MouseDown;
            this.MouseMove += SelectionOverlay_MouseMove;
            this.MouseUp += SelectionOverlay_MouseUp;

            // Enable double buffering for smooth drawing
            this.DoubleBuffered = true;
        }

        private Rectangle GetAllScreensBounds()
        {
            // Start with an empty rectangle
            Rectangle bounds = Rectangle.Empty;
            
            // Combine all screen bounds
            foreach (Screen screen in Screen.AllScreens)
            {
                if (bounds == Rectangle.Empty)
                {
                    // Initialize with the first screen
                    bounds = screen.Bounds;
                }
                else
                {
                    // Union with each additional screen
                    bounds = Rectangle.Union(bounds, screen.Bounds);
                }
            }
            
            return bounds;
        }

        private void SelectionOverlay_KeyDown(object? sender, KeyEventArgs e)
        {
            // Escape key cancels the selection
            if (e.KeyCode == Keys.Escape)
            {
                SelectionCompleted?.Invoke(this, Rectangle.Empty);
                this.Close();
            }
        }

        private void SelectionOverlay_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location;
                currentPoint = e.Location;
                isSelecting = true;
            }
        }

        private void SelectionOverlay_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                currentPoint = e.Location;
                this.Invalidate();
            }
        }

        private void SelectionOverlay_MouseUp(object? sender, MouseEventArgs e)
        {
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                
                // Calculate the selection rectangle
                Rectangle selection = GetSelectionRectangle();
                
                // Invoke the event to notify completion
                SelectionCompleted?.Invoke(this, selection);
                
                this.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            
            // Get proper bounds for multi-monitor
            Rectangle bounds = GetAllScreensBounds();
            
            // Calculate offset for drawing the background image
            int offsetX = -bounds.X;
            int offsetY = -bounds.Y;
            
            // Draw the background image at the correct position
            if (backgroundImage != null)
            {
                g.DrawImage(backgroundImage, offsetX, offsetY);
            }
            
            // Draw semi-transparent overlay
            using (SolidBrush brush = new SolidBrush(overlayColor))
            {
                g.FillRectangle(brush, this.ClientRectangle);
            }

            if (isSelecting)
            {
                // Calculate selection rectangle
                Rectangle selection = GetSelectionRectangle();
                
                // FIXED: Draw the background image in the selected area without overlay
                g.SetClip(selection);
                if (backgroundImage != null)
                {
                    // Draw just the selected portion of the background image without overlay
                    g.DrawImage(backgroundImage, offsetX, offsetY);
                }
                g.ResetClip();
                
                // Draw selection border
                using (Pen pen = new Pen(Color.White, 1))
                {
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(pen, selection);
                }
                
                // Draw horizontal and vertical rulers
                DrawRulers(g, currentPoint);
            }
        }

        private Rectangle GetSelectionRectangle()
        {
            int x = Math.Min(startPoint.X, currentPoint.X);
            int y = Math.Min(startPoint.Y, currentPoint.Y);
            int width = Math.Abs(currentPoint.X - startPoint.X);
            int height = Math.Abs(currentPoint.Y - startPoint.Y);
            
            return new Rectangle(x, y, width, height);
        }

        private void DrawRulers(Graphics g, Point position)
        {
            using (Pen pen = new Pen(rulerColor, 1))
            {
                // Draw horizontal ruler
                g.DrawLine(pen, 0, position.Y, this.Width, position.Y);
                
                // Draw vertical ruler
                g.DrawLine(pen, position.X, 0, position.X, this.Height);
            }
            
            // Draw position information
            string text = $"X: {position.X}, Y: {position.Y}";
            Rectangle selection = GetSelectionRectangle();
            if (selection.Width > 0 && selection.Height > 0)
            {
                text += $" ({selection.Width}x{selection.Height})";
            }
            
            using (Font font = new Font("Arial", 9))
            using (SolidBrush brush = new SolidBrush(Color.White))
            using (SolidBrush shadowBrush = new SolidBrush(Color.Black))
            {
                // Position the text near the cursor but ensure it's visible
                Point textPosition = new Point(position.X + 10, position.Y + 10);
                if (textPosition.X + 150 > this.Width)
                    textPosition.X = position.X - 160;
                if (textPosition.Y + 20 > this.Height)
                    textPosition.Y = position.Y - 30;
                
                // Draw text shadow for better visibility
                g.DrawString(text, font, shadowBrush, textPosition.X + 1, textPosition.Y + 1);
                g.DrawString(text, font, brush, textPosition);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
