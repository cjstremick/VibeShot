using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class SelectionTool : EditorToolBase
    {
        private DrawableElement? selectedElement;
        private bool isDragging;
        private Point lastPoint;
        private HandlePosition currentHandle = HandlePosition.None;
        
        public SelectionTool(EditorContext context) 
            : base(context, "Select")
        {
            Cursor = Cursors.Default;
        }
        
        public override void OnMouseDown(Point location, MouseButtons buttons)
        {
            if (buttons != MouseButtons.Left) return;
            
            lastPoint = location;
            isDragging = false;
            
            // Check if we clicked on a handle of the selected element
            if (selectedElement != null)
            {
                currentHandle = selectedElement.GetHandleAtPoint(location);
                if (currentHandle != HandlePosition.None)
                {
                    isDragging = true;
                    return;
                }
            }
            
            // Find the topmost element that contains the click point
            selectedElement = null;
            for (int i = Context.Elements.Count - 1; i >= 0; i--)
            {
                if (Context.Elements[i].Contains(location))
                {
                    // Deselect previous elements
                    foreach (var element in Context.Elements)
                    {
                        element.IsSelected = false;
                    }
                    
                    // Select new element
                    selectedElement = Context.Elements[i];
                    selectedElement.IsSelected = true;
                    isDragging = true;
                    Context.Render();
                    return;
                }
            }
            
            // If no element was clicked, deselect any previously selected element
            if (selectedElement != null)
            {
                selectedElement.IsSelected = false;
                selectedElement = null;
                Context.Render();
            }
        }
        
        public override void OnMouseMove(Point location, MouseButtons buttons)
        {
            if (isDragging && selectedElement != null && buttons == MouseButtons.Left)
            {
                // Move the selected element
                int deltaX = location.X - lastPoint.X;
                int deltaY = location.Y - lastPoint.Y;
                
                if (currentHandle == HandlePosition.None)
                {
                    // Move the whole element
                    selectedElement.Move(deltaX, deltaY);
                }
                else
                {
                    // Handle resize operations (not implemented for brevity)
                    // This would modify the element's size/shape based on the handle being dragged
                }
                
                lastPoint = location;
                Context.Render();
            }
        }
        
        public override void OnMouseUp(Point location, MouseButtons buttons)
        {
            if (isDragging && selectedElement != null)
            {
                isDragging = false;
                currentHandle = HandlePosition.None;
            }
        }
        
        public override void OnPaint(Graphics g)
        {
            // No special painting needed for selection tool
        }
        
        public DrawableElement? GetSelectedElement()
        {
            return selectedElement;
        }
        
        public void ClearSelection()
        {
            if (selectedElement != null)
            {
                selectedElement.IsSelected = false;
                selectedElement = null;
            }
        }
        
        public void DeleteSelectedElement()
        {
            if (selectedElement != null)
            {
                Context.SaveForUndo();
                Context.Elements.Remove(selectedElement);
                selectedElement = null;
                Context.Render();
            }
        }
    }
}
