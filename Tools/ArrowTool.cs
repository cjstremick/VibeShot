using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class ArrowTool : EditorToolBase
    {
        private bool isDrawing;
        
        public ArrowTool(EditorContext context) 
            : base(context, "Arrow")
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
                
                // Create and add arrow element
                ArrowElement arrowElement = new ArrowElement(
                    Context.PreviewStart, location, Context.CurrentColor, Context.CurrentSize);
                Context.AddElement(arrowElement);
                
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
                    pen.CustomEndCap = new AdjustableArrowCap(5f, 5f);
                    g.DrawLine(pen, Context.PreviewStart, Context.PreviewEnd);
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
