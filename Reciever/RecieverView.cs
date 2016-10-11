using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

#region Additional references

using System.Net.Sockets;
using WMPLib;
using System.Runtime.Serialization.Formatters.Binary;
using Data  = MediaDataSerialization.DataObject;
using Timer = MediaDataSerialization.Timer;
using MediaDataSerialization;
using System.IO;
using System.Net;
using System.Threading;
using CommonTypes;
using WMPLib;
using MessageWindow;
using HelpfulMethods;
using ExtControlLibrary;

#endregion

namespace Reciever
{
    public partial class RecieverView : Form
    {
        public static Settings settingsMessageWindow { get; set; }

        public List<Form> _childrenListForms;

        public static string Message { get; set; }

        public string _fieldCurrentDisplayName;

        private int _deltaTimerHeight;

        private readonly bool _isLocalStart;

        private class ImageTextShowingForm
        {
            public string text     { get; set; }
            public Stream image    { get; set; }
            public Form   reciever { get; set; }

            public ImageTextShowingForm() : this(null, null, null)
            { 
            }

            public ImageTextShowingForm(string txt, Form rec) : this(txt, null, rec)
            {
            }

            public ImageTextShowingForm(Stream img, Form rec) : this(null, img, rec)
            {
            }

            public ImageTextShowingForm(string txt, Stream img, Form rec)
            {
                text     = txt;
                image    = img;
                reciever = rec;
            }
        }

        private delegate void InvokeControlUIThread();

        private Timer _timeCounter;

        int port = 12000;
        private static readonly string localHostName;            // local
        private static string remoteHostName;                    // remote

        IPAddress localAddr;
        TcpListener server = null;                // Ссылка на сервер
        NetworkStream readerStream;               // Объявили ссылку на транспортный поток

        private readonly WindowsMediaPlayer musicPlayer;          // Для прослушивания музыки
        private readonly WindowsMediaPlayer soundPlayer;          // Для прослушивания звуков
        private readonly List<string>       musicPlaylist;        // Declare a playlist with file names
        private readonly List<string>       soundPlaylist;        // Declare a playlist with file names
        private readonly List<string>       videoPlaylist;        // Declare a playlist with file names

        static RecieverView()
        {
            settingsMessageWindow = new Settings();  // TODO: Rework this snippet code

            localHostName = GetLocalIPAddress();
        }

        public RecieverView()
        {
            InitializeComponent();

            // TODO: Переделать более корректно! Учесть исключения
            var timer = this.Controls.Find("simpleTimerReciver", false).FirstOrDefault();

            _deltaTimerHeight = timer != null ? (timer as SimpleTimer).Height : 0;

            simpleTimerReciver.Location = new Point(this.Width/2 - simpleTimerReciver.Width/2);

            _childrenListForms = new List<Form>();

            musicPlayer = new WindowsMediaPlayer();
            soundPlayer = new WindowsMediaPlayer();
            musicPlayer.PlayStateChange += new _WMPOCXEvents_PlayStateChangeEventHandler(musicPlayer_PlayStateChange);
            soundPlayer.PlayStateChange += soundPlayer_PlayStateChange;
            musicPlaylist = new List<string>();
            soundPlaylist = new List<string>();
            videoPlaylist = new List<string>();

            timerReciever.Stop();
            _timeCounter = new Timer();

            #region Primary settings

            // TODO: Переделать, используя методы в хелпере ScreenManagerHelper
            //ScreenManagerHelper.SetFormLocation(this, Screen.FromControl(this), CheckState.Checked);
            GoFullscreen(true);

            _fieldCurrentDisplayName = ScreenManagerHelper.GetCorrectMonitorNameByFullName(Screen.FromControl(this).DeviceName);

            axWMPOnlyVideo.Visible = false;
            axWMPOnlyVideo.uiMode = "none";
            axWMPOnlyVideo.enableContextMenu = false;

            #endregion
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            // TODO: Rework
            throw new Exception("Local IP Address Not Found!");
        }

        private void soundPlayer_PlayStateChange(int newState)
        {
            // Detect what the player is finished to play a track
            if (newState == (int)WMPPlayState.wmppsMediaEnded)
            {
                //
                //********************* Отправка на Sender **********************
                //

                SendToSenderActionForCurrentAndNextTracks(AutoPlayState.StopTrack, false);
            }
        }

        public RecieverView(bool isLocal) : this()
        {
            _isLocalStart = isLocal;

            if (isLocal)  // Local start
            {
                axWMPOnlyVideo.PlayStateChange -= new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(this.axWMPOnlyVideo_PlayStateChange);
            }
            else         // Remote start
            {
                OpenListnerPort(localHostName, port);
            }
        }

        #region Additional methods

        private void GoFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                this.WindowState     = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Bounds          = Screen.FromControl(this).Bounds;
            }
            else
            {
                this.Bounds          = Screen.FromControl(this).Bounds;
                this.WindowState     = FormWindowState.Maximized;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                this.Icon            = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            }
        }

        #endregion

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
                GoFullscreen(false);

            bool res = base.ProcessCmdKey(ref msg, keyData);
            return res;
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoFullscreen(true);
        }

        private void standartScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GoFullscreen(false);
        }

        private void OpenListnerPort(string hostname, int port)
        {
            // Конвертируем IP в другой формат
            localAddr = IPAddress.Parse(localHostName);

            // Запускаем в новом потоке (ните)
            Thread thread = new Thread(ExecuteLoop);
            thread.Name = "Tcp Listener";
            thread.IsBackground = true;
            thread.Start();
        }

        private void ExecuteLoop()
        {
            try
            {
                server = new TcpListener(localAddr, port);                                  // Создаем сервер-слушатель
                server.Start();                                                             // Запускаем сервер

                // Бесконечный цикл прослушивания очереди клиентов
                while (true)
                {
                    // Проверяем очередь соединений
                    if (!server.Pending())                                                  // Очередь запросов пуста
                        continue;

                    TcpClient client = server.AcceptTcpClient();                            // Текущий клиент

                    //
                    //********************* Получение *********************
                    //

                    // Подключаемся к сокету для чтения
                    readerStream = client.GetStream();

                    // Получить объект данных и десериализовать
                    var formatter = new BinaryFormatter();

                    var data = formatter.Deserialize(readerStream);


                    // Rewrite this code

                    var settings = data as Settings;

                    if (settings != null)
                    {
                        RecieverView.settingsMessageWindow = settings;
                        continue;
                    }

                    var dataReceived = data as Data;

                    if (dataReceived == null)
                    {
                        remoteHostName = (string)data;
                        continue;
                    }

                    //
                    //************ Выполнение действий **********
                    //

                    // Распознаем присланное и исполняем
                    var dirVideo = Path.Combine(Application.StartupPath, @"Video");
                    if (!Directory.Exists(dirVideo))
                        Directory.CreateDirectory(dirVideo);

                    var dirMusic = Path.Combine(Application.StartupPath, @"Music");
                    if (!Directory.Exists(dirMusic))
                        Directory.CreateDirectory(dirMusic);

                    var dirSounds =  Path.Combine(Application.StartupPath, @"Sounds");
                    if (!Directory.Exists(dirSounds))
                        Directory.CreateDirectory(dirSounds);

                    var dirImages =  Path.Combine(Application.StartupPath, @"Images");
                    if (!Directory.Exists(dirImages))
                        Directory.CreateDirectory(dirImages);

                    var dirLog =  Path.Combine(Application.StartupPath, @"RemoteRecieverLogs");
                    if (!Directory.Exists(dirLog))
                        Directory.CreateDirectory(dirLog);

                    var pathLog = Path.Combine(dirLog, @"Log.txt");

                    var message = "";
                    FileStream fs;
                    Byte[] bytes;

                    switch (dataReceived.Command)
                    {
                        case Command.AddTextInLog:                                                          // Клиент хочет сообщение добавить в журнал
                            fs = new FileStream(pathLog, FileMode.Append);
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine("Сервер: " + DateTime.Now.ToString());
                            message = "Получил команду: \"" + dataReceived.DisplayCommand;
                            sw.WriteLine(message + "\"");                                                   // Три конкатенации сразу нельзя
                            sw.WriteLine("Идентификатор команды: " +
                                Enum.GetName(typeof(Command), dataReceived.Command));
                            sw.WriteLine("Получил сообщение: " + dataReceived.Text);
                            message = "Добавлено сообщение в журнал!";
                            sw.WriteLine(message);
                            sw.WriteLine();
                            sw.Close();
                            break;

                        case Command.DeleteLog:                                                             // Клиент хочет удалить журнал с диска сервера
                            if (File.Exists(pathLog))
                                File.Delete(pathLog);
                            break;

                        case Command.BackgroundImage:

                            if (dataReceived.Image != null)
                            {
                                picboxRecievedImage.BackgroundImageLayout = ImageLayout.Stretch;
                                picboxRecievedImage.BackgroundImage       = new Bitmap(dataReceived.Image);
                            }
                            else
                            {
                                picboxRecievedImage.BackColor = Color.Red;
                            }

                            break;

                        case Command.Time:                                                                 // Клиент хочет получить сообщение 

                            /*
                             Компоненты пользовательского интерфейса 
                             не позволяют выполнять каких-либо операций на потоке, 
                             кроме как там, где вы их создали.
                            */
                            if (this.InvokeRequired)
                            {
                                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/e41330b7-aefc-4693-8656-2b816e15ae60/messagebox-in-backgroundworker-thread-gets-hidden?forum=vsto
                                /* Non-UI thread, use Invoke so the callback gets invoked on the UI thread */

                                InvokeControlUIThread StartTimer = delegate()
                                {
                                    _timeCounter.Hours   = dataReceived.Time.Hours;
                                    _timeCounter.Minutes = dataReceived.Time.Minutes;
                                    _timeCounter.Seconds = dataReceived.Time.Seconds;
                                    _timeCounter.command = Command.Time;

                                    simpleTimerReciver.SetTime(_timeCounter.ToString());
                                    //lblTime.Text         = " " + _timeCounter.ToString();

                                    timerReciever.Start();
                                };

                                InvokeControlUIThread PauseOrResetTimer = delegate()
                                {
                                    _timeCounter.Hours   = dataReceived.Time.Hours;
                                    _timeCounter.Minutes = dataReceived.Time.Minutes;
                                    _timeCounter.Seconds = dataReceived.Time.Seconds;
                                    _timeCounter.command = Command.Time;

                                    simpleTimerReciver.SetTime(_timeCounter.ToString());
                                    //lblTime.Text         = " " + _timeCounter.ToString();

                                    timerReciever.Stop();
                                };

                                switch (dataReceived.Time.State)
                                {
                                    case TimerStatus.Play:
                                        this.Invoke(StartTimer);
                                        break;

                                    case TimerStatus.None:
                                    case TimerStatus.Pause:
                                        this.Invoke(PauseOrResetTimer);
                                        break;
                                }
                            }
                            else
                            {
                                //UI thread, all calls should work
                            }
                            break;

                        case Command.CloseAllSecondaryForms:
                            // TODO: Change code

                            axWMPOnlyVideo.Ctlcontrols.stop();
                            //axWMPOnlyVideo.URL = "";
                            axWMPOnlyVideo.Visible = false;

                            // надо переделать так, чтобы вызов назад не шел
                            SendToSenderActionForCurrentAndNextTracks(AutoPlayState.StopVideoTrack, false);

                            foreach (var f in this._childrenListForms)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    f.Opacity -= 0.1;
                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        f.Close();
                                    });
                                }
                            }

                            this._childrenListForms.Clear();

                            Cursor.Show();

                            Message = string.Empty;

                            picboxRecievedImage.Refresh();

                            break;

                        case Command.Text:                                                                // Клиент хочет получить сообщение 

                            //axWMPOnlyVideo.URL = "";
                            axWMPOnlyVideo.Ctlcontrols.stop();
                            axWMPOnlyVideo.Visible = false;

                            SendToSenderActionForCurrentAndNextTracks(AutoPlayState.StopVideoTrack, false);

                            // TODO: Change code
                            foreach (var f in this._childrenListForms)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    f.Opacity -= 0.1;

                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        f.Close();
                                    });
                                }
                            }

                            this._childrenListForms.Clear();

                            Cursor.Show();

                            Message = dataReceived.Text;

                            

                            picboxRecievedImage.Refresh();

                            break;

                        case Command.ResetBgImage:

                            picboxRecievedImage.BackgroundImage = null;

                            //picboxRecievedImage.Refresh();

                            break;

                        case Command.Image:                                                              // Клиент хочет получить рисунок

                            //axWMPOnlyVideo.URL = "";
                            axWMPOnlyVideo.Ctlcontrols.stop();
                            axWMPOnlyVideo.Visible = false;

                            SendToSenderActionForCurrentAndNextTracks(AutoPlayState.StopVideoTrack, false);

                            // TODO: Change code
                            foreach (var f in this._childrenListForms)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    f.Opacity -= 0.1;

                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        f.Close();
                                    });
                                }
                            }

                            this._childrenListForms.Clear();

                            Cursor.Show();

                            Message = string.Empty;

                            picboxRecievedImage.Refresh();

                            ShowMessageWithTextXorImage(this, dataReceived.Image, null);

                            break;

                        case Command.Log:                                                                   // Клиент требует содержимое журнала
                            if (!File.Exists(pathLog))
                            {
                                //dataSend.Text = "Журнал еще не создан";                                     // Отчет для отправки
                                //dataSend.Command = Command.Text;                                            // Изменили команду
                                break;
                            }

                            fs = new FileStream(pathLog, FileMode.Open, FileAccess.Read, FileShare.Read);
                            StreamReader sr = new StreamReader(fs);
                            sr.Close();
                            break;

                        case Command.MusicPlay:                                                                 // Клиент хочет музычку

                            if (dataReceived.MediaData != null)
                            {
                                var musicFullFileName = Path.Combine(dirMusic, dataReceived.MediaDataName);
                                if (!musicPlaylist.Contains(musicFullFileName))
                                {
                                    using (BinaryReader br = new BinaryReader(dataReceived.MediaData))
                                    {
                                        var b = br.ReadBytes((int)dataReceived.MediaData.Length);
                                        File.WriteAllBytes(musicFullFileName, b);
                                        // TODO: It's a bad playlist
                                        musicPlaylist.Add(musicFullFileName);
                                    }
                                }

                                musicPlayer.URL = musicFullFileName;
                                musicPlayer.controls.play();
                            }
                            //  Посылаю музычку                                                             // Отчет для отправки
                            break;

                        case Command.MusicPause:

                            if (musicPlayer != null)
                                musicPlayer.controls.pause();
                            break;

                        case Command.MusicPausePlay:

                            if (musicPlayer != null)
                                musicPlayer.controls.play();
                            break;

                        case Command.MusicStopPlay:

                            if (musicPlayer != null)
                            {
                                musicPlayer.controls.stop();
                                musicPlayer.close();
                            }

                            // TODO: дублирование - плохо
                            if (dataReceived.MediaData != null)
                            {
                                var musicFullFileName = Path.Combine(dirMusic, dataReceived.MediaDataName);
                                if (!musicPlaylist.Contains(musicFullFileName))
                                {
                                    using (BinaryReader br = new BinaryReader(dataReceived.MediaData))
                                    {
                                        var b = br.ReadBytes((int)dataReceived.MediaData.Length);
                                        File.WriteAllBytes(musicFullFileName, b);
                                        // TODO: It's a bad playlist
                                        musicPlaylist.Add(musicFullFileName);
                                    }
                                }

                                musicPlayer.URL = musicFullFileName;
                                musicPlayer.controls.play();
                            }
                            break;

                        case Command.MusicStop:

                            if (musicPlayer != null)
                            {
                                musicPlayer.controls.stop();
                                musicPlayer.URL = "";
                            }
                            break;

                        case Command.VideoPlay:                                                                 // Клиент хочет видос (пока не понял, когда используется

                            // TODO: Change code
                            /*
                            foreach (var f in this._childrenListForms)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    f.Opacity -= 0.1;

                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        f.Close();
                                    });
                                }
                            }

                            this._childrenListForms.Clear();

                            Cursor.Show();

                            Message = string.Empty;

                            picboxRecievedImage.Refresh();
                            */

                            /////////////////////////////////////////

                            if (dataReceived.MediaData != null)
                            {
                                //axWMPOnlyVideo.uiMode = "none";

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        axWMPOnlyVideo.Size = new Size(simpleTimerReciver.Width, simpleTimerReciver.Width);
                                        axWMPOnlyVideo.Location = new Point((this.Width - axWMPOnlyVideo.Width) / 2, (this.Height - axWMPOnlyVideo.Height) / 2 + simpleTimerReciver.Height / 2);
                                    });
                                }

                                var videoFullFileName = Path.Combine(dirVideo, dataReceived.MediaDataName);
                                if (!videoPlaylist.Contains(videoFullFileName))
                                {
                                    using (BinaryReader br = new BinaryReader(dataReceived.MediaData))
                                    {
                                        var b = br.ReadBytes((int)dataReceived.MediaData.Length);
                                        File.WriteAllBytes(videoFullFileName, b);
                                        // TODO: It's a bad playlist
                                        videoPlaylist.Add(videoFullFileName);
                                    }
                                }

                                axWMPOnlyVideo.URL = videoFullFileName;

                                axWMPOnlyVideo.Ctlcontrols.play();
                            }
                          
                            break;

                        case Command.VideoPause:

                            if (axWMPOnlyVideo != null)
                            {
                                axWMPOnlyVideo.Ctlcontrols.pause();
                            }
                            break;

                        case Command.VideoPausePlay:

                            // TODO: Change code
                            
                            foreach (var f in this._childrenListForms)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    f.Opacity -= 0.1;

                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        f.Close();
                                    });
                                }
                            }

                            this._childrenListForms.Clear();

                            Cursor.Show();

                            Message = string.Empty;

                            picboxRecievedImage.Refresh();

                            
                            /////////////////////////////////////////

                            if (axWMPOnlyVideo != null)
                            {
                                axWMPOnlyVideo.Ctlcontrols.play();
                            }
                            break;

                        case Command.VideoStopPlay:

                            // TODO: Change code
                            
                            foreach (var f in this._childrenListForms)
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    Thread.Sleep(100);
                                    f.Opacity -= 0.1;

                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        f.Close();
                                    });
                                }
                            }

                            this._childrenListForms.Clear();

                            Cursor.Show();

                            Message = string.Empty;

                            picboxRecievedImage.Refresh();

                            
                            /////////////////////////////////////////

                            if (axWMPOnlyVideo != null)
                            {
                                axWMPOnlyVideo.Ctlcontrols.stop();
                            }

                            // TODO: дублирование - плохо
                            if (dataReceived.MediaData != null)
                            {
                                var videoFullFileName = Path.Combine(dirVideo, dataReceived.MediaDataName);
                                if (!videoPlaylist.Contains(videoFullFileName))
                                {
                                    using (BinaryReader br = new BinaryReader(dataReceived.MediaData))
                                    {
                                        var b = br.ReadBytes((int)dataReceived.MediaData.Length);
                                        File.WriteAllBytes(videoFullFileName, b);
                                        // TODO: It's a bad playlist
                                        videoPlaylist.Add(videoFullFileName);
                                    }
                                }

                                if (this.InvokeRequired)
                                {
                                    this.Invoke((MethodInvoker)delegate()
                                    {
                                        axWMPOnlyVideo.Size = new Size(simpleTimerReciver.Width, simpleTimerReciver.Width); // (int)((formReceiverView.Height - timer.Height) * 0.75)
                                        axWMPOnlyVideo.Location = new Point((this.Width - axWMPOnlyVideo.Width) / 2, (this.Height - axWMPOnlyVideo.Height) / 2 + simpleTimerReciver.Height / 2);
                                    });
                                }
                                

                                axWMPOnlyVideo.URL = videoFullFileName;
                                axWMPOnlyVideo.Ctlcontrols.play();
                                
                            }
                            break;

                        case Command.VideoStop:

                            if (axWMPOnlyVideo != null)
                            {
                                axWMPOnlyVideo.Ctlcontrols.stop();
                            }
                            break;
                        ///////////////////////////

                        case Command.SoundPlay:                                                                 // Клиент хочет музычку

                            if (dataReceived.MediaData != null)
                            {
                                var soundFullFileName = Path.Combine(dirSounds, dataReceived.MediaDataName);
                                if (!soundPlaylist.Contains(soundFullFileName))
                                {
                                    using (BinaryReader br = new BinaryReader(dataReceived.MediaData))
                                    {
                                        var b = br.ReadBytes((int)dataReceived.MediaData.Length);
                                        File.WriteAllBytes(soundFullFileName, b);
                                        // TODO: It's a bad playlist
                                        soundPlaylist.Add(soundFullFileName);
                                    }
                                }

                                soundPlayer.URL = soundFullFileName;
                                soundPlayer.controls.play();
                            }
                            //  Посылаю музычку                                                             // Отчет для отправки
                            break;

                        case Command.SoundPause:

                            if (soundPlayer != null)
                                soundPlayer.controls.pause();
                            break;

                        case Command.SoundPausePlay:

                            if (soundPlayer != null)
                                soundPlayer.controls.play();
                            break;

                        case Command.SoundStopPlay:

                            if (soundPlayer != null)
                            {
                                soundPlayer.controls.stop();
                                soundPlayer.close();
                            }

                            // TODO: дублирование - плохо
                            if (dataReceived.MediaData != null)
                            {
                                var soundFullFileName = Path.Combine(dirSounds, dataReceived.MediaDataName);
                                if (!soundPlaylist.Contains(soundFullFileName))
                                {
                                    using (BinaryReader br = new BinaryReader(dataReceived.MediaData))
                                    {
                                        var b = br.ReadBytes((int)dataReceived.MediaData.Length);
                                        File.WriteAllBytes(soundFullFileName, b);
                                        // TODO: It's a bad playlist
                                        soundPlaylist.Add(soundFullFileName);
                                    }
                                }

                                soundPlayer.URL = soundFullFileName;
                                soundPlayer.controls.play();
                            }
                            break;

                        case Command.SoundStop:

                            if (soundPlayer != null)
                            {
                                soundPlayer.controls.stop();
                                soundPlayer.URL = "";
                            }
                            break;

                        case Command.StopSoundAndDeleteBackgroundImage:

                            if (musicPlayer != null)
                            {
                                musicPlayer.controls.stop();
                                musicPlayer.URL = "";
                            }

                            if (soundPlayer != null)
                            {
                                soundPlayer.controls.stop();
                                soundPlayer.URL = "";
                            }

                            if (axWMPOnlyVideo != null)
                            {
                                
                                axWMPOnlyVideo.Ctlcontrols.stop();
                                axWMPOnlyVideo.Visible = false; //???
                            }

                            picboxRecievedImage.BackgroundImage = null;
                            simpleTimerReciver.SetTime("00:00:00");

                            break;
                    }
                    client.Close();                                                                         // Освобождаем сокет текущего клиента
                }
            }
            finally
            {
                timerReciever.Stop();

                if (musicPlayer != null)
                    musicPlayer.close();

                // Останавливаем сервер
                server.Stop();
            }
        }

        private void timerReciever_Tick(object sender, EventArgs e)
        {
            _timeCounter.Seconds = _timeCounter.Seconds - 1;
            simpleTimerReciver.SetTime(_timeCounter.ToString());
        }

        private void musicPlayer_PlayStateChange(int newState)
        {
            // Detect what the player is finished to play a track
            if (newState == (int)WMPPlayState.wmppsMediaEnded)
            {
                //
                //********************* Отправка на Sender **********************
                //

                SendToSenderActionForCurrentAndNextTracks(AutoPlayState.NextTrack, false);
            }
        }

        public void ShowMessageWithTextXorImage(RecieverView form, Stream streamImage = null, string messageText = null)
        {
            if (streamImage == null && messageText == null)
                return;

            ImageTextShowingForm message;

            if (streamImage == null)
                message = new ImageTextShowingForm(messageText, form);
            else
                message = new ImageTextShowingForm(streamImage, form);

            var threadShowText = new Thread(new ParameterizedThreadStart(OperationWithForm));
            threadShowText.Name = "Thread: Show a form with the recieved text";
            threadShowText.IsBackground = true;

            threadShowText.Start(message);
        }

        private static void OperationWithForm(object parameters)
        {
            var data     = parameters    as ImageTextShowingForm;
            var reciever = data.reciever as RecieverView;
            var image    = data.image;
            var text     = data.text;

            var formMessageShow = new MessageWindowForm(text, settingsMessageWindow);
            reciever._childrenListForms.Add(formMessageShow);

            if (image != null)
            {
                formMessageShow.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                formMessageShow.AllowTransparency = true;
                formMessageShow.TransparencyKey   = formMessageShow.BackColor;  //он же будет заменен на прозрачный цвет
            }

            formMessageShow.Opacity = 0d;

            Cursor.Hide();
            if (reciever.InvokeRequired)
            {
                reciever.Invoke((MethodInvoker)delegate()
                {
                    var screenForShow = ScreenManagerHelper.GetScreenByNameInListMonitorsLocalMachine(reciever._fieldCurrentDisplayName);

                    var bitmapImage                 = new Bitmap(image);
                    var koef                        = (float)bitmapImage.Size.Width / (float)bitmapImage.Size.Height;
                    formMessageShow.BackgroundImage = bitmapImage;
                    formMessageShow.Size            = ScreenManagerHelper.GetSizeForImageMessageByBitmapAndScreen(screenForShow, bitmapImage, koef, reciever._deltaTimerHeight);

                    ScreenManagerHelper.SetFormLocationWithoutMode(formMessageShow, screenForShow, reciever._deltaTimerHeight);

                    for (int i = 0; i < 10; i++)
                    {
                        Thread.Sleep(10);
                        formMessageShow.Opacity += 0.1;
                    }
                });
            }
        }

        private void RecieverView_Load(object sender, EventArgs e)
        {
            simpleTimerReciver.Parent    = picboxRecievedImage;
            simpleTimerReciver.BackColor = Color.Transparent;
        }

        private void RecieverView_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendToSenderActionForCurrentAndNextTracks(AutoPlayState.NothingIsPlaying, false);
        }

        private void RecieverView_Resize(object sender, EventArgs e)
        {
            simpleTimerReciver.Location = new Point(this.Width / 2 - simpleTimerReciver.Width / 2);
            this.Refresh();
        }

        private void picboxRecievedImage_Paint(object sender, PaintEventArgs e)
        {
            if (Message != null)
            {
                var brush = (Brush)new SolidBrush(Color.FromName(settingsMessageWindow.ForeColorText));
                TextManager.DrawTextInForm(Message, settingsMessageWindow.GetFontForMessage(), brush, e);
            }
        }

        private void picboxRecievedImage_Resize(object sender, EventArgs e)
        {
            var picbox = sender as PictureBox;

            picbox.Refresh();
        }

        /// <summary>
        /// Код внутри нужно вынести в отдельный метод-сервис отправки состояния проигрывателя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void axWMPOnlyVideo_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            var videoPlayer = sender as AxWMPLib.AxWindowsMediaPlayer;

            if (e.newState == 3)
            {
                //videoPlayer.uiMode = "none";    // без этого условия то работает полноэкранный режим, то нет
                videoPlayer.fullScreen = true;
                videoPlayer.Visible = true;
            }

            // Detect what the player is finished to play a track
            if (e.newState == (int)WMPPlayState.wmppsMediaEnded && !_isLocalStart)
            {
                videoPlayer.fullScreen = false;
                videoPlayer.Visible = false;

                SendToSenderActionForCurrentAndNextTracks(AutoPlayState.StopVideoTrack, false);
            }
        }

        private void SendToSenderActionForCurrentAndNextTracks(AutoPlayState actionAfterEndedTrack, bool withMessage)
        {
            try
            {
                TcpClient clientRecieverToSender = new TcpClient(remoteHostName, 13000);  // мы условились, что этот порт не занимать (нужно так, чтобы пользователь не указывал этот порт - иначе креш)

                if (clientRecieverToSender == null || clientRecieverToSender.Connected == false)
                {
                    MessageBox.Show("Not data for sending");
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();

                // место для параметра
                var autoPlay = new AutoPlay(actionAfterEndedTrack);
                using (Stream writerStream = clientRecieverToSender.GetStream())
                {
                    formatter.Serialize(writerStream, autoPlay);
                }

                clientRecieverToSender.Close();
            }
            catch (Exception ex)
            {
                if (withMessage == true)
                    MessageBox.Show("A server is not found!");

                return;
            }
        }
    }
}