using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VibeShot.Tools
{
    public class ToolManager
    {
        private readonly Dictionary<string, EditorToolBase> tools = new Dictionary<string, EditorToolBase>();
        private EditorToolBase? activeTool;
        
        public SelectionTool SelectionTool { get; }
        
        public ToolManager(EditorContext context)
        {
            // Create all tool instances
            SelectionTool = new SelectionTool(context);
            
            // Register all tools
            RegisterTool(SelectionTool);
            RegisterTool(new ArrowTool(context));
            RegisterTool(new RectangleTool(context));
            RegisterTool(new TextTool(context));
            RegisterTool(new NumberStampTool(context));
            
            // Default to selection tool
            SetActiveTool(SelectionTool.Name);
        }
        
        public void RegisterTool(EditorToolBase tool)
        {
            if (tool == null) throw new ArgumentNullException(nameof(tool));
            tools[tool.Name] = tool;
        }
        
        public bool SetActiveTool(string toolName)
        {
            if (!tools.TryGetValue(toolName, out var tool))
                return false;
                
            // Deactivate current tool
            activeTool?.OnDeactivate();
            
            // Activate new tool
            activeTool = tool;
            activeTool.OnActivate();
            
            return true;
        }
        
        public EditorToolBase? GetActiveTool()
        {
            return activeTool;
        }
        
        public Cursor GetActiveCursor()
        {
            return activeTool?.Cursor ?? Cursors.Default;
        }
        
        public void HandleMouseDown(Point location, MouseButtons buttons)
        {
            activeTool?.OnMouseDown(location, buttons);
        }
        
        public void HandleMouseMove(Point location, MouseButtons buttons)
        {
            activeTool?.OnMouseMove(location, buttons);
        }
        
        public void HandleMouseUp(Point location, MouseButtons buttons)
        {
            activeTool?.OnMouseUp(location, buttons);
        }
        
        public void HandlePaint(Graphics g)
        {
            activeTool?.OnPaint(g);
        }
    }
}
