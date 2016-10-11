using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace AppConsoleForApp
{
    public enum MyEnum : int
    {
        fieldName0 = 0,
        fieldName2 = 2,
        fieldName4 = 4
    }

    public enum MyEnumTest : byte
    {
        Play,
        Pause,
        PausePlay,
        StopPlay
    }

    class Program
    {
        static void Main(string[ ] args)
        {
            var tempEnum = new MyEnum();

            Console.WriteLine(tempEnum.GetType());
            Console.WriteLine(tempEnum.GetTypeCode());

            var structSbyte = new SByte();

            Console.WriteLine(structSbyte.GetType());

            Console.WriteLine("\n\nТестируем управление мониторами!");

            var screens = Screen.AllScreens;

            var dictionaryByName = new Dictionary<string, int>();

            foreach (Screen screen in screens)
            {
                var deviceName = screen.DeviceName.Substring(4, screen.DeviceName.Length - 4);

                int deviceIndex;
                if (Int32.TryParse(deviceName.Substring(deviceName.Length - 1, 1), out deviceIndex))
                    dictionaryByName[deviceName] = deviceIndex;

                Console.WriteLine("Monitor №{0} with the name is equal to {1} ", deviceIndex, deviceName);
            }

            foreach (var key in dictionaryByName.Keys)
            {
                var screen = Screen.AllScreens[dictionaryByName[key] - 1];
                if (screen.Primary)
                    Console.WriteLine("The primary monitor is {0}", screen.DeviceName);
            }

            Console.WriteLine("\n\nТестируем определение границ");

            var form = new Form();


            var formBounds =  form.DesktopLocation;

            foreach (var key in dictionaryByName.Keys)
            {
                var screenBounds = Screen.AllScreens[dictionaryByName[key] - 1].Bounds;
                var screenWidth  = screenBounds.Width;
                var screenHeight = screenBounds.Height;

                Console.WriteLine("Width  = {0}", screenWidth.ToString());
                Console.WriteLine("Height = {0}", screenHeight.ToString());

                var screenX = screenBounds.Location.X;
                var screenY = screenBounds.Location.Y;

                Console.WriteLine("screen X is {0}", screenX);
                Console.WriteLine("screen Y is {0}", screenY);
            }

            var cmd = MyEnumTest.PausePlay;

            Console.WriteLine(cmd.ToString());

            Console.WriteLine(Screen.FromControl(form).DeviceName);

            Console.ReadLine();
        }

        public static void MyMethod(int param)
        {
 
        }
    }

    public struct ClassWithBaseEnum
    {
 
    }
}
