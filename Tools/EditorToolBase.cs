using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public abstract class EditorToolBase
    {
        protected EditorContext Context { get; }
        
        public string Name { get; }
        public Cursor Cursor { get; protected set; } = Cursors.Cross;
        public bool IsActive { get; set; }
        
        public EditorToolBase(EditorContext context, string name)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
        
        public abstract void OnMouseDown(Point location, MouseButtons buttons);
        public abstract void OnMouseMove(Point location, MouseButtons buttons);
        public abstract void OnMouseUp(Point location, MouseButtons buttons);
        public abstract void OnPaint(Graphics g);
        
        public virtual void OnActivate() 
        {
            IsActive = true;
        }
        
        public virtual void OnDeactivate() 
        {
            IsActive = false;
        }
    }
}
