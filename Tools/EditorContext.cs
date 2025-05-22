using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class EditorContext
    {
        // Change from private to public property
        public Control ImageControl { get; }
        private readonly Action renderCallback;
        private readonly Action<DrawableElement> addElementCallback;
        private readonly Action saveForUndoCallback;
        
        public Color CurrentColor { get; set; }
        public int CurrentSize { get; set; }
        public Font CurrentFont { get; set; }
        public List<DrawableElement> Elements { get; }
        
        // User preference for inline text editing
        public bool UseInlineTextEditing { get; set; } = true;
        
        // For direct text editing
        public TextBox DirectTextBox { get; }
        public Panel? TextEditPanel { get; set; }
        public bool IsDirectEditing { get; set; }
        
        // Preview state for real-time drawing
        public Point PreviewStart { get; set; }
        public Point PreviewEnd { get; set; }
        public bool IsPreviewActive { get; set; }
        
        public int NumberCounter { get; set; } = 1;
        
        public EditorContext(
            Control imageControl, 
            List<DrawableElement> elements,
            TextBox directTextBox,
            Color currentColor, 
            int currentSize, 
            Font currentFont,
            Action renderCallback,
            Action<DrawableElement> addElementCallback,
            Action saveForUndoCallback)
        {
            // Store imageControl as public property instead of private field
            ImageControl = imageControl ?? throw new ArgumentNullException(nameof(imageControl));
            Elements = elements ?? throw new ArgumentNullException(nameof(elements));
            DirectTextBox = directTextBox ?? throw new ArgumentNullException(nameof(directTextBox));
            CurrentColor = currentColor;
            CurrentSize = currentSize;
            CurrentFont = currentFont;
            this.renderCallback = renderCallback ?? throw new ArgumentNullException(nameof(renderCallback));
            this.addElementCallback = addElementCallback ?? throw new ArgumentNullException(nameof(addElementCallback));
            this.saveForUndoCallback = saveForUndoCallback ?? throw new ArgumentNullException(nameof(saveForUndoCallback));
        }
        
        public void Render()
        {
            renderCallback();
        }
        
        public void AddElement(DrawableElement element)
        {
            addElementCallback(element);
        }
        
        public void SaveForUndo()
        {
            saveForUndoCallback();
        }
        
        public void InvalidateView()
        {
            ImageControl.Invalidate();
        }
    }
}
