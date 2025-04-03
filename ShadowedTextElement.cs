using System;
using System.Drawing;
using System.Drawing.Text;

namespace VibeShot
{
    public class ShadowedTextElement : DrawableElement
    {
        public string Text { get; set; }
        public Point Location { get; set; }
        public Color Color { get; set; }
        public Font Font { get; set; }
        
        // Shadow properties
        public Color ShadowColor { get; set; } = Color.FromArgb(128, 0, 0, 0);
        public int ShadowOffset { get; set; } = 2;
        
        public ShadowedTextElement(string text, Point location, Color color, Font font)
        {
            Text = text;
            Location = location;
            Color = color;
            Font = font;
            UpdateBounds();
        }
        
        private void UpdateBounds()
        {
            // Measure text size
            using (Bitmap temp = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(temp))
            {
                // Calculate bounding box for potentially multi-line text
                string[] lines = Text.Split('\n');
                float maxWidth = 0;
                float totalHeight = 0;
                
                foreach (string line in lines)
                {
                    SizeF lineSize = g.MeasureString(line, Font);
                    maxWidth = Math.Max(maxWidth, lineSize.Width);
                    totalHeight += lineSize.Height;
                }
                
                // Add some padding plus room for the shadow
                Bounds = new Rectangle(
                    Location, 
                    new Size((int)maxWidth + 8 + ShadowOffset, 
                             (int)totalHeight + 8 + ShadowOffset));
            }
        }
        
        public override void Draw(Graphics g)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            
            // Create a rectangle for text layout
            RectangleF textRect = new RectangleF(
                Location.X, Location.Y, Bounds.Width, Bounds.Height);
                
            // Draw shadow text first
            using (SolidBrush shadowBrush = new SolidBrush(ShadowColor))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                
                // Draw the shadow offset from the main text
                g.DrawString(
                    Text, 
                    Font, 
                    shadowBrush, 
                    new RectangleF(
                        textRect.X + ShadowOffset, 
                        textRect.Y + ShadowOffset, 
                        textRect.Width, 
                        textRect.Height), 
                    sf);
            }
            
            // Draw main text on top
            using (SolidBrush textBrush = new SolidBrush(Color))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                g.DrawString(Text, Font, textBrush, textRect, sf);
            }
            
            DrawSelectionHandles(g);
        }
        
        public override bool Contains(Point point)
        {
            return Bounds.Contains(point);
        }
        
        public override void Move(int deltaX, int deltaY)
        {
            Location = new Point(Location.X + deltaX, Location.Y + deltaY);
            UpdateBounds();
        }
    }
}
