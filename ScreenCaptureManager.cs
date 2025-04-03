using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace VibeShot
{
    public class ScreenCaptureManager
    {
        private SelectionOverlay? selectionOverlay;
        private Bitmap? fullScreenshot;

        public void StartCapture()
        {
            // Capture the entire screen (all monitors)
            fullScreenshot = CaptureAllScreens();
            
            // Show selection overlay form
            selectionOverlay = new SelectionOverlay(fullScreenshot);
            selectionOverlay.SelectionCompleted += SelectionOverlay_SelectionCompleted;
            selectionOverlay.Show();
        }

        private void SelectionOverlay_SelectionCompleted(object? sender, Rectangle selectedRegion)
        {
            if (fullScreenshot != null && selectedRegion.Width > 0 && selectedRegion.Height > 0)
            {
                // Crop the screenshot to the selected region
                Bitmap croppedImage = CropImage(fullScreenshot, selectedRegion);
                
                // Show the editor window instead of preview
                ShowEditorWindow(croppedImage);
            }
            
            // Clean up
            if (selectionOverlay != null)
            {
                selectionOverlay.SelectionCompleted -= SelectionOverlay_SelectionCompleted;
                selectionOverlay.Dispose();
                selectionOverlay = null;
            }
            
            fullScreenshot?.Dispose();
            fullScreenshot = null;
        }

        private Bitmap CaptureAllScreens()
        {
            // Get the combined size of all screens
            Rectangle bounds = GetAllScreensBounds();
            
            // Create a bitmap to hold the screenshot
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            
            // Draw the screen content to the bitmap
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
            
            return screenshot;
        }

        private Rectangle GetAllScreensBounds()
        {
            // Start with an empty rectangle
            Rectangle bounds = Rectangle.Empty;
            
            // Combine all screen bounds
            foreach (Screen screen in Screen.AllScreens)
            {
                if (bounds == Rectangle.Empty)
                {
                    // Initialize with the first screen
                    bounds = screen.Bounds;
                }
                else
                {
                    // Union with each additional screen
                    bounds = Rectangle.Union(bounds, screen.Bounds);
                }
            }
            
            return bounds;
        }

        private Bitmap CropImage(Bitmap source, Rectangle region)
        {
            Bitmap croppedImage = new Bitmap(region.Width, region.Height);
            
            using (Graphics g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(source, new Rectangle(0, 0, croppedImage.Width, croppedImage.Height),
                    region, GraphicsUnit.Pixel);
            }
            
            return croppedImage;
        }

        private void ShowEditorWindow(Bitmap capturedImage)
        {
            // Create and show the editor window
            EditorWindow editorWindow = new EditorWindow(capturedImage);
            editorWindow.Show();
        }
    }
}
