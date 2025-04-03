using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot
{
    public class TextInputDialog : Form
    {
        private TextBox textBox;
        public string InputText { get; private set; } = "";

        public TextInputDialog()
        {
            this.Text = "Enter Text";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(300, 200); // Increased height for better layout
            
            // Create controls
            Label label = new Label
            {
                Text = "Enter text:",
                AutoSize = true,
                Location = new Point(10, 15)
            };
            
            textBox = new TextBox
            {
                Location = new Point(10, 40),
                Width = 265,
                Height = 80, // Increased height for text input
                Multiline = true,
                AcceptsReturn = true
            };
            
            Button okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(120, 130), // Moved down below the textbox
                Size = new Size(75, 25)
            };
            
            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(200, 130), // Moved down below the textbox
                Size = new Size(75, 25)
            };
            
            // Add controls to form
            this.Controls.Add(label);
            this.Controls.Add(textBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            
            // Set up events
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
            this.FormClosing += TextInputDialog_FormClosing;
            
            // Set focus to the textbox
            this.Load += (s, e) => textBox.Focus();
        }

        private void TextInputDialog_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                InputText = textBox.Text;
            }
        }
    }
}
