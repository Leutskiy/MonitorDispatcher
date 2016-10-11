using System;
using System.Runtime.InteropServices;

namespace Sender.Win32
{
    public static class Win32API
    {
        /// <summary>
        /// Creates the round rect RGN.
        /// </summary>
        /// <param name="nLeftRect">The n left rect.</param>
        /// <param name="nTopRect">The n top rect.</param>
        /// <param name="nRightRect">The n right rect.</param>
        /// <param name="nBottomRect">The n bottom rect.</param>
        /// <param name="nWidthEllipse">The n width ellipse.</param>
        /// <param name="nHeightEllipse">The n height ellipse.</param>
        /// <returns></returns>
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,          // x-coordinate of upper-left corner
            int nTopRect,           // y-coordinate of upper-left corner
            int nRightRect,         // x-coordinate of lower-right corner
            int nBottomRect,        // y-coordinate of lower-right corner
            int nWidthEllipse,      // height of ellipse
            int nHeightEllipse      // width of ellipse
         );

        /// <summary>
        /// Dispose an created object
        /// </summary>
        /// <param name="hObject">An created object</param>
        /// <returns>A result of deleting</returns>
        [DllImport("Gdi32.dll", EntryPoint = "DeleteObject")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}