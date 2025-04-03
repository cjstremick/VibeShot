using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class RectangleTool : EditorToolBase
    {
        private bool isDrawing;
        
        public RectangleTool(EditorContext context) 
            : base(context, "Rectangle")
        {
        }
        
        public override void OnMouseDown(Point location, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                isDrawing = true;
                Context.PreviewStart = location;
                Context.PreviewEnd = location;
                Context.IsPreviewActive = true;
            }
        }
        
        public override void OnMouseMove(Point location, MouseButtons buttons)
        {
            if (isDrawing && buttons == MouseButtons.Left)
            {
                Context.PreviewEnd = location;
                Context.InvalidateView();
            }
        }
        
        public override void OnMouseUp(Point location, MouseButtons buttons)
        {
            if (isDrawing && buttons == MouseButtons.Left)
            {
                Context.SaveForUndo();
                
                // Create rectangle object from the preview points
                Rectangle rect = new Rectangle(
                    Math.Min(Context.PreviewStart.X, location.X),
                    Math.Min(Context.PreviewStart.Y, location.Y),
                    Math.Abs(location.X - Context.PreviewStart.X),
                    Math.Abs(location.Y - Context.PreviewStart.Y));
                
                // Create and add rectangle element
                RectangleElement rectElement = new RectangleElement(
                    rect, Context.CurrentColor, Context.CurrentSize);
                Context.AddElement(rectElement);
                
                isDrawing = false;
                Context.IsPreviewActive = false;
                Context.Render();
            }
        }
        
        public override void OnPaint(Graphics g)
        {
            if (Context.IsPreviewActive && IsActive)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                using (Pen pen = new Pen(Context.CurrentColor, Context.CurrentSize))
                {
                    Rectangle rect = new Rectangle(
                        Math.Min(Context.PreviewStart.X, Context.PreviewEnd.X),
                        Math.Min(Context.PreviewStart.Y, Context.PreviewEnd.Y),
                        Math.Abs(Context.PreviewEnd.X - Context.PreviewStart.X),
                        Math.Abs(Context.PreviewEnd.Y - Context.PreviewStart.Y));
                    
                    g.DrawRectangle(pen, rect);
                }
            }
        }
        
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            isDrawing = false;
            Context.IsPreviewActive = false;
        }
    }
}
