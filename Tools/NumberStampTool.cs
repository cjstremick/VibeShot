using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class NumberStampTool : EditorToolBase
    {
        public NumberStampTool(EditorContext context) 
            : base(context, "Stamp")
        {
        }
        
        public override void OnMouseDown(Point location, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                Context.SaveForUndo();
                
                // Create and add number stamp element
                NumberStampElement stampElement = new NumberStampElement(
                    Context.NumberCounter, location, Context.CurrentColor);
                Context.AddElement(stampElement);
                
                // Increment counter for next stamp
                Context.NumberCounter++;
                
                // Update display
                Context.Render();
            }
        }
        
        public override void OnMouseMove(Point location, MouseButtons buttons)
        {
            // No action needed for stamp tool on mouse move
        }
        
        public override void OnMouseUp(Point location, MouseButtons buttons)
        {
            // No action needed for stamp tool on mouse up
        }
        
        public override void OnPaint(Graphics g)
        {
            // No preview painting needed for stamp tool
        }
    }
}
