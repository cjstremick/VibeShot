using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace VibeShot
{
    public abstract class DrawableElement
    {
        // Common properties
        public bool IsSelected { get; set; }
        public Rectangle Bounds { get; protected set; }
        
        // Selection handles size
        protected const int HandleSize = 7;
        
        // Abstract methods that all drawable elements must implement
        public abstract void Draw(Graphics g);
        public abstract bool Contains(Point point);
        public abstract void Move(int deltaX, int deltaY);
        
        // Helper method to draw selection handles when the element is selected
        protected void DrawSelectionHandles(Graphics g)
        {
            if (!IsSelected) return;
            
            using (SolidBrush handleBrush = new SolidBrush(Color.White))
            using (Pen handlePen = new Pen(Color.Black, 1))
            {
                // Top-left
                DrawHandle(g, Bounds.Left, Bounds.Top, handleBrush, handlePen);
                // Top-middle
                DrawHandle(g, Bounds.Left + Bounds.Width / 2, Bounds.Top, handleBrush, handlePen);
                // Top-right
                DrawHandle(g, Bounds.Right, Bounds.Top, handleBrush, handlePen);
                
                // Middle-left
                DrawHandle(g, Bounds.Left, Bounds.Top + Bounds.Height / 2, handleBrush, handlePen);
                // Middle-right
                DrawHandle(g, Bounds.Right, Bounds.Top + Bounds.Height / 2, handleBrush, handlePen);
                
                // Bottom-left
                DrawHandle(g, Bounds.Left, Bounds.Bottom, handleBrush, handlePen);
                // Bottom-middle
                DrawHandle(g, Bounds.Left + Bounds.Width / 2, Bounds.Bottom, handleBrush, handlePen);
                // Bottom-right
                DrawHandle(g, Bounds.Right, Bounds.Bottom, handleBrush, handlePen);
            }
        }
        
        // Helper to draw a single handle
        private void DrawHandle(Graphics g, int x, int y, Brush brush, Pen pen)
        {
            int halfSize = HandleSize / 2;
            Rectangle handleRect = new Rectangle(x - halfSize, y - halfSize, HandleSize, HandleSize);
            g.FillRectangle(brush, handleRect);
            g.DrawRectangle(pen, handleRect);
        }
        
        // Check if point is over any handle (for resize operations)
        public virtual HandlePosition GetHandleAtPoint(Point point)
        {
            if (!IsSelected) return HandlePosition.None;
            
            int halfSize = HandleSize / 2;
            
            // Top-left
            if (IsPointNearPosition(point, Bounds.Left, Bounds.Top, halfSize))
                return HandlePosition.TopLeft;
                
            // Top-middle
            if (IsPointNearPosition(point, Bounds.Left + Bounds.Width / 2, Bounds.Top, halfSize))
                return HandlePosition.TopMiddle;
                
            // Top-right
            if (IsPointNearPosition(point, Bounds.Right, Bounds.Top, halfSize))
                return HandlePosition.TopRight;
                
            // Middle-left
            if (IsPointNearPosition(point, Bounds.Left, Bounds.Top + Bounds.Height / 2, halfSize))
                return HandlePosition.MiddleLeft;
                
            // Middle-right
            if (IsPointNearPosition(point, Bounds.Right, Bounds.Top + Bounds.Height / 2, halfSize))
                return HandlePosition.MiddleRight;
                
            // Bottom-left
            if (IsPointNearPosition(point, Bounds.Left, Bounds.Bottom, halfSize))
                return HandlePosition.BottomLeft;
                
            // Bottom-middle
            if (IsPointNearPosition(point, Bounds.Left + Bounds.Width / 2, Bounds.Bottom, halfSize))
                return HandlePosition.BottomMiddle;
                
            // Bottom-right
            if (IsPointNearPosition(point, Bounds.Right, Bounds.Bottom, halfSize))
                return HandlePosition.BottomRight;
                
            return HandlePosition.None;
        }
        
        private bool IsPointNearPosition(Point point, int x, int y, int tolerance)
        {
            return Math.Abs(point.X - x) <= tolerance && Math.Abs(point.Y - y) <= tolerance;
        }
    }
    
    public enum HandlePosition
    {
        None,
        TopLeft,
        TopMiddle,
        TopRight,
        MiddleLeft,
        MiddleRight,
        BottomLeft,
        BottomMiddle,
        BottomRight
    }
}
