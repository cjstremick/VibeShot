using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class TextTool : EditorToolBase
    {
        private TextBox inlineTextBox;
        private bool isEditing = false;
        private Point editLocation;

        private Font GetEmojiCapableFont(float size, FontStyle style = FontStyle.Regular)
        {
            // Try Segoe UI Emoji, fallback to Segoe UI Symbol, then default
            try { return new Font("Segoe UI Emoji", size, style); } catch {}
            try { return new Font("Segoe UI Symbol", size, style); } catch {}
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

        public TextTool(EditorContext context) 
            : base(context, "Text")
        {
            // Use emoji-capable font for the inline text editor
            var emojiFont = GetEmojiCapableFont(context.CurrentFont.Size, context.CurrentFont.Style);
            inlineTextBox = new TextBox
            {
                Multiline = true,
                BorderStyle = BorderStyle.None,
                // Use a fully opaque background color to avoid ArgumentException
                BackColor = Color.White, // Changed from semi-transparent to solid color
                Font = emojiFont,
                ForeColor = Context.CurrentColor
            };
            
            inlineTextBox.KeyDown += InlineTextBox_KeyDown;
            inlineTextBox.LostFocus += InlineTextBox_LostFocus;
        }
        
        public override void OnMouseDown(Point location, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                // We'll either start in-place editing or show a dialog based on user preferences
                if (Context.UseInlineTextEditing)
                {
                    StartInlineEditing(location);
                }
                else
                {
                    ShowTextDialog(location);
                }
            }
        }
        
        private void StartInlineEditing(Point location)
        {
            // Save the click location
            editLocation = location;
            isEditing = true;
            
            // Configure text box and position it at the click location
            inlineTextBox.Text = string.Empty;
            inlineTextBox.Font = Context.CurrentFont;
            inlineTextBox.ForeColor = Context.CurrentColor;
            inlineTextBox.Location = new Point(location.X, location.Y);
            inlineTextBox.Size = new Size(300, 100); // Initial size
            
            // Add to control and focus
            Context.ImageControl.Controls.Add(inlineTextBox);
            inlineTextBox.BringToFront();
            inlineTextBox.Focus();
        }
        
        private void InlineTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // Handle Enter key for confirmation (Shift+Enter for new line)
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                CompleteTextEditing();
                e.Handled = true;
            }
            // Escape to cancel
            else if (e.KeyCode == Keys.Escape)
            {
                CancelTextEditing();
                e.Handled = true;
            }
        }
        
        private void InlineTextBox_LostFocus(object? sender, EventArgs e)
        {
            // Apply text when focus is lost
            if (isEditing)
            {
                CompleteTextEditing();
            }
        }
        
        private void CompleteTextEditing()
        {
            if (isEditing && !string.IsNullOrEmpty(inlineTextBox.Text))
            {
                Context.SaveForUndo();
                // Use emoji-capable font for the rendered text
                var emojiFont = GetEmojiCapableFont(Context.CurrentFont.Size, Context.CurrentFont.Style);
                var textElement = new ShadowedTextElement(
                    inlineTextBox.Text,
                    editLocation,
                    Context.CurrentColor,
                    emojiFont);
                Context.AddElement(textElement);
            }
            
            // Clean up
            CleanupTextEditing();
            Context.Render();
        }
        
        private void CancelTextEditing()
        {
            CleanupTextEditing();
            Context.Render();
        }
        
        private void CleanupTextEditing()
        {
            if (isEditing)
            {
                Context.ImageControl.Controls.Remove(inlineTextBox);
                isEditing = false;
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
                    Context.SaveForUndo();
                    var emojiFont = GetEmojiCapableFont(Context.CurrentFont.Size, Context.CurrentFont.Style);
                    var textElement = new ShadowedTextElement(
                        inputDialog.InputText,
                        location,
                        Context.CurrentColor, 
                        emojiFont);
                    Context.AddElement(textElement);
                    Context.Render();
                }
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            // If editing, cancel and remove the inline textbox
            CancelTextEditing();
        }
    }
}
