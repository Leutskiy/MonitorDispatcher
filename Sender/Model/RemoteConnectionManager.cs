using System;
using System.Collections.Generic;
using System.Text;

// Дополнительные пространства имен для ADO.NET
using System.Data;

// Дополнительные пространства имен
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using System.Collections;
using System.Configuration;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using WMPLib;

// Своя библиотека
using Data = MediaDataSerialization.DataObject;
using Timer = MediaDataSerialization.Timer;
using MediaDataSerialization;
using CommonTypes;

namespace Sender.Model
{
    public interface IRemoteConnectionManager
    {
        void   OpenConnection();
        void   OpenConn(string host, int port);
        string OpenConnection(string host, int port);
        void   SendData(Timer timer, Command command);
        void   SendData(string message, string imageFileName, string mediaDataFileName, Command command);
        void   SendSettings(Settings settings, Command command);
        void   SendBackgroundImage(string imageFileName, Command command);
        bool   SendIPAddressSender(string ipSender);
    }

    public class RemoteConnectionManager : IRemoteConnectionManager
    {
        private const String hostName       = "192.168.1.14"; // localhost
        private const int port              = 12000;          // port

        private TcpClient client            = null;           // Ссылка на клиента
        private NetworkStream writerStream  = null;           // Объявили ссылки на транспортный поток

        private void ConnectionConstant(object args)
        {
            TcpClient cl = (TcpClient)args;

            while(cl.Connected)
            {
                //Thread.Sleep(20000);
                MessageBox.Show(cl.SendTimeout.ToString());
            }
        }

        public void OpenConnection()
        {
            try
            {
                // Создать клиента
                client = new TcpClient(hostName, port);
                
                //Thread connThread = new Thread(new ParameterizedThreadStart(ConnectionConstant));
                //connThread.IsBackground = true;
                //connThread.Start(client);
            }
            catch
            {
                // todo: генерировать свое исключение
                //throw;
            }
        }

        public void OpenConn(string host, int port)
        {
            client = new TcpClient(host, port);
        }

        public string OpenConnection(string host, int port)
        {
            try
            {
                //TODO: Перенести строки в числа в какое-нить перечисление

                /* Создать клиента */
                if (client == null || !client.Connected)
                {
                    client = new TcpClient(host, port);
                    return "Successfully!";
                }
                else if (client.Connected)
                {
                    return "Successfully!";
                }

                return "Not Successfully!";
            }
            catch
            {
                return "Not Successfully!";
                // todo: генерировать свое исключение
                throw;
            }
        }

        public void SendData(string message, string imageFileName, string mediaDataFileName, Command command)
        {
            if (client == null) return;

            //
            //********************* Отправка *********************
            //

            BinaryFormatter formatter = new BinaryFormatter();

            // Собираем данные с элементов управления для отправки через объект
            Data dataSend = new Data();
            dataSend.Command = (Command)command;

            if (string.IsNullOrEmpty(message + imageFileName + mediaDataFileName)) goto loopNowSendCommand;

            dataSend.Text = string.IsNullOrEmpty(message) ? string.Empty : message.Trim();    // Берем из текстового поля Message

            // Обнуляем ссылки на потоки
            dataSend.Image     = null;
            dataSend.MediaData = null;

            // Имена зададим жестко в коде, хотя можно 
            // сделать диалог опроса пользователя
            dataSend.LogName   = @"Log.txt";                    // Имя журнала, локализация на сервере C:\Server
            dataSend.ImageName = imageFileName;                 // Разместить в каталоге сборки сервера

            if (!string.IsNullOrEmpty(mediaDataFileName))
            {
                var startIndexMediaDataFileName = mediaDataFileName.LastIndexOf(@"\") + 1;
                var countChars = mediaDataFileName.Length - startIndexMediaDataFileName;
                dataSend.MediaDataName = mediaDataFileName.Substring(startIndexMediaDataFileName, countChars);                 // Разместить в каталоге сборки сервера
            }

            Byte[] bytes;
            switch (command)
            {
                case Command.AddTextInLog:                      // Клиент хочет добавить сообщение в журнал
                    break;
                case Command.DeleteLog:                         // Клиент хочет удалить журнал с диска сервера
                    break;
                case Command.Text:                              // Клиент хочет получить сообщение 
                    break;
                case Command.BackgroundImage:
                case Command.Image:                             // Клиент хочет получить рисунок
                    // Файл должен быть в каталоге сборки, 
                    // иначе надо дополнить путем
                    if (!File.Exists(imageFileName))
                    {
                        // Рисунок не существует
                        break;
                    }
                    // Читаем файл с рисунком
                    bytes = File.ReadAllBytes(imageFileName);
                    // Подключаем поток к свойству объекта
                    dataSend.Image = new MemoryStream(bytes);
                    break;
                case Command.Log:                               // Клиент требует содержимое журнала
                    break;

                case Command.VideoPlay:
                case Command.VideoStopPlay:
                case Command.VideoPause: 
                case Command.SoundPlay:
                case Command.SoundStopPlay:
                case Command.SoundPause: 
                case Command.MusicPlay:
                case Command.MusicStopPlay:
                case Command.MusicPause:                             // Клиент хочет медиа контент
                    // Файл должен быть в каталоге сборки, 
                    // иначе надо дополнить путем
                    if (!File.Exists(mediaDataFileName))
                    {
                        // Файл с медиа данными не существует
                        break;
                    }

                    // Читаем файл с медиа содержимым
                    bytes = File.ReadAllBytes(mediaDataFileName);
                    // Подключаем поток к свойству объекта
                    dataSend.MediaData = new MemoryStream(bytes);
                    break;

                case Command.ResetBgImage:
                case Command.StopSoundAndDeleteBackgroundImage: // непонятно, что с этим делать???
                    break;
            }

            // Если ничего не передаем, кроме команды, то сразу отправляем на ресивер
            loopNowSendCommand:

            // Подключаем транспортный поток для отправки
            writerStream = client.GetStream();

            // Сериализуем объект в транспортный поток и отправляем          
            formatter.Serialize(writerStream, dataSend);        // Отправленный объект
            writerStream.Close();                               // Освобождаем поток
            client.Close();                                     // Освобождаем сокет клиента
        }


        public void SendData(Timer timer, Command command)
        {
            if (client == null) return;

            //
            //********************* Отправка *********************
            //

            BinaryFormatter formatter = new BinaryFormatter();

            // Собираем данные с элементов управления для отправки через объект
            Data dataSend = new Data();
            dataSend.Command = (Command)command;

            dataSend.Time = new Timer();
            dataSend.Text = String.Empty;
            // Обнуляем ссылки на потоки
            dataSend.Image = null;
            dataSend.MediaData = null;

            // Имена зададим жестко в коде, хотя можно 
            // сделать диалог опроса пользователя
            dataSend.LogName   = @"Log.txt";                    // Имя журнала, локализация на сервере C:\Server
            dataSend.ImageName = String.Empty;                 // Разместить в каталоге сборки сервера
            dataSend.MediaDataName = String.Empty;                 // Разместить в каталоге сборки сервера

                                   
            switch (command)
            {
                case Command.Time:                             // Клиент хочет получить время
                    dataSend.Time = timer;
                    break;
                default:
                    break;
            }

            // Подключаем транспортный поток для отправки
            writerStream = client.GetStream();

            // Сериализуем объект в транспортный поток и отправляем 
            formatter.Serialize(writerStream, dataSend);        // Отправленный объект
            writerStream.Close();                               // Освобождаем поток
            client.Close();                                     // Освобождаем сокет клиента
        }

        public void SendSettings(Settings settings, Command command)
        {
            if (client == null) return;

            //
            //********************* Отправка *********************
            //

            BinaryFormatter formatter = new BinaryFormatter();

            // Собираем данные с элементов управления для отправки через объект
            var dataSend = new Settings();

            Byte[] bytes;
            switch (command)
            {
                case Command.Settings:
                    dataSend = settings;
                    break;
            }

            // Подключаем транспортный поток для отправки
            writerStream = client.GetStream();


            // Сериализуем объект в транспортный поток и отправляем 

            formatter.Serialize(writerStream, dataSend);        // Отправленный объект


            writerStream.Close();                               // Освобождаем поток
            client.Close();                                     // Освобождаем сокет клиента
        }

        public void SendBackgroundImage(string imageFileName, Command command)
        {
            SendData(null, imageFileName, null, command);
        }

        public bool SendIPAddressSender(string ipSender)
        {
            if (client == null) return false;

            //
            //********************* Отправка *********************
            //

            BinaryFormatter formatter = new BinaryFormatter();

            // Собираем данные с элементов управления для отправки через объект
            var dataSend = ipSender;

            // Подключаем транспортный поток для отправки
            writerStream = client.GetStream();

            // Сериализуем объект в транспортный поток и отправляем 

            try
            {
                formatter.Serialize(writerStream, dataSend);        // Отправленный объект
                return true;
            }
            finally
            {
                writerStream.Close();                               // Освобождаем поток
                client.Close();                                     // Освобождаем сокет клиента
            }
        }
    }
}
