using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace HelpfulMethods
{
    public static class ScreenManagerHelper
    {
        /// <summary>
        /// Возвращает номер монитора по его наименованию
        /// </summary>
        /// <param name="monitorName">Наименование монитора</param>
        /// <returns>Индекс монитора в списке всех доступных мониторов машины</returns>
        public static int GetNumberScreenByName(string monitorName)
        {
            int deviceIndex;
            if (Int32.TryParse(monitorName.Substring(monitorName.Length - 1, 1), out deviceIndex))
                return deviceIndex - 1;

            return -1;
        }

        // TODO: Возможно, что наименование зависит от типа либо версии ОС
        /// <summary>
        /// Возвращает наименование монитора в формате DISPLAY{№} по его наименованию в ОС
        /// </summary>
        /// <param name="fullMonitorName">Полное наименование монитора в ОС</param>
        /// <returns>Наименование монитора в формате DISPLAY{№}</returns>
        public static string GetCorrectMonitorNameByFullName(string fullMonitorName)
        {
            return fullMonitorName.Substring(4, fullMonitorName.Length - 4);
        }

        /// <summary>
        /// Отображает форму на указанном мониторе в полноэкранном/стандартном режиме
        /// </summary>
        /// <param name="form">Форма, которую требуется отобразить</param>
        /// <param name="screen">Монитор, на котором должна быть отображена форма</param>
        public static void SetFormLocation(Form form, Screen screen, CheckState checkBoxMinimizeMaximize = CheckState.Unchecked)
        {
            // Показать форму
            form.Show();

            // Развернуть/Свернуть форму
            if (checkBoxMinimizeMaximize == CheckState.Checked)
                GoFullscreen(form, screen, true);
            else if (checkBoxMinimizeMaximize == CheckState.Unchecked)
                GoFullscreen(form, screen, false);
        }

        public static void SetFormLocationWithoutMode(Form form, Screen screen, int delta)
        {
            // Показать форму
            form.Show();

            GoFullscreen(form, screen, delta);
        }

        /// <summary>
        /// Устанавливает размер формы в одном из вариантов: либо полноэкранный размер, либо стандартный (развернутый) режим
        /// </summary>
        /// <param name="form">Форма, которую следует установить в нужный режим</param>
        /// <param name="screen">Монитор, на который следует замостить форму</param>
        /// <param name="fullscreen">Режим</param>
        public static void GoFullscreen(Form form, Screen screen, bool fullscreen)
        {
            if (fullscreen)
            {
                form.WindowState     = FormWindowState.Normal;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Bounds          = screen.Bounds;               // устанавливаем форму на монитор (важно, что в конце!)
            }
            else
            {
                form.Bounds          = screen.Bounds;               // устанавливаем форму на монитор (важно, что в начале!)
                form.WindowState     = FormWindowState.Maximized;
                form.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        public static void GoFullscreen(Form form, Screen screen, int delta)
        {
            var sizeForm = form.Size;
            var xForm    = (screen.Bounds.X + screen.Bounds.Width / 2) - (sizeForm.Width) / 2;
            var yForm    = delta / 2 + (screen.Bounds.Height / 2 - sizeForm.Height / 2);

            form.WindowState     = FormWindowState.Normal;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Bounds          = screen.Bounds;         // а это вообще нужно?
            form.Bounds          = new Rectangle(xForm, yForm, sizeForm.Width, sizeForm.Height);
            form.StartPosition   = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// Получаем монитор по заданному имени
        /// </summary>
        /// <param name="deviceName">Наименование монитора в системе</param>
        /// <returns>Монитор, которому соответствует переданное наименование устройства</returns>
        public static Screen GetScreenByNameInListMonitorsLocalMachine(string deviceName)
        {
            var deviceIndex = GetNumberScreenByName(deviceName);

            if (deviceIndex != -1)
                return Screen.AllScreens[deviceIndex];

            return Screen.PrimaryScreen;
        }

        /// <summary>
        /// Получаем монитор по заданному индексу
        /// </summary>
        /// <param name="deviceIndex">Индекс монитора в системе</param>
        /// <returns>Монитор, которому соответствует переданный индекс устройства</returns>
        public static Screen GetScreenByIndexInListMonitorsLocalMachine(int deviceIndex)
        {
            if (deviceIndex != -1)
                return Screen.AllScreens[deviceIndex];

            return Screen.PrimaryScreen;
        }

        public static Size GetSizeForImageMessageByBitmapAndScreen(Screen screen, Bitmap bitmapImage, float relationWidthHeight, int delta)
        {
            var heightScreen     = screen.Bounds.Height - delta - 20;
            var widthScreen      = screen.Bounds.Width;
            var halfWidthScreen  = screen.Bounds.Width  / 2;
            var halfHeightScreen = screen.Bounds.Height / 2;

            var sizeBitmapWidth  = bitmapImage.Size.Width;
            var sizeBitmapHeight = bitmapImage.Size.Height;

            while (sizeBitmapHeight > heightScreen) sizeBitmapHeight--;

            sizeBitmapWidth = (int) (sizeBitmapHeight * relationWidthHeight);

            return new Size(sizeBitmapWidth, sizeBitmapHeight);
        }
    }
}
