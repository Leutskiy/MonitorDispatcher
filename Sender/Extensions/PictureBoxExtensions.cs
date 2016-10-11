using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Sender.Extensions
{
    public static class PictureBoxExtensions
    {
        public static void ForceImageIntoPictureBox(this PictureBox pictureBox, int width, int height, string imageFilePath)
        {
            if (pictureBox.Image != null)
            {
                pictureBox.Width  = width;
                pictureBox.Height = height;
            }

            var image = Image.FromFile(imageFilePath);
            var imagePreviewSize = new Size(4, 4);

            // Get the image's original width and height
            int originalWidth  = image.Width;
            int originalHeight = image.Height;

            // To preserve the aspect ratio
            var maxWidth  = pictureBox.Width;
            var maxHeight = pictureBox.Height;
            var ratioX    = (float)maxWidth  / (float)originalWidth;
            var ratioY    = (float)maxHeight / (float)originalHeight;
            var ratio     = Math.Min(ratioX, ratioY);

            // New width and height based on aspect ratio
            int newWidth  = (int)(originalWidth  * ratio);
            int newHeight = (int)(originalHeight * ratio);

            Bitmap newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);          // Convert other formats (including CMYK) to RGB

            using (Graphics graphics = Graphics.FromImage(newImage))                                // Draws the image in the specified size with quality mode set to HighQuality
            {
                graphics.SmoothingMode      = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode  = InterpolationMode.HighQualityBicubic;
               
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            pictureBox.Tag                   = new { Width = pictureBox.Width, Height = pictureBox.Height };
            pictureBox.Width                 = newWidth;
            pictureBox.Height                = newHeight;
            pictureBox.BackColor             = Color.AliceBlue;
            pictureBox.Image                 = newImage;
            pictureBox.BackgroundImageLayout = ImageLayout.None;
        }
    }
}
