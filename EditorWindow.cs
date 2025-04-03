using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Linq;

namespace VibeShot
{
    public class EditorWindow : Form
    {
        private Bitmap capturedImage;
        private Bitmap workingImage;  // Copy for editing - background only
        private Bitmap originalImage;  // Unmodified backup
        private PictureBox imageBox = null!;
        private ToolStrip toolStrip = null!;  // Using null! instead of required for broader compatibility
        private Point lastPoint = Point.Empty;
        private bool isDrawing = false;
        
        // List to store all drawable elements
        private List<DrawableElement> elements = new List<DrawableElement>();
        
        // Element selection and manipulation
        private DrawableElement? selectedElement = null;
        private Point dragStartPoint;
        private bool isDragging = false;
        private HandlePosition currentHandle = HandlePosition.None;

        // Annotation history for undo functionality
        private Stack<List<DrawableElement>> history = new Stack<List<DrawableElement>>();
        private int numberCounter = 1;

        // Current tool and settings
        private enum Tool
        {
            Select,
            Arrow,
            Rectangle,
            Text,
            NumberStamp  // Will update the name but keep the enum value for compatibility
        }

        private Tool currentTool = Tool.Select;
        private Color currentColor = Color.FromArgb(255, 230, 100, 180); // Lighter pink color
        private int currentSize = 5; // Increased from 2 to 5
        private Font currentFont = new Font("Arial", 14);
        
        // For direct text editing
        private TextBox directTextBox = null!;
        private bool isDirectEditing = false;
        private Panel? textEditPanel = null;

        // Preview state for real-time drawing
        private Point previewStart;
        private Point previewEnd;
        private bool isPreviewActive = false;

        public EditorWindow(Bitmap image)
        {
            capturedImage = image ?? throw new ArgumentNullException(nameof(image));
            // Make copies for working and backup
            workingImage = new Bitmap(capturedImage);
            originalImage = new Bitmap(capturedImage);
            
            // Save initial state for undo
            SaveForUndoElementState();
            
            InitializeComponents();
            
            // Calculate the height of all non-image UI elements
            int menuStripHeight = 24;
            int toolStripHeight = 25;
            int statusStripHeight = 22; // Approximate height of status strip
            
            // Total vertical space needed for UI elements
            int totalUIHeight = menuStripHeight + toolStripHeight + statusStripHeight;
            
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

        private void InitializeComponents()
        {
            this.Text = "VibeShot - Screenshot Editor";
            this.Icon = SystemIcons.Application; // Custom icon will be added later
            this.MinimizeBox = false;
            this.MaximizeBox = false; // Disable maximize box
            this.KeyPreview = true;
            
            // Create menu strip
            MenuStrip menuStrip = new MenuStrip();
            
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Save As...", null, SaveAs_Click);
            menuStrip.Items.Add(fileMenu);
            
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit");
            editMenu.DropDownItems.Add("Copy", null, Copy_Click);
            editMenu.DropDownItems.Add("Undo", null, Undo_Click);
            editMenu.DropDownItems.Add("Reset", null, Reset_Click);
            menuStrip.Items.Add(editMenu);
            
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            
            // Create toolbar for annotation tools
            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            
            // Select tool
            var selectButton = new ToolStripButton("Select", null, (s, e) => SetTool(Tool.Select))
            {
                Checked = true,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            
            // Arrow tool
            var arrowButton = new ToolStripButton("Arrow", null, (s, e) => SetTool(Tool.Arrow))
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            
            // Rectangle tool
            var rectButton = new ToolStripButton("Rectangle", null, (s, e) => SetTool(Tool.Rectangle))
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            
            // Text tool
            var textButton = new ToolStripButton("Text", null, (s, e) => SetTool(Tool.Text))
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            
            // Number stamp tool - update the visible label only
            var numberButton = new ToolStripButton("Stamp", null, (s, e) => SetTool(Tool.NumberStamp))
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            
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
                    currentSize = size;
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
                new ToolStripButton("Undo", null, Undo_Click) { DisplayStyle = ToolStripItemDisplayStyle.Text },
                new ToolStripButton("Reset", null, Reset_Click) { DisplayStyle = ToolStripItemDisplayStyle.Text }
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
            directTextBox.BackColor = Color.White; // Change from semi-transparent to solid white
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
                // Get the top position after toolbars
                int topPosition = menuStrip.Height + toolStrip.Height;
                // Move the image down below the toolbars
                imageBox.Location = new Point(0, topPosition);
            };
        }

        private void SetTool(Tool tool)
        {
            currentTool = tool;
            // Uncheck all tool buttons
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item is ToolStripButton toolButton)
                    toolButton.Checked = false;
            }
            
            // Check the current tool button
            int toolIndex = (int)tool;
            if (toolIndex < toolStrip.Items.Count && toolStrip.Items[toolIndex * 2] is ToolStripButton currentButton)
                currentButton.Checked = true;
            
            // Set appropriate cursor
            switch (tool)
            {
                case Tool.Select:
                    imageBox.Cursor = Cursors.Default;
                    break;
                default:
                    imageBox.Cursor = Cursors.Cross;
                    break;
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
                history.ToArray()[history.Count - 1] = null; // Allow GC to collect it
                
                // Rebuild stack without the oldest item
                var historyArray = history.ToArray();
                history.Clear();
                
                for (int i = 0; i < historyArray.Length - 1; i++)
                {
                    if (historyArray[i] != null)
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
            
            // Draw real-time preview for shapes
            if (isPreviewActive)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                switch (currentTool)
                {
                    case Tool.Arrow:
                        using (Pen pen = new Pen(currentColor, currentSize))
                        {
                            pen.CustomEndCap = new AdjustableArrowCap(5f, 5f);
                            e.Graphics.DrawLine(pen, previewStart, previewEnd);
                        }
                        break;
                        
                    case Tool.Rectangle:
                        using (Pen pen = new Pen(currentColor, currentSize))
                        {
                            Rectangle rect = new Rectangle(
                                Math.Min(previewStart.X, previewEnd.X),
                                Math.Min(previewStart.Y, previewEnd.Y),
                                Math.Abs(previewEnd.X - previewStart.X),
                                Math.Abs(previewEnd.Y - previewStart.Y));
                            
                            e.Graphics.DrawRectangle(pen, rect);
                        }
                        break;
                }
            }
        }

        private void ImageBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // If we were in direct edit mode, commit the text first
                try
                {
                    if (isDirectEditing && textEditPanel != null)
                    {
                        CommitDirectTextEdit();
                    }
                    else if (isDirectEditing)
                    {
                        // If somehow we're in edit mode but panel is null, just reset the state
                        isDirectEditing = false;
                    }
                }
                catch (Exception ex)
                {
                    // Safely handle any errors during text commit
                    MessageBox.Show($"Error while committing text: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    // Reset edit state to avoid further issues
                    isDirectEditing = false;
                    textEditPanel?.Dispose();
                    textEditPanel = null;
                    directTextBox.Visible = false;
                }
                
                lastPoint = e.Location;
                previewStart = e.Location;
                dragStartPoint = e.Location;
                
                if (currentTool == Tool.Select)
                {
                    // Check if we clicked on a handle of the selected element
                    if (selectedElement != null)
                    {
                        currentHandle = selectedElement.GetHandleAtPoint(e.Location);
                        if (currentHandle != HandlePosition.None)
                        {
                            isDragging = true;
                            return;
                        }
                    }
                    
                    // Find the topmost element that contains the click point
                    selectedElement = null;
                    for (int i = elements.Count - 1; i >= 0; i--)
                    {
                        if (elements[i].Contains(e.Location))
                        {
                            // Deselect previous element
                            foreach (var element in elements)
                                element.IsSelected = false;
                                
                            // Select new element
                            selectedElement = elements[i];
                            selectedElement.IsSelected = true;
                            isDragging = true;
                            RenderAllElements();
                            return;
                        }
                    }
                    
                    // If no element was clicked, deselect any previously selected element
                    if (selectedElement != null)
                    {
                        selectedElement.IsSelected = false;
                        selectedElement = null;
                        RenderAllElements();
                    }
                }
                else
                {
                    // Start drawing with other tools
                    isDrawing = true;
                    
                    if (currentTool == Tool.Text)
                    {
                        // Start direct text editing at click position
                        StartDirectTextEdit(e.Location);
                        isDrawing = false;
                    }
                    else if (currentTool == Tool.NumberStamp)
                    {
                        SaveForUndoElementState();
                        
                        // Create and add number stamp element
                        NumberStampElement stampElement = new NumberStampElement(
                            numberCounter, e.Location, currentColor);
                        elements.Add(stampElement);
                        
                        // Increment counter for next stamp
                        numberCounter++;
                        
                        // Update display
                        RenderAllElements();
                        isDrawing = false;
                    }
                    else if (currentTool == Tool.Arrow || currentTool == Tool.Rectangle)
                    {
                        // Start preview drawing
                        previewEnd = e.Location;
                        isPreviewActive = true;
                    }
                }
            }
        }
        
        private void StartDirectTextEdit(Point location)
        {
            // Create a new panel for text editing
            Panel textBoxPanel = new Panel();
            textBoxPanel.Location = location;
            textBoxPanel.Size = new Size(300, 100);
            textBoxPanel.BackColor = Color.FromArgb(240, 255, 255, 255);
            textBoxPanel.BorderStyle = BorderStyle.FixedSingle;
            
            // Reset the textbox properties
            directTextBox.Text = "";
            directTextBox.ForeColor = currentColor;
            directTextBox.Font = currentFont;
            directTextBox.Location = new Point(0, 0);
            directTextBox.Size = new Size(textBoxPanel.Width - 2, textBoxPanel.Height - 2);
            directTextBox.Visible = true;
            directTextBox.Dock = DockStyle.None; // Use explicit sizing instead of docking
            
            // Build the control hierarchy
            textBoxPanel.Controls.Add(directTextBox);
            
            // Get the parent control safely
            Control? parent = imageBox?.Parent;
            if (parent != null)
            {
                parent.Controls.Add(textBoxPanel);
                textBoxPanel.BringToFront();
                
                // Store panel reference and switch to edit mode
                textEditPanel = textBoxPanel;
                isDirectEditing = true;
                
                // Focus the textbox
                directTextBox.Focus();
            }
            else
            {
                // If no parent was found, dispose the panel and don't start edit mode
                textBoxPanel.Dispose();
                MessageBox.Show("Cannot initialize text editor.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                
                // Remove textbox from panel
                if (!textEditPanel.IsDisposed && textEditPanel.Controls.Contains(directTextBox))
                {
                    textEditPanel.Controls.Remove(directTextBox);
                }
                
                Control? parent = imageBox?.Parent;
                if (parent != null && !parent.IsDisposed && parent.Controls.Contains(textEditPanel))
                {
                    parent.Controls.Remove(textEditPanel);
                }
                
                // Dispose panel if needed
                if (!textEditPanel.IsDisposed)
                {
                    textEditPanel.Dispose();
                }
                textEditPanel = null;
                
                directTextBox.Visible = false;
                directTextBox.Text = "";
                isDirectEditing = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error committing text: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                // Reset state to avoid further issues
                ClearTextEditState();
            }
        }
        
        private void ClearTextEditState()
        {
            try
            {
                isDirectEditing = false;
                directTextBox.Visible = false;
                directTextBox.Text = "";
                
                if (textEditPanel != null)
                {
                    if (!textEditPanel.IsDisposed)
                    {
                        if (textEditPanel.Controls.Contains(directTextBox))
                            textEditPanel.Controls.Remove(directTextBox);
                            
                        Control? parent = imageBox?.Parent;
                        if (parent != null && parent.Controls.Contains(textEditPanel))
                            parent.Controls.Remove(textEditPanel);
                            
                        textEditPanel.Dispose();
                    }
                    textEditPanel = null;
                }
            }
            catch
            {
                // Last resort clean-up, ignore any errors
            }
        }
        
        private void CancelDirectTextEdit()
        {
            if (!isDirectEditing || textEditPanel == null) return;
            
            // Remove textbox from panel
            textEditPanel.Controls.Remove(directTextBox);
            
            Control? parent = imageBox?.Parent;
            if (parent != null && !parent.IsDisposed && parent.Controls.Contains(textEditPanel))
            {
                parent.Controls.Remove(textEditPanel);
            }
            
            textEditPanel.Dispose();
            textEditPanel = null;
            
            directTextBox.Visible = false;
            directTextBox.Text = "";
            isDirectEditing = false;
            
            // Return focus to the form
            this.Focus();
        }
        
        // Override the form's ProcessCmdKey to handle Tab and other navigation keys
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

        private void ImageBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (currentTool == Tool.Select && isDragging && selectedElement != null)
            {
                // Move the selected element
                int deltaX = e.Location.X - lastPoint.X;
                int deltaY = e.Location.Y - lastPoint.Y;
                
                if (currentHandle == HandlePosition.None)
                {
                    // Move the whole element
                    selectedElement.Move(deltaX, deltaY);
                }
                else
                {
                    // Handle resize operations here (not implemented for brevity)
                    // This would modify the element's size/shape based on the handle being dragged
                }
                
                lastPoint = e.Location;
                RenderAllElements();
                return;
            }
            
            if (isDrawing && e.Button == MouseButtons.Left)
            {
                switch (currentTool)
                {
                    case Tool.Arrow:
                    case Tool.Rectangle:
                        // Update preview end point and force redraw
                        previewEnd = e.Location;
                        imageBox.Invalidate();
                        break;
                }
            }
        }

        private void ImageBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (isDragging && selectedElement != null)
            {
                isDragging = false;
                currentHandle = HandlePosition.None;
                return;
            }
            
            if (isDrawing && e.Button == MouseButtons.Left)
            {
                switch (currentTool)
                {
                    case Tool.Arrow:
                        SaveForUndoElementState();
                        
                        // Create and add arrow element
                        ArrowElement arrowElement = new ArrowElement(
                            previewStart, e.Location, currentColor, currentSize);
                        elements.Add(arrowElement);
                        
                        RenderAllElements();
                        break;
                        
                    case Tool.Rectangle:
                        SaveForUndoElementState();
                        
                        // Create rectangle object from the preview points
                        Rectangle rect = new Rectangle(
                            Math.Min(previewStart.X, e.Location.X),
                            Math.Min(previewStart.Y, e.Location.Y),
                            Math.Abs(e.Location.X - previewStart.X),
                            Math.Abs(e.Location.Y - previewStart.Y));
                            
                        // Create and add rectangle element
                        RectangleElement rectElement = new RectangleElement(
                            rect, currentColor, currentSize);
                        elements.Add(rectElement);
                        
                        RenderAllElements();
                        break;
                }
                
                isDrawing = false;
                isPreviewActive = false;
                imageBox.Invalidate(); // Clear any preview
            }
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
            else if (e.KeyCode == Keys.Delete && selectedElement != null)
            {
                SaveForUndoElementState();
                elements.Remove(selectedElement);
                selectedElement = null;
                RenderAllElements();
                e.Handled = true;
            }
        }

        private void Undo_Click(object? sender, EventArgs e)
        {
            if (history.Count > 0)
            {
                // Restore previous element state - Fix warning CS8625
                var previousElements = history.Pop();
                elements = previousElements ?? new List<DrawableElement>(); // Use empty list instead of null
                
                // Reset selection state
                selectedElement = null;
                
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
                numberCounter = 1;
                
                // Save this clean state for undo
                SaveForUndoElementState();
                
                // Update display
                RenderAllElements();
            }
        }

        private void Copy_Click(object? sender, EventArgs e)
        {
            CopyToClipboard();
        }

        private void SaveAs_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                saveDialog.DefaultExt = "png";
                saveDialog.FileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Create a final composite image to save
                    Bitmap finalImage = new Bitmap(workingImage.Width, workingImage.Height);
                    using (Graphics g = Graphics.FromImage(finalImage))
                    {
                        g.DrawImage(workingImage, 0, 0);
                    }
                    
                    string extension = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                    ImageFormat format;
                    
                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            format = ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = ImageFormat.Bmp;
                            break;
                        default:
                            format = ImageFormat.Png;
                            break;
                    }
                    
                    finalImage.Save(saveDialog.FileName, format);
                    finalImage.Dispose();
                }
            }
        }

        private void CopyToClipboard()
        {
            try
            {
                // Need to create a final composite image to copy
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
                // If we were in text edit mode, commit the text before closing
                if (isDirectEditing && textEditPanel != null)
                {
                    CommitDirectTextEdit();
                }
                else if (isDirectEditing)
                {
                    // Just reset the state if panel is null
                    isDirectEditing = false;
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
            
            // Clean up resources
            textEditPanel?.Dispose();
            textEditPanel = null;
            
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
