using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class TextTool : EditorToolBase
    {
        public TextTool(EditorContext context) 
            : base(context, "Text")
        {
        }
        
        public override void OnMouseDown(Point location, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                // Show text input dialog instead of direct editing
                ShowTextDialog(location);
            }
        }
        
        public override void OnMouseMove(Point location, MouseButtons buttons)
        {
            // No action needed for text tool on mouse move
        }
        
        public override void OnMouseUp(Point location, MouseButtons buttons)
        {
            // No action needed for text tool on mouse up
        }
        
        public override void OnPaint(Graphics g)
        {
            // No preview painting needed for text tool
        }
        
        private void ShowTextDialog(Point location)
        {
            // Create input dialog to get text
            using (var inputDialog = new TextInputDialog())
            {
                // Position the dialog near but not directly under the mouse
                Point screenPoint = Context.ImageControl.PointToScreen(location);
                inputDialog.StartPosition = FormStartPosition.Manual;
                inputDialog.Location = new Point(
                    screenPoint.X + 20, // Offset a bit to the right
                    screenPoint.Y + 20  // Offset a bit down
                );
                
                // Show dialog and wait for result
                if (inputDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(inputDialog.InputText))
                {
                    // Save for undo
                    Context.SaveForUndo();
                    
                    // Create a new text element with the input text
                    var textElement = new ShadowedTextElement(
                        inputDialog.InputText,
                        location,
                        Context.CurrentColor, 
                        Context.CurrentFont);
                        
                    // Add the element
                    Context.AddElement(textElement);
                    
                    // Update display
                    Context.Render();
                }
            }
        }
    }
}
