using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AxWMPLib;

namespace Sender.Presenter
{
    using Sender.Model;
    using Sender.View;
    using HelpfulMethods;
    using MediaDataSerialization;
    using Reciever;
    using CommonTypes;
    using Sender.EventArguments;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using WMPLib;
    using Sender.EXControls;

    public class PresenterSender
    {
        #region Model

        private readonly ILocalMonitorManager     _modelLocalMonitorManager;
        private readonly IRemoteConnectionManager _modelRemoteConnectionManager;

        #endregion

        #region View

        private readonly ISenderView _viewSender;

        #endregion

        #region Presenter Properties

        private readonly List<Form> _listRecieverView;

        #endregion

        public PresenterSender(ISenderView view, ILocalMonitorManager modelLocalMonitor, IRemoteConnectionManager modelRemoteConnection)
        {
            view.Presenter = this;
            this._viewSender = view;
            this._modelLocalMonitorManager = modelLocalMonitor;
            this._modelRemoteConnectionManager = modelRemoteConnection;

            _listRecieverView = new List<Form>();
        }

        public void OnButtonSend(object sender, EventArgs eventArgs)
        {
            var message           = _viewSender.DataSending.Message;
            var imageFileName     = _viewSender.DataSending.ImageFileName;
            var mediaDataFileName = _viewSender.DataSending.MediaDataFileName;
            var command           = _viewSender.DataSending.Command;
            var settings          = _viewSender.ConfigurationSettings;

            //_viewSender.DataSending.Message       = !string.IsNullOrEmpty(message)       ? string.Empty : message;
            //_viewSender.DataSending.ImageFileName = !string.IsNullOrEmpty(imageFileName) ? string.Empty : imageFileName;
            _viewSender.DataSending.MediaDataFileName = !string.IsNullOrEmpty(mediaDataFileName) ? string.Empty : mediaDataFileName;
            _viewSender.DataSending.Command       = Command.None;

            try
            {
                //_modelRemoteConnectionManager.OpenConnection();
                // The connection is opened!
                //MessageBox.Show(message.ToString() + " / " + imageFileName + " / " +  musicFileName + " / " + command.ToString());
                _modelRemoteConnectionManager.SendData(message, imageFileName, mediaDataFileName, command);
                //MessageBox.Show("Error!");
                // The image was send!
            }
            catch(Exception e)
            {
                // todo: Создать свое исключение и генерировать его в методе OpenConnection
                // MessageBox.Show(e.Message);
                MessageBox.Show("Сервер не готов!");
            }
            finally
            {
                
            }
        }

        public void OnButtonAccept(object sender, EventArgs eventArgs)
        {
            var settings      = _viewSender.ConfigurationSettings;
            var command       = Command.Settings;

            try
            {
                //_modelRemoteConnectionManager.OpenConnection();
                _modelRemoteConnectionManager.SendSettings(settings, command);
            }
            catch(Exception e)
            {
                // todo: Создать свое исключение и генерировать его в методе OpenConnection
                // MessageBox.Show(e.Message);
                MessageBox.Show("Сервер не готов!");
            }
            finally
            {
                
            }
        }

        public void OnButtonSetBackgroundImage(object sender, EventArgs eventArgs)
        {
            var backgroundImage = _viewSender.DataSending.ImageFileName;
            var command         = Command.BackgroundImage;

            _viewSender.DataSending.ImageFileName = string.Empty;

            try
            {
                //_modelRemoteConnectionManager.OpenConnection();
                _modelRemoteConnectionManager.SendBackgroundImage(backgroundImage, command);
            }
            catch (Exception e)
            {
                // todo: Создать свое исключение и генерировать его в методе OpenConnection
                // MessageBox.Show(e.Message);
                MessageBox.Show("Сервер не готов!");
            }
            finally
            {

            }
        }

        public void OnButtonStartOrResetTimer(object sender, EventArgs eventArgs)
        {
            var timeStartTimer    = _viewSender.TimerSettings.Timer;
            var commandReciever   = _viewSender.TimerSettings.Command;

            //var ip   = _viewSender.ConnectionParameters.IPAddress;
            //var port = _viewSender.ConnectionParameters.Port; 

            try
            {
                // Open a connection to the reciever
                //_modelRemoteConnectionManager.OpenConnection(ip, port);
                // The connection is opened!
                _modelRemoteConnectionManager.SendData(timeStartTimer, commandReciever);
                // The image was send!
            }
            catch (Exception e)
            {
                // todo: Создать свое исключение и генерировать его в методе OpenConnection
                // MessageBox.Show("Сервер не готов!");
            }
            finally
            {
                // something code that'll must be completed
            }
        }

        public void OnButtonSetAsBackgroundImage(object sender, BackgroundImageEventArgs eventArgs)
        {
            foreach (var formReciver in _listRecieverView)
            {
                var pictureBox = formReciver.Controls.Find("picboxRecievedImage", false).FirstOrDefault() as PictureBox;
                if (pictureBox != null)
                {
                    pictureBox.BackgroundImage       = Image.FromFile(eventArgs.ImageFileName);
                    pictureBox.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
        }

        public void OnButtonStartTimer(object sender, EventArgs eventArgs)
        {
            foreach (var formReciver in _listRecieverView)
            {
                
                var pictureBox = formReciver.Controls.Find("picboxRecievedImage", false).FirstOrDefault() as PictureBox;
                if (pictureBox != null)
                {
                    
                    var timer = pictureBox.Controls.Find("simpleTimerReciver", false).FirstOrDefault() as ExtControlLibrary.SimpleTimer;
                    if (timer != null)
                    {
                        timer.SetTime(_viewSender.TimerSettings.Timer.ToString());
                    }
                }
            }
        }

        public void OnButtonSendMessageXorImage(object sender, EventArgs eventArgs)
        {
            var formReciver = _listRecieverView.FirstOrDefault() as RecieverView;

            if (formReciver != null)
            {
                var imageFileName = _viewSender.DataSending.ImageFileName;
                var text          = _viewSender.DataSending.Message;

                if (!string.IsNullOrEmpty(text))
                {
                    var fontFamily = _viewSender.ConfigurationSettings.FontFamilyText;           // шрифт
                    var size       = (float)_viewSender.ConfigurationSettings.SizeText;          // размер

                    FontStyle style;                                    // стиль:
                    switch (_viewSender.ConfigurationSettings.TextStyle)
                    {
                        case TypeText.Bold:
                            style = FontStyle.Bold;                     // жирный
                            break;
                        case TypeText.Italic:
                            style = FontStyle.Italic;                   // курсивный
                            break;
                        case TypeText.ItalicBold:
                            style = FontStyle.Bold | FontStyle.Italic;  // жирный и курсивный
                            break;
                        default:
                            style = FontStyle.Regular;                  // обычный (регулярный)
                            break;
                    }

                    var font = new Font(fontFamily, size, style);

                    var picboxImage = formReciver.Controls.Find("picboxRecievedImage", false).FirstOrDefault() as PictureBox;
                    picboxImage.Tag = font;

                    picboxImage.Refresh();
                }
                else
                {
                    var streamImage = new MemoryStream(File.ReadAllBytes(imageFileName));
                    //MessageBox.Show(_modelLocalMonitorManager.NumberSelectedMonitor.ToString());
                    //MessageBox.Show("DISPLAY" + (_modelLocalMonitorManager.NumberSelectedMonitor - 1).ToString());

                    formReciver._fieldCurrentDisplayName = "DISPLAY" + (_modelLocalMonitorManager.NumberSelectedMonitor + 1).ToString();
                    formReciver.ShowMessageWithTextXorImage(formReciver, streamImage, null);
                }
            }
        }

        public void OnButtonCloseVideoPlayer(object sender, EventArgs eventArgs)
        {
            var receiver = _listRecieverView.FirstOrDefault() as RecieverView;

            if (receiver != null)
            {
                // Отключаем наш медиа плеер
                var wmp = receiver.Controls.Find("axWMPOnlyVideo", false).FirstOrDefault() as AxWindowsMediaPlayer;
                wmp.Ctlcontrols.stop();
                wmp.URL = "";
                wmp.Visible = false;

                var listviewVideo = _viewSender.DictionaryListViewPlayers[MediaDataType.Video];

                var playableVideoTrackNumber = listviewVideo.Playlist.PlayableTrackNumber;
                if (playableVideoTrackNumber != -1)
                {
                    var button = listviewVideo.GetEmbeddedControl(1, (playableVideoTrackNumber) % listviewVideo.Playlist.TracksCount) as StartPlayMediaDataButton;

                    if (button.CanSelect)
                        button.PerformClick();
                    else
                        button.HandlerClick(button, EventArgs.Empty);
                }

                receiver.Refresh();
            }
        }

        // TODO: Testing
        async public void OnButtonCloseFormWithTextOrImage(object sender, EventArgs eventArgs)
        {
            var receiver = _listRecieverView.FirstOrDefault() as RecieverView;

            if (receiver != null)
            {
                var formWithTxtOrImg = receiver._childrenListForms.FirstOrDefault();

                if (formWithTxtOrImg == null)
                    return;

                for (int iter = 0; iter < 5; iter++)
                {
                    await Task.Delay(2);
                    formWithTxtOrImg.Opacity -= 0.2;
                }

                if (receiver.InvokeRequired)
                {
                    receiver.Invoke((MethodInvoker)delegate()
                    {
                        formWithTxtOrImg.Close();
                    });
                }

                receiver._childrenListForms.Remove(formWithTxtOrImg);

                Cursor.Show();
            }
        }

        public void OnButtonCloseAllAudio(object sender, EventArgs eventArgs)
        {
            var empty           = string.Empty;       
            var message         = empty;
            var imageFileName   = empty;
            var musicFileName   = empty;
            var command         = Command.StopSoundAndDeleteBackgroundImage;


            var ip   = _viewSender.ConnectionParameters.IPAddress;
            var port = _viewSender.ConnectionParameters.Port; 


            try
            {
                _modelRemoteConnectionManager.OpenConnection(ip, port);
                _modelRemoteConnectionManager.SendData(message, imageFileName, musicFileName, command);
            }
            //catch (Exception e)
            //{
            //    // todo: Создать свое исключение и генерировать его в методе OpenConnection
            //    MessageBox.Show("Сервер не готов!");
            //}
            finally
            {
               
            }

        }

        public void OnButtonCloseAllSecondaryForms(object sender, EventArgs eventArgs)
        {
            var empty           = string.Empty;

            var message         = empty;
            var imageFileName   = empty;
            var musicFileName   = empty;

            var ip   = _viewSender.ConnectionParameters.IPAddress;
            var port = _viewSender.ConnectionParameters.Port; 

            var command         = Command.CloseAllSecondaryForms;

            try
            {
                _modelRemoteConnectionManager.OpenConnection(ip, port);
                _modelRemoteConnectionManager.SendData(message, imageFileName, musicFileName, command);
            }
            //catch (Exception e)
            //{
            //    // todo: Создать свое исключение и генерировать его в методе OpenConnection
            //    MessageBox.Show("Сервер не готов!");
            //}
            finally
            {

            }
        }

        public void OnButtonPausePlayVideo(object sender, EventArgs eventArgs)
        {
            var videoFile = _viewSender.DataSending.MediaDataFileName;

            var formReceiverView = _listRecieverView.FirstOrDefault();

            if (formReceiverView != null)
            {
                var wmp = formReceiverView.Controls.Find("axWMPOnlyVideo", false).FirstOrDefault() as AxWindowsMediaPlayer;
                if (wmp == null)
                    return;

                // лишнее, но пока нет других вариантов (((
                if (!wmp.Visible)
                {
                    wmp.uiMode = "none";
                    wmp.Visible = true;

                    var timer = formReceiverView.Controls.Find("simpleTimerReciver", true).FirstOrDefault() as ExtControlLibrary.SimpleTimer;

                    if (timer != null)
                    {
                        wmp.Size = new Size(timer.Width, timer.Width); // (int)((formReceiverView.Height - timer.Height) * 0.75)
                        wmp.Location = new Point((formReceiverView.Width - wmp.Width) / 2, (formReceiverView.Height - wmp.Height) / 2 + timer.Height / 2);
                    }
                }

                if (wmp.playState == WMPPlayState.wmppsStopped)
                    wmp.URL = videoFile;

                wmp.Ctlcontrols.play();
            }
        }

        public void OnButtonPauseVideo(object sender, EventArgs eventArgs)
        {
            var formReceiverView = _listRecieverView.FirstOrDefault();

            if (formReceiverView != null)
            {
                var wmp = formReceiverView.Controls.Find("axWMPOnlyVideo", false).FirstOrDefault() as AxWindowsMediaPlayer;
                if (wmp == null)
                    return;

                wmp.Ctlcontrols.pause();
            }
        }

        public void OnButtonStopPlayVideo(object sender, EventArgs eventArgs)
        {
            var videoFile = _viewSender.DataSending.MediaDataFileName;

            var formReceiverView = _listRecieverView.FirstOrDefault();

            if (formReceiverView != null)
            {
                var wmp = formReceiverView.Controls.Find("axWMPOnlyVideo", false).FirstOrDefault() as AxWindowsMediaPlayer;
                if (wmp == null)
                    return;

                if (!wmp.Visible)
                {
                    var timer = formReceiverView.Controls.Find("simpleTimerReciver", true).FirstOrDefault() as ExtControlLibrary.SimpleTimer;

                    if (timer != null)
                    {
                        wmp.Size = new Size(timer.Width, timer.Width); // (int)((formReceiverView.Height - timer.Height) * 0.75)
                        wmp.Location = new Point((formReceiverView.Width - wmp.Width) / 2, (formReceiverView.Height - wmp.Height) / 2 + timer.Height / 2);
                    }
                }

                wmp.URL = videoFile;
                wmp.Ctlcontrols.play();

                formReceiverView.Refresh();
            }
        }

        public List<string> OnLoadGetMonitors(object sender, EventArgs e)
        {
            return _modelLocalMonitorManager.GetListMonitors();
        }

        public byte OnLoadGetPrimaryMonitor(object sender, EventArgs e)
        {
            return _modelLocalMonitorManager.NumberPrimaryMonitor;
        }

        public void OnChangeSelectedMonitor(object sender, EventArgs e)
        {
            var cmbbox = sender as ComboBox;
            _modelLocalMonitorManager.NumberSelectedMonitor = (byte)cmbbox.SelectedIndex;
        }

        public void OnButtonStartDisplayForm(object sender, EventArgs e, CheckState stateFullscreenOrNot)
        {
            _listRecieverView.Clear();

            RecieverView.settingsMessageWindow = _viewSender.ConfigurationSettings;

            var formRecieverView = new RecieverView(true);

            var axMediaPlayer = formRecieverView.Controls.Find("axWMPOnlyVideo", false).FirstOrDefault() as AxWindowsMediaPlayer;

            axMediaPlayer.PlayStateChange += axMediaPlayer_PlayStateChange;

            var picboxImage = formRecieverView.Controls.Find("picboxRecievedImage", false).FirstOrDefault() as PictureBox;

            picboxImage.Paint  += picboxImage_Paint;
            picboxImage.Resize += picboxImage_Resize;

            _listRecieverView.Add(formRecieverView);

            var selectedMonitor = _modelLocalMonitorManager.Screens[_modelLocalMonitorManager.NumberSelectedMonitor];
            _modelLocalMonitorManager.SetFormOnChoosenMonitor(selectedMonitor, formRecieverView, stateFullscreenOrNot);
        }

        private async void axMediaPlayer_PlayStateChange(object sender, _WMPOCXEvents_PlayStateChangeEvent e)
        {
            var axMediaPlayer = sender as AxWindowsMediaPlayer;

            if (axMediaPlayer == null)
                return;

            var listviewVideo = _viewSender.DictionaryListViewPlayers[MediaDataType.Video];

            if (e.newState == (int)WMPPlayState.wmppsPlaying)
            {
                //axMediaPlayer.uiMode = "none";    // без этого условия то работает полноэкранный режим, то нет
                axMediaPlayer.fullScreen = true;
                axMediaPlayer.Visible = true;
            }

            if (e.newState == (int)WMPPlayState.wmppsMediaEnded)
            {
                axMediaPlayer.Visible = false;

                var playableVideoTrackNumber = listviewVideo.Playlist.PlayableTrackNumber;
                if (playableVideoTrackNumber == -1)
                    return;

                var button = listviewVideo.GetEmbeddedControl(1, (playableVideoTrackNumber) % listviewVideo.Playlist.TracksCount) as StartPlayMediaDataButton;
                await Task.Delay(1);
                if (button.CanSelect)
                    button.PerformClick();
                else
                    button.HandlerClick(button, EventArgs.Empty);
            }
        }

        void picboxImage_Resize(object sender, EventArgs e)
        {
            (sender as PictureBox).Refresh();
        }

        void picboxImage_Paint(object sender, PaintEventArgs e)
        {
            var message = _viewSender.DataSending.Message;
            
            if (!string.IsNullOrEmpty(message))
            {
                var picbox = (sender as PictureBox);
                var brush = new SolidBrush(Color.FromName(_viewSender.ConfigurationSettings.ForeColorText));
                // Кажется лишшнее - нужно также, как и с браш
                //var font = picbox.Tag == null ? new Font(FontFamily.Families[0], 1, FontStyle.Regular) : (Font)(picbox.Tag);
                var font = (Font)(picbox.Tag);

                if (font != null)
                    TextManager.DrawTextInForm(message, font, brush, e);
            }
        }

        public void OnButtonCloseDisplayForm(object sender, EventArgs e)
        {
            foreach (var recieverView in _listRecieverView)
                recieverView.Close();

            /*
             Вызвать сброс на стороне сендера
             */

            _listRecieverView.Clear();
        }

        // TODO: Rework

        public async Task<string> OnButtonConnect(string ip, int port)
        {
            return _modelRemoteConnectionManager.OpenConnection(ip, port);
        }

        public void OnButtonDisconnect(string ip, int port)
        {
            _modelRemoteConnectionManager.OpenConn(ip, port);
        }

        public bool OnButtonSendIPAddressSender(string ipSender)
        {
            return _modelRemoteConnectionManager.SendIPAddressSender(ipSender);
        }


        // Reset the background color of the receiver

        //////////////////////////////////////////////

        // Reset the background image of the receiver

        public void OnButtonResetBgImageRemoteReceiver()
        {
            var empty           = string.Empty;
            var message         = empty;
            var imageFileName   = empty;
            var musicFileName   = empty;
            var command         = Command.ResetBgImage;


            var ip   = _viewSender.ConnectionParameters.IPAddress;
            var port = _viewSender.ConnectionParameters.Port;


            try
            {
                _modelRemoteConnectionManager.OpenConnection(ip, port);
                _modelRemoteConnectionManager.SendData(message, imageFileName, musicFileName, command);
            }
            //catch (Exception e)
            //{
            //    // todo: Создать свое исключение и генерировать его в методе OpenConnection
            //    MessageBox.Show("Сервер не готов!");
            //}
            finally
            {

            }
        }

        public void OnButtonResetBackImageLocalReceiver()
        {
            var receiver = _listRecieverView.FirstOrDefault();

            if (receiver != null)
            {
                var pictureBoxForBgImage = receiver.Controls.Find("picboxRecievedImage", false).FirstOrDefault();

                if (pictureBoxForBgImage != null)
                {
                    pictureBoxForBgImage.BackgroundImage = null;
                    pictureBoxForBgImage.BackColor       = Color.DarkRed;
                }
            }
        }
    }
}
