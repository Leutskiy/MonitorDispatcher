using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sender
{
    using Sender.Model;
    using Sender.View;
    using Sender.Presenter;

    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Models
            LocalMonitorManager modelLocalMonitor = new LocalMonitorManager();
            RemoteConnectionManager modelRemoteConnection = new RemoteConnectionManager();

            // View
            ViewSender view = new ViewSender();

            //Presenter
            PresenterSender presenter = new PresenterSender(view, modelLocalMonitor, modelRemoteConnection);

            Application.Run(view);
        }
    }
}
