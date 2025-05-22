using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using VibeShot.Tools;

namespace VibeShot
{
    public class EditorWindow : Form
    {
        private Bitmap capturedImage;
        private Bitmap workingImage;  // Copy for editing - background only
        private Bitmap originalImage;  // Unmodified backup
        private PictureBox imageBox = null!;
        private ToolStrip toolStrip = null!;
        
        // List to store all drawable elements
        private List<DrawableElement> elements = new List<DrawableElement>();
        
        // Annotation history for undo functionality
        private Stack<List<DrawableElement>> history = new Stack<List<DrawableElement>>();
        
        // Tool management
        private EditorContext editorContext = null!;
        private ToolManager toolManager = null!;
        
        // Current settings
        private Color currentColor = Color.FromArgb(255, 230, 100, 180); // Lighter pink color
        private int currentSize = 5; // Default line thickness
        private Font currentFont = new Font("Arial", 14);
        
        // For direct text editing
        private TextBox directTextBox = null!;
        private bool isDirectEditing = false;
        private Panel? textEditPanel = null;

        public EditorWindow(Bitmap image)
        {
            capturedImage = image ?? throw new ArgumentNullException(nameof(image));
            // Make copies for working and backup
            workingImage = new Bitmap(capturedImage);
            originalImage = new Bitmap(capturedImage);
            
            InitializeComponents();
            
            // Initialize editor context and tool manager after UI components are created
            editorContext = new EditorContext(
                imageBox,
                elements,
                directTextBox,
                currentColor,
                currentSize,
                currentFont,
                RenderAllElements,
                AddElement,
                SaveForUndoElementState
            );
            
            toolManager = new ToolManager(editorContext);
            
            // Save initial state for undo
            SaveForUndoElementState();
            
            // Calculate the height of all non-image UI elements
            int toolStripHeight = 25;
            int statusStripHeight = 22; // Approximate height of status strip
            
            // Total vertical space needed for UI elements
            int totalUIHeight = toolStripHeight + statusStripHeight;
            
            // Set client size to match image size plus space for controls
            this.ClientSize = new Size(
                Math.Max(capturedImage.Width, 640), 
                capturedImage.Height + totalUIHeight);
            
            // Center on screen
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Make the form non-resizable
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }
        
        private void AddElement(DrawableElement element)
        {
            elements.Add(element);
        }

        private void InitializeComponents()
        {
            this.Text = "VibeShot - Screenshot Editor";
            
            // Load custom icon instead of using system icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VibeShot.ico");
            this.Icon = File.Exists(iconPath) 
                ? new Icon(iconPath) 
                : SystemIcons.Application; // Fallback to system icon if not found
            
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.KeyPreview = true;
            
            // Create toolbar for annotation tools
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            
            // Create tool buttons
            var selectButton = CreateToolButton("Select", (s, e) => SetActiveTool("Select"));
            var arrowButton = CreateToolButton("Arrow", (s, e) => SetActiveTool("Arrow"));
            var rectButton = CreateToolButton("Rectangle", (s, e) => SetActiveTool("Rectangle"));
            var textButton = CreateToolButton("Text", (s, e) => SetActiveTool("Text"));
            var numberButton = CreateToolButton("Stamp", (s, e) => SetActiveTool("Stamp"));
            
            // Set initial selection
            selectButton.Checked = true;
            
            // Color selector
            var colorButton = new ToolStripButton("Color...", null, (s, e) => ChooseColor())
            {
                BackColor = currentColor,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            
            // Size selector
            var sizeSelector = new ToolStripComboBox("Size")
            {
                Items = { "1", "2", "3", "4", "5", "8", "10", "15" },
                SelectedIndex = 4, // Default to 5px (index 4)
                AutoSize = false,
                Width = 50
            };
            sizeSelector.SelectedIndexChanged += (s, e) => 
            {
                if (int.TryParse(sizeSelector.SelectedItem?.ToString(), out int size))
                {
                    currentSize = size;
                    if (editorContext != null)
                        editorContext.CurrentSize = size;
                }
            };
            
            // Reset button
            var resetButton = new ToolStripButton("Reset", null, Reset_Click)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            
            // Add items to toolbar
            toolStrip.Items.AddRange(new ToolStripItem[] 
            {
                selectButton, 
                new ToolStripSeparator(), 
                arrowButton, 
                rectButton, 
                textButton, 
                numberButton,
                new ToolStripSeparator(),
                colorButton,
                sizeSelector,
                new ToolStripSeparator(),
                resetButton
            });
            
            this.Controls.Add(toolStrip);
            
            // Create panel for image with proper positioning
            Panel imagePanel = new Panel();
            imagePanel.Dock = DockStyle.Fill;
            imagePanel.AutoScroll = false;
            imagePanel.Padding = new Padding(0, 0, 0, 0);
            
            // Create picture box to display the image
            imageBox = new PictureBox();
            imageBox.Size = new Size(capturedImage.Width, capturedImage.Height);
            // Position the image below the toolbars
            imageBox.Location = new Point(0, 0);
            imageBox.SizeMode = PictureBoxSizeMode.Normal;
            imageBox.Image = workingImage;
            imageBox.MouseDown += ImageBox_MouseDown;
            imageBox.MouseMove += ImageBox_MouseMove;
            imageBox.MouseUp += ImageBox_MouseUp;
            imageBox.Paint += ImageBox_Paint;
            
            // Create the direct editing textbox (initially hidden)
            directTextBox = new TextBox();
            directTextBox.Multiline = true;
            directTextBox.BorderStyle = BorderStyle.None;
            directTextBox.BackColor = Color.White;
            directTextBox.Font = currentFont;
            directTextBox.ForeColor = currentColor;
            directTextBox.Visible = false;
            directTextBox.KeyDown += DirectTextBox_KeyDown;
            directTextBox.LostFocus += DirectTextBox_LostFocus;
            
            // Add controls to panel
            imagePanel.Controls.Add(imageBox);
            
            // Add panel to form
            this.Controls.Add(imagePanel);
            
            // Add status strip with image info
            StatusStrip statusStrip = new StatusStrip();
            statusStrip.Items.Add($"Image size: {capturedImage.Width} x {capturedImage.Height}");
            statusStrip.Items.Add("Press Ctrl+C to copy to clipboard");
            this.Controls.Add(statusStrip);
            
            // Set up event handlers
            this.KeyDown += EditorWindow_KeyDown;
            this.FormClosing += EditorWindow_FormClosing;
            
            // Adjust the position of the image after all controls are loaded
            this.Load += (s, e) => {
                // Get the top position after toolbar
                int topPosition = toolStrip.Height;
                // Move the image down below the toolbar
                imageBox.Location = new Point(0, topPosition);
            };
        }
        
        private ToolStripButton CreateToolButton(string text, EventHandler clickHandler)
        {
            return new ToolStripButton(text, null, clickHandler)
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
        }
        
        private void SetActiveTool(string toolName)
        {
            if (toolManager.SetActiveTool(toolName))
            {
                // Update UI to show selected tool
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    if (item is ToolStripButton button)
                    {
                        button.Checked = (button.Text == toolName);
                    }
                }
                
                // Update cursor
                imageBox.Cursor = toolManager.GetActiveCursor();
            }
        }

        private void ChooseColor()
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = currentColor;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    currentColor = dialog.Color;
                    editorContext.CurrentColor = currentColor;
                    
                    // Update color button
                    foreach (ToolStripItem item in toolStrip.Items)
                    {
                        if (item is ToolStripButton button && button.Text == "Color...")
                            button.BackColor = currentColor;
                    }
                }
            }
        }

        private void SaveForUndoElementState()
        {
            // Make a deep copy of the current elements list
            List<DrawableElement> elementsCopy = new List<DrawableElement>();
            foreach (var element in elements)
            {
                if (element is ArrowElement arrow)
                {
                    elementsCopy.Add(new ArrowElement(arrow.StartPoint, arrow.EndPoint, arrow.Color, arrow.Size));
                }
                else if (element is RectangleElement rect)
                {
                    elementsCopy.Add(new RectangleElement(rect.Rect, rect.Color, rect.Size));
                }
                else if (element is TextElement text)
                {
                    elementsCopy.Add(new TextElement(text.Text, text.Location, text.Color, text.Font));
                }
                else if (element is ShadowedTextElement shadowedText)
                {
                    elementsCopy.Add(new ShadowedTextElement(shadowedText.Text, shadowedText.Location, 
                        shadowedText.Color, shadowedText.Font));
                }
                else if (element is NumberStampElement stamp)
                {
                    elementsCopy.Add(new NumberStampElement(stamp.Number, stamp.Center, stamp.Color));
                }
            }
            
            // Push copy to history
            history.Push(elementsCopy);
            
            // Limit history size to prevent memory issues
            if (history.Count > 20)
            {
                var historyArray = history.ToArray();
                history.Clear();
                
                for (int i = 0; i < historyArray.Length - 1; i++)
                {
                    history.Push(historyArray[i]);
                }
            }
        }

        private void RenderAllElements()
        {
            // Create a clean working image from the original captured image
            using (Graphics g = Graphics.FromImage(workingImage))
            {
                g.Clear(Color.White);
                g.DrawImage(originalImage, 0, 0);
                
                // Draw all elements in order
                foreach (var element in elements)
                {
                    element.Draw(g);
                }
            }
            
            // Update the picture box
            imageBox.Invalidate();
        }

        private void ImageBox_Paint(object? sender, PaintEventArgs e)
        {
            // Draw the base image with all elements
            e.Graphics.DrawImage(workingImage, 0, 0);
            
            // Let the active tool handle any additional painting
            toolManager.HandlePaint(e.Graphics);
        }

        private void ImageBox_MouseDown(object? sender, MouseEventArgs e)
        {
            // Handle direct text editing first
            if (isDirectEditing)
            {
                CommitDirectTextEdit();
            }
            
            // Forward event to the tool manager
            toolManager.HandleMouseDown(e.Location, e.Button);
        }
        
        private void ImageBox_MouseMove(object? sender, MouseEventArgs e)
        {
            // Forward event to the tool manager
            toolManager.HandleMouseMove(e.Location, e.Button);
        }

        private void ImageBox_MouseUp(object? sender, MouseEventArgs e)
        {
            // Forward event to the tool manager
            toolManager.HandleMouseUp(e.Location, e.Button);
        }
        
        private void DirectTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // Enter commits the text
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                CommitDirectTextEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // Escape cancels the edit
            else if (e.KeyCode == Keys.Escape)
            {
                CancelDirectTextEdit();
                e.Handled = true;
            }
        }
        
        private void DirectTextBox_LostFocus(object? sender, EventArgs e)
        {
            // When textbox loses focus, commit the text if there is any
            try
            {
                // Add delay to avoid race condition with mouse events
                BeginInvoke(new Action(() =>
                {
                    if (isDirectEditing && textEditPanel != null)
                    {
                        CommitDirectTextEdit();
                    }
                }));
            }
            catch (Exception ex)
            {
                // Log or handle the error gracefully
                Console.WriteLine($"Error in LostFocus: {ex.Message}");
            }
        }
        
        private void CommitDirectTextEdit()
        {
            if (!isDirectEditing || textEditPanel == null) return;
            
            try
            {
                // Ensure controls exist and panel contains textbox
                if (textEditPanel.IsDisposed || !textEditPanel.Controls.Contains(directTextBox))
                {
                    CancelDirectTextEdit();
                    return;
                }
                
                string text = directTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    // Save for undo
                    SaveForUndoElementState();
                    
                    // Create a new text element
                    TextElement textElement = new TextElement(
                        text, textEditPanel.Location, currentColor, currentFont);
                    elements.Add(textElement);
                    
                    // Update display
                    RenderAllElements();
                }
                
                CleanupTextEdit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error committing text: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                // Reset state to avoid further issues
                CleanupTextEdit();
            }
        }
        
        private void CleanupTextEdit()
        {
            if (textEditPanel != null)
            {
                if (!textEditPanel.IsDisposed)
                {
                    if (textEditPanel.Controls.Contains(directTextBox))
                        textEditPanel.Controls.Remove(directTextBox);
                        
                    Control? parent = textEditPanel.Parent;
                    if (parent != null && !parent.IsDisposed)
                        parent.Controls.Remove(textEditPanel);
                        
                    textEditPanel.Dispose();
                }
                textEditPanel = null;
            }
            
            directTextBox.Visible = false;
            directTextBox.Text = "";
            isDirectEditing = false;
        }
        
        private void CancelDirectTextEdit()
        {
            if (!isDirectEditing) return;
            CleanupTextEdit();
            this.Focus();
        }
        
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // If we're direct editing, let the textbox handle most keys
            if (isDirectEditing)
            {
                if (keyData == Keys.Tab)
                {
                    // Insert a tab character instead of changing focus
                    directTextBox.SelectedText = "\t";
                    return true;
                }
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void EditorWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            // Handle Ctrl+C to copy to clipboard
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyToClipboard();
                e.Handled = true;
            }
            // Handle Ctrl+Z to undo
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
            // Handle Delete to remove selected element
            else if (e.KeyCode == Keys.Delete)
            {
                var selectionTool = toolManager.SelectionTool;
                selectionTool?.DeleteSelectedElement();
                e.Handled = true;
            }
            // Handle ESC to clear selection if selection tool is active
            else if (e.KeyCode == Keys.Escape)
            {
                if (toolManager.GetActiveTool() == toolManager.SelectionTool)
                {
                    toolManager.SelectionTool.HandleKeyDown(Keys.Escape);
                    e.Handled = true;
                }
            }
        }

        private void Undo_Click(object? sender, EventArgs e)
        {
            if (history.Count > 0)
            {
                // Restore previous element state
                var previousElements = history.Pop();
                elements = previousElements ?? new List<DrawableElement>();
                
                // Reset selection state
                var selectionTool = toolManager.SelectionTool;
                selectionTool?.ClearSelection();
                
                // Update display
                RenderAllElements();
            }
        }

        private void Reset_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Reset all edits? This cannot be undone.",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                // Clear undo history
                history.Clear();
                
                // Reset elements list
                elements.Clear();
                
                // Reset counter
                editorContext.NumberCounter = 1;
                
                // Save this clean state for undo
                SaveForUndoElementState();
                
                // Update display
                RenderAllElements();
            }
        }

        private void CopyToClipboard()
        {
            try
            {
                // Create a final composite image to copy
                Bitmap finalImage = new Bitmap(workingImage.Width, workingImage.Height);
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    g.DrawImage(workingImage, 0, 0);
                }
                
                Clipboard.SetImage(finalImage);
                ShowCopyConfirmation();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to copy image to clipboard: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowCopyConfirmation()
        {
            // Create a temporary label for feedback
            Label label = new Label
            {
                Text = "✓ Copied to clipboard",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(180, 0, 120, 0),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30,
                Width = 200
            };

            this.Controls.Add(label);
            label.BringToFront();
            
            // Position at bottom center
            label.Left = (this.ClientSize.Width - label.Width) / 2;
            label.Top = this.ClientSize.Height - label.Height - 50;

            // Create a timer to hide the label
            Timer timer = new Timer { Interval = 1500 };
            timer.Tick += (s, e) => {
                this.Controls.Remove(label);
                label.Dispose();
                timer.Dispose();
            };
            timer.Start();
        }

        private void EditorWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // If we're in text edit mode, commit the text before closing
                if (isDirectEditing)
                {
                    CommitDirectTextEdit();
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
            
            // Clean up resources
            CleanupTextEdit();
            
            if (imageBox != null)
            {
                imageBox.Image = null;
            }
            
            // Dispose all bitmaps
            workingImage?.Dispose();
            originalImage?.Dispose();
            capturedImage?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose resources
                workingImage?.Dispose();
                originalImage?.Dispose();
                capturedImage?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
