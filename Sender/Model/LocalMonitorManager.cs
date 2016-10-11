using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace Sender.Model
{
    public interface ILocalMonitorManager
    {
            byte NumberPrimaryMonitor  { get; set; }
            byte NumberSelectedMonitor { get; set; }
            List<string> Monitors      { get; set; }
            byte CountMonitors         { get; set; }
            bool FullscreenMode        { get; set; }
            List<Screen> Screens       { get; set; }
            List<string> GetListMonitors();
            void SetFormOnChoosenMonitor(Screen screen, Form form, CheckState stateFullscreenOrNot);
    }

    public class LocalMonitorManager : ILocalMonitorManager
    {
        public byte NumberPrimaryMonitor  { get; set; }

        public byte NumberSelectedMonitor { get; set; }

        public List<string> Monitors      { get; set; }

        public List<Screen> Screens       { get; set; }

        public byte CountMonitors         { get; set; }

        public bool FullscreenMode        { get; set; }

        public LocalMonitorManager()
        {
            NumberPrimaryMonitor  = 0;
            NumberSelectedMonitor = 0;
            Monitors              = new List<string>();
            Screens               = new List<Screen>();
            CountMonitors         = 0;
            FullscreenMode        = true;
        }

        public List<string> GetListMonitors()
        {
            CountMonitors = checked((byte)Screen.AllScreens.Length);

            for (byte i = 0; i < CountMonitors; ++i)
            {
                if (Screen.AllScreens[i].Primary)
                {
                    NumberPrimaryMonitor = i;
                    NumberSelectedMonitor = i;
                    Monitors.Add("Primary:    " + Screen.AllScreens[i].DeviceName.Replace(@"\\.\", ""));
                    Screens.Add(Screen.AllScreens[i]);
                }
                else
                {
                    Monitors.Add("Secondary: " + Screen.AllScreens[i].DeviceName.Replace(@"\\.\", ""));
                    Screens.Add(Screen.AllScreens[i]);
                }
            }

            return Monitors;
        }

        public void SetFormOnChoosenMonitor( Screen screen, Form form, CheckState stateFullscreenOrNot)
        {
            // Show the form
            form.Show();

            // Maximize the form on the monitor's display in the Fullscreen mode
            GoFullscreen(screen, form, stateFullscreenOrNot);
        }

        private void GoFullscreen(Screen screen, Form form, CheckState stateFullscreenOrNot)
        {
            switch (stateFullscreenOrNot)
            {
                case CheckState.Checked:
                    {
                        form.WindowState = FormWindowState.Normal;
                        form.FormBorderStyle = FormBorderStyle.None;
                        form.Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
                        form.Bounds = screen.Bounds;                       // устанавливаем форму на монитор (важно, что в конце!)
                    }
                    break;
                case CheckState.Unchecked:
                    {
                        form.Bounds = screen.Bounds;                       // устанавливаем форму на монитор (важно, что в начале!)
                        form.WindowState = FormWindowState.Maximized;
                        form.FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    break;
            }
        }
    }
}
