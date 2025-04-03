using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace VibeShot
{
    // Arrow element
    public class ArrowElement : DrawableElement
    {
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; }
        
        public ArrowElement(Point start, Point end, Color color, int size)
        {
            StartPoint = start;
            EndPoint = end;
            Color = color;
            Size = size;
            UpdateBounds();
        }
        
        private void UpdateBounds()
        {
            int left = Math.Min(StartPoint.X, EndPoint.X);
            int top = Math.Min(StartPoint.Y, EndPoint.Y);
            int right = Math.Max(StartPoint.X, EndPoint.X);
            int bottom = Math.Max(StartPoint.Y, EndPoint.Y);
            
            // Add some padding for the arrow head
            int padding = Size * 3;
            Bounds = Rectangle.FromLTRB(left - padding, top - padding, right + padding, bottom + padding);
        }
        
        public override void Draw(Graphics g)
        {
            using (Pen pen = new Pen(Color, Size))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5f, 5f);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawLine(pen, StartPoint, EndPoint);
            }
            
            DrawSelectionHandles(g);
        }
        
        public override bool Contains(Point point)
        {
            // Line hit testing - check if point is near the line
            using (Pen pen = new Pen(Color.Black, Size + 6)) // Use wider pen for easier selection
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddLine(StartPoint, EndPoint);
                    return path.IsOutlineVisible(point, pen);
                }
            }
        }
        
        public override void Move(int deltaX, int deltaY)
        {
            StartPoint = new Point(StartPoint.X + deltaX, StartPoint.Y + deltaY);
            EndPoint = new Point(EndPoint.X + deltaX, EndPoint.Y + deltaY);
            UpdateBounds();
        }
    }
    
    // Rectangle element with rounded corners
    public class RectangleElement : DrawableElement
    {
        public Rectangle Rect { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; }
        public int CornerRadius { get; set; } = 10; // Default corner radius
        
        public RectangleElement(Rectangle rect, Color color, int size)
        {
            Rect = rect;
            Color = color;
            Size = size;
            Bounds = new Rectangle(rect.X - size, rect.Y - size, 
                rect.Width + size * 2, rect.Height + size * 2);
        }
        
        public override void Draw(Graphics g)
        {
            using (Pen pen = new Pen(Color, Size))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw rounded rectangle instead of regular rectangle
                DrawRoundedRectangle(g, pen, Rect, CornerRadius);
            }
            
            DrawSelectionHandles(g);
        }
        
        // Helper method to draw a rounded rectangle
        private void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            // Make sure the radius isn't too large for the rectangle
            int effectiveRadius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            
            if (effectiveRadius <= 1 || rect.Width <= 1 || rect.Height <= 1)
            {
                // Fall back to regular rectangle for very small sizes
                g.DrawRectangle(pen, rect);
                return;
            }
            
            // Create a rounded rectangle path
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, effectiveRadius * 2, effectiveRadius * 2, 180, 90);
                path.AddArc(rect.X + rect.Width - effectiveRadius * 2, rect.Y, effectiveRadius * 2, effectiveRadius * 2, 270, 90);
                path.AddArc(rect.X + rect.Width - effectiveRadius * 2, rect.Y + rect.Height - effectiveRadius * 2, effectiveRadius * 2, effectiveRadius * 2, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - effectiveRadius * 2, effectiveRadius * 2, effectiveRadius * 2, 90, 90);
                path.CloseFigure();
                
                g.DrawPath(pen, path);
            }
        }
        
        public override bool Contains(Point point)
        {
            // Check if point is near any of the rectangle's edges
            using (Pen pen = new Pen(Color.Black, Size + 6))
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    // Create a path for the rounded rectangle to improve hit testing
                    int radius = Math.Min(CornerRadius, Math.Min(Rect.Width / 2, Rect.Height / 2));
                    
                    if (radius <= 1)
                    {
                        path.AddRectangle(Rect);
                    }
                    else 
                    {
                        path.AddArc(Rect.X, Rect.Y, radius * 2, radius * 2, 180, 90);
                        path.AddArc(Rect.X + Rect.Width - radius * 2, Rect.Y, radius * 2, radius * 2, 270, 90);
                        path.AddArc(Rect.X + Rect.Width - radius * 2, Rect.Y + Rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                        path.AddArc(Rect.X, Rect.Y + Rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                        path.CloseFigure();
                    }
                    
                    return path.IsOutlineVisible(point, pen);
                }
            }
        }
        
        public override void Move(int deltaX, int deltaY)
        {
            Rect = new Rectangle(Rect.X + deltaX, Rect.Y + deltaY, Rect.Width, Rect.Height);
            Bounds = new Rectangle(Rect.X - Size, Rect.Y - Size, 
                Rect.Width + Size * 2, Rect.Height + Size * 2);
        }
    }
    
    // Text element
    public class TextElement : DrawableElement
    {
        public string Text { get; set; }
        public Point Location { get; set; }
        public Color Color { get; set; }
        public Font Font { get; set; }
        
        public TextElement(string text, Point location, Color color, Font font)
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
                
                // Add some padding
                Bounds = new Rectangle(Location, new Size((int)maxWidth + 8, (int)totalHeight + 8));
            }
        }
        
        public override void Draw(Graphics g)
        {
            using (SolidBrush brush = new SolidBrush(Color))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                
                // Drawing with better formatting for multi-line text
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Near;
                    sf.LineAlignment = StringAlignment.Near;
                    g.DrawString(Text, Font, brush, new RectangleF(
                        Location.X, Location.Y, Bounds.Width, Bounds.Height), sf);
                }
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
    
    // Number stamp element
    public class NumberStampElement : DrawableElement
    {
        public int Number { get; set; }
        public Point Center { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; } = 26; // Default size
        
        public NumberStampElement(int number, Point center, Color color)
        {
            Number = number;
            Center = center;
            Color = color;
            UpdateBounds();
        }
        
        private void UpdateBounds()
        {
            // Calculate bounds based on circle size
            Bounds = new Rectangle(Center.X - Size / 2, Center.Y - Size / 2, Size, Size);
        }
        
        public override void Draw(Graphics g)
        {
            string numberText = Number.ToString();
            
            // Draw shadow
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle shadowRect = new Rectangle(Bounds.X + 2, Bounds.Y + 2, Bounds.Width, Bounds.Height);
                g.FillEllipse(shadowBrush, shadowRect);
            }
            
            // Draw circle
            using (SolidBrush circleBrush = new SolidBrush(Color))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(circleBrush, Bounds);
            }
            
            // Draw number
            using (Font font = new Font("Arial", 12, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                StringFormat stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
                };
                
                RectangleF textRect = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                
                // Draw text shadow
                using (SolidBrush textShadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.DrawString(numberText, font, textShadowBrush, 
                        new RectangleF(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height), 
                        stringFormat);
                }
                
                // Draw text
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString(numberText, font, textBrush, textRect, stringFormat);
            }
            
            DrawSelectionHandles(g);
        }
        
        public override bool Contains(Point point)
        {
            // Check if point is within circle
            int distance = (int)Math.Sqrt(Math.Pow(point.X - Center.X, 2) + Math.Pow(point.Y - Center.Y, 2));
            return distance <= Size / 2 + 3; // Add a little extra for easier selection
        }
        
        public override void Move(int deltaX, int deltaY)
        {
            Center = new Point(Center.X + deltaX, Center.Y + deltaY);
            UpdateBounds();
        }
    }
}
