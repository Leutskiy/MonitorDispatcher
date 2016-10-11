using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Helpers
{
    public static class FileManager
    {
        /// <summary>
        /// Get the Filter string for all supported image types.
        /// This can be used directly to the FileDialog class Filter Property.
        /// </summary>
        /// <returns>A string for all supported image types.</returns>
        public static string GetImageFilter()
        {
            StringBuilder allImageExtensions = new StringBuilder();
            string separator = "";
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            Dictionary<string, string> images = new Dictionary<string, string>();
            foreach (ImageCodecInfo codec in codecs)
            {
                allImageExtensions.Append(separator);
                allImageExtensions.Append(codec.FilenameExtension);
                separator = ";";
                images.Add(string.Format("{0} Files: ({1})", codec.FormatDescription, codec.FilenameExtension),
                           codec.FilenameExtension);
            }
            StringBuilder sb = new StringBuilder();
            if (allImageExtensions.Length > 0)
            {
                sb.AppendFormat("{0}|{1}", "All Images", allImageExtensions.ToString());
            }
            images.Add("All Files", "*.*");
            foreach (KeyValuePair<string, string> image in images)
            {
                sb.AppendFormat("|{0}|{1}", image.Key, image.Value);
            }
            return sb.ToString();
        }

        public static string GetAudioFilter()
        {
            var fileExtensions = @"All Supported Audio | *.mp3; *.wma| MP3s | *.mp3| WMAs | *.wma";

            return fileExtensions;
        }

        public static string GetVideoFilter()
        {
            var allVideoFormats = "*.avi; *.wma; *.mp4; *.wav; *.flv; *.swf; *.wmv; *.dv; *.mpg; *.ogg; *.mov; *.3gp; *.mjpeg; *.gif; *.dvd";

            var fileExtensions = @"All Supported Video |" + allVideoFormats + "| AVIs | *.avi| WMAs | *.wma| MP4s | *.mp4| WAVs | *.wav| FLVs | *.flv| SWFs | *.swf| WMVs | *.wmv| DVs | *.dv| MPGs | *.mpg | OGGs | *.ogg| MOVs | *.mov| 3GPs | *.3gp| MJPEGs | *.mjpeg| GIFs | *.gif| DVDs | *.dvd";

            return fileExtensions;
        }
    }
}
