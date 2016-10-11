using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace Sender.View
{
    using Sender.Entities;
    using Sender.EXControls;
    using Sender.Extensions;
    using Sender.Win32;
    using Sender.Helpers;
    using System.Drawing.Imaging;
    using Sender.Presenter;
    using MediaDataSerialization;   
    using CommonTypes;
    using System.Linq.Expressions;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Net.Sockets;
    using System.Net;
    using System.Diagnostics;
    using WMPLib;
    using Sender.EventArguments;
    using Sender.Services;

    using ViewDetails = System.Windows.Forms.View;
    using Timer = MediaDataSerialization.Timer;
    using System.Globalization;
    using System.Reflection;

    public enum ModeViewSender : byte
    {
        None              = 0,
        MonitorFullScreen = 10,
        RemoteScreen      = 20,
        Chromecast        = 30
    }

    public struct TimerData
    {
        public Timer   Timer;
        public Command Command;
        
        public TimerData(Timer time)
        {
            Timer   = time;
            Command = Command.Time;
        }
    }

    public struct ConnectionData
    {
        public string IPAddress;
        public ushort Port;

        public ConnectionData(string hostName, ushort port)
        {
            IPAddress = hostName;
            Port      = port;
        }
    }

    public interface ISenderView
    {
        PresenterSender Presenter              { get; set; }
        MediaData       DataSending            { get; set; }
        TimerData       TimerSettings          { get; set; }
        Settings        ConfigurationSettings  { get; set; }
        ConnectionData  ConnectionParameters   { get; set; }

        Dictionary<MediaDataType, Player> DictionaryListViewPlayers { get; }
    }

    partial class ViewSender : Form, ISenderView
    {
        private Thread threadListenReciever = default(Thread);

        private readonly WindowsMediaPlayer senderPlayerVideo;
        private readonly WindowsMediaPlayer senderPlayerMusic;
        private readonly WindowsMediaPlayer senderPlayerSound;

        private readonly Dictionary<MediaDataType, WindowsMediaPlayer> dictionarySenderPlayers = new Dictionary<MediaDataType,WindowsMediaPlayer>();

        private const string folderVideoFiles          = @"Video";
        private const string folderMusicFiles          = @"Music";
        private const string folderImageFiles          = @"Images";
        private const string folderSoundFiles          = @"Sounds";
        private const string folderStorageMessagesFile = @"Messages";
        private const string folderStorageSettingsFile = @"Settings";

        private const string fileStorageVideo      = @"VideoPlaylist.dat";
        private const string fileStorageMusic      = @"MusicPlaylist.dat";
        private const string fileStorageImages     = @"ImagePlaylist.dat";
        private const string fileStorageSounds     = @"SoundPlaylist.dat";
        private const string fileStorageMessages   = @"MessagePlaylist.dat";
        private const string fileStorageSettings   = @"Settings.dat";

        private static readonly string _applicationDirectory;

        private static readonly string _pathPlaylistVideoTracks;
        private static readonly string _pathPlaylistMusicTracks;
        private static readonly string _pathPlaylistSoundTracks;
        private static readonly string _pathListBackgroundImages;
        private static readonly string _pathListStorageMessages;
        private static readonly string _pathListStorageSettings;

        private static readonly string dirVideoFiles;
        private static readonly string dirMusicFiles;
        private static readonly string dirImageFiles;
        private static readonly string dirSoundFiles;
        private static readonly string dirMessagesStorageFile;
        private static readonly string dirSettingsStorageFile;


        private Timer _timer;


        private struct PictureSize
        {
            internal int Width;
            internal int Height;
        }
        
        private readonly PictureSize MessagePictureBoxSize;
        private readonly PictureSize BackgroundImagePictureBoxSize;

        private VideoPlayerListView    lstviewVideo;
        private MediaPlayerListView    lstviewMusic;
        private MediaPlayerListView    lstviewSound;
        private ImageListView          lstviewImage;
        private StorageMessageListView lstviewMessage;

        // вынести в отдельное свойство
        public Dictionary<MediaDataType, Player> DictionaryListViewPlayers
        {
            get { return dictionaryListViewPlayers; }
        }

        private readonly Dictionary<MediaDataType, Player> dictionaryListViewPlayers = new Dictionary<MediaDataType,Player>();

        private ModeViewSender CurrentModeViewSender { get; set; }
        private TimerStatus    CurrentTimerStatus    { get; set; }


        public TimerData       TimerSettings         { get; set; }
        public PresenterSender Presenter             { get; set; }
        public MediaData       DataSending           { get; set; }
        public Settings        ConfigurationSettings { get; set; }


        private delegate void AddElementsToListWithButtons(int startRow);


        private ConnectionData _connParams;
        public  ConnectionData ConnectionParameters
        {
            get { return _connParams; }
            set { _connParams = value; }
        }


        private bool _isConnectedToReciver = false;

        private bool _hasSelectedMode      = false;

        private bool _isFinishCodePath     = false;


        private static readonly string _ipAddressSender;

        private static readonly ushort _portSender;

        private static readonly ushort _portReceiver;

        private static readonly ushort _portChromecast;


        static ViewSender()
        {
            # region Создание папок и файлов для хранения видео/музыки/аудио, а также сообщений и настроек

            #region Получение локального IP-адреса Отправителя (Sender) и Приемника (Receiver)

            _ipAddressSender = GetLocalIPAddress();
            _portSender      = 13000;
            _portReceiver    = 12000;
            _portChromecast  = 8008;

            #endregion

            _applicationDirectory = Application.StartupPath;

            dirVideoFiles          = Path.Combine(_applicationDirectory, folderVideoFiles);
            dirMusicFiles          = Path.Combine(_applicationDirectory, folderMusicFiles);
            dirSoundFiles          = Path.Combine(_applicationDirectory, folderSoundFiles);
            dirImageFiles          = Path.Combine(_applicationDirectory, folderImageFiles);
            dirMessagesStorageFile = Path.Combine(_applicationDirectory, folderStorageMessagesFile);
            dirSettingsStorageFile = Path.Combine(_applicationDirectory, folderStorageSettingsFile);

            _pathPlaylistVideoTracks  = Path.Combine(dirVideoFiles, fileStorageVideo);   
            _pathPlaylistMusicTracks  = Path.Combine(dirMusicFiles, fileStorageMusic);          
            _pathPlaylistSoundTracks  = Path.Combine(dirSoundFiles, fileStorageSounds);
            _pathListBackgroundImages = Path.Combine(dirImageFiles, fileStorageImages);
            _pathListStorageMessages  = Path.Combine(dirMessagesStorageFile, fileStorageMessages);
            _pathListStorageSettings  = Path.Combine(dirSettingsStorageFile, fileStorageSettings);

            if (!File.Exists(_pathPlaylistVideoTracks))
                CreateFileIfNotExist(_pathPlaylistVideoTracks, dirVideoFiles);

            if (!File.Exists(_pathPlaylistMusicTracks))
                CreateFileIfNotExist(_pathPlaylistMusicTracks, dirMusicFiles);

            if (!File.Exists(_pathListBackgroundImages))
                CreateFileIfNotExist(_pathListBackgroundImages, dirImageFiles);

            if (!File.Exists(_pathPlaylistSoundTracks))
                CreateFileIfNotExist(_pathPlaylistSoundTracks, dirSoundFiles);

            if (!File.Exists(_pathListStorageMessages))
                CreateFileIfNotExist(_pathListStorageMessages, dirMessagesStorageFile);

            if (!File.Exists(_pathListStorageSettings))
                CreateFileIfNotExist(_pathListStorageSettings, dirSettingsStorageFile);

            #endregion
        }

        public ViewSender()
        {
            // Производим инициализацию компонентов
            InitializeComponent();

            #region Проведение основной настройки работы приложения

            // Отмечаем, что пока нет выбранного режима
            CurrentModeViewSender = ModeViewSender.None;


            // Создаем Видео-/Музыкальный-/Аудиоплеер
            senderPlayerVideo = new WindowsMediaPlayer();
            senderPlayerMusic = new WindowsMediaPlayer();
            senderPlayerSound = new WindowsMediaPlayer();

            // Регистрируем наши плееры в специальном справочнике
            dictionarySenderPlayers.Add(MediaDataType.Video, senderPlayerVideo);
            dictionarySenderPlayers.Add(MediaDataType.Music, senderPlayerMusic);
            dictionarySenderPlayers.Add(MediaDataType.Sound, senderPlayerSound);

            // Подписываем плееры на событие изменения состояния проигрываемого трэка
            senderPlayerVideo.PlayStateChange += senderPlayerVideo_PlayStateChange;
            senderPlayerMusic.PlayStateChange += senderPlayerMusic_PlayStateChange;
            senderPlayerSound.PlayStateChange += senderPlayerSound_PlayStateChange;


            // Инициализируем таймер
            _timer = new Timer();
            // Получаем первоначальные настройки таймер
            TimerSettings = new TimerData(_timer);


            // Получаем настройки из двоичного файла настроек
            var settings = Settings.DeserializeSettings(_pathListStorageSettings);

            // Если файл с настройками есть, то
            if (settings != null)
                ConfigurationSettings = settings;           // принимаем эти настройки
            else
                ConfigurationSettings = new Settings();     // иначе, принимаем по умолчанию
            
 
            // Сбрасываем кнопки, отвечающие за выбранный режим, к первоначальному состоянию
            ResetModeButtonToOriginalState(true, true, true);
            // Отключаем контролы во всех блоках с контролами
            TurnOnOffControls(false, false, false);
            // Устанавливаем таймер в исходное состояние
            SetUpTimerToOriginalState();


            // Получаем размеры отправляемого изображения
            MessagePictureBoxSize.Width  = picboxAttachingImage.Width;
            MessagePictureBoxSize.Height = picboxAttachingImage.Height;

            // Получаем размеры фонового изображения
            BackgroundImagePictureBoxSize.Width  = picboxBgImagePreview.Width;
            BackgroundImagePictureBoxSize.Height = picboxBgImagePreview.Height;


            // Данные для отправки на Ресивер по умолчанию
            DataSending = new MediaData 
            { 
                MediaDataFileName = String.Empty,
                ImageFileName     = String.Empty,
                Message           = String.Empty, 
                Command           = Command.Text 
            };

            #endregion

            #region Restore data into the Settings block

            datetimepickerStartValue.Value = dateTimePickerStartTimeByDefault.Value = ConfigurationSettings.StartTimer;

            txtboxIpAddressRemoteConnByDefault.Text = ConfigurationSettings.IpAddressRemoteMachine.ToString();
            txtBoxRemoteConnPortByDefault.Text      = ConfigurationSettings.RemoteConnectionPort.ToString();
            txtBoxChromecastConnPortByDefault.Text  = ConfigurationSettings.ChromecastConnectionPort.ToString();

            numUpDownTextSize.Value = ConfigurationSettings.SizeText;

            cmbBoxTextFont.DataSource     = FontFamily.Families.Select(f => f.Name).ToList();
            cmbBoxTextFont.SelectedItem   = ConfigurationSettings.FontFamilyText;
            cmbBoxTextFont.DropDownHeight = 150;

            var styleItalicBold = ConfigurationSettings.TextStyle;

            switch (styleItalicBold)
            {
                case TypeText.Bold:
                    butTextTypeItalic.Tag = 0;
                    butTextTypeItalic.BackColor = Color.White;
                    butTextTypeItalic.ForeColor = Color.Black;

                    butTextTypeBold.Tag = 1;
                    butTextTypeBold.BackColor = Color.DodgerBlue;
                    butTextTypeBold.ForeColor = Color.White;
                    break;
                case TypeText.Italic:
                    butTextTypeBold.Tag = 0;
                    butTextTypeBold.BackColor = Color.White;
                    butTextTypeBold.ForeColor = Color.Black;

                    butTextTypeItalic.Tag = 1;
                    butTextTypeItalic.BackColor = Color.DodgerBlue;
                    butTextTypeItalic.ForeColor = Color.White;
                    break;
                case TypeText.ItalicBold:
                    butTextTypeBold.Tag = 1;
                    butTextTypeBold.BackColor = Color.DodgerBlue;
                    butTextTypeBold.ForeColor = Color.White;

                    butTextTypeItalic.Tag = 1;
                    butTextTypeItalic.BackColor = Color.DodgerBlue;
                    butTextTypeItalic.ForeColor = Color.White;
                    break;
                case TypeText.None:
                    butTextTypeBold.Tag = 0;
                    butTextTypeBold.BackColor = Color.White;
                    butTextTypeBold.ForeColor = Color.Black;

                    butTextTypeItalic.Tag = 0;
                    butTextTypeItalic.BackColor = Color.White;
                    butTextTypeItalic.ForeColor = Color.Black;
                    break;
            }

            #region Устанавливаем цвет текста как в файле настроек

            //TODO: код можно упростить

            var colors  = Enum.GetValues(typeof(KnownColor));
            foreach (var knowColor in colors)
            {
                cmbBoxListColors.Items.Add(knowColor);
            }

            var dictColors = new Dictionary<string, KnownColor>();

            foreach (var knowColor in colors)
            {
                dictColors[knowColor.ToString()] = (KnownColor)knowColor;
            }

            cmbBoxListColors.SelectedItem = dictColors[ConfigurationSettings.ForeColorText];
            cmbBoxListColors.DropDownHeight = 150;

            #endregion

            #endregion

            #region Остальные настройки

            ConnectionParameters = new ConnectionData(_ipAddressSender, _portReceiver);

            picboxBgImagePreview.BackColor = Color.DarkRed;
            // Делаем все контролы программы доступными
            checkBoxOnOffScreen.Checked    = true;
            butChromecast.Enabled          = false;

            #endregion

            //TODO: События ниже перенести в другое место
        }

        // Рудимент
        private async void senderPlayerVideo_PlayStateChange(int NewState)
        {
            if (NewState == (int)WMPPlayState.wmppsMediaEnded)
            {
                var playableVideoTrackNumber = lstviewVideo.Playlist.PlayableTrackNumber;
                await Task.Delay(1);
                butPlayPauseMediaDataUnit_Click(lstviewVideo.GetEmbeddedControl(1, (playableVideoTrackNumber) % lstviewVideo.Playlist.TracksCount), new EventArgs());
            }
        }

        private async void senderPlayerSound_PlayStateChange(int NewState)
        {
            if (NewState == (int)WMPPlayState.wmppsMediaEnded)
            {
                var playableSoundTrackNumber = lstviewSound.Playlist.PlayableTrackNumber;
                await Task.Delay(1);
                butPlayPauseMediaDataUnit_Click(lstviewSound.GetEmbeddedControl(1, (playableSoundTrackNumber) % lstviewSound.Playlist.TracksCount), new EventArgs());
            }
        }

        private async void senderPlayerMusic_PlayStateChange(int NewState)
        {
            if (NewState == (int)WMPPlayState.wmppsMediaEnded)
            {
                var playableMusicTrackNumber = lstviewMusic.Playlist.PlayableTrackNumber;
                await Task.Delay(1);
                butPlayPauseMediaDataUnit_Click(lstviewMusic.GetEmbeddedControl(1, (++playableMusicTrackNumber) % lstviewMusic.Playlist.TracksCount), new EventArgs());
            }
        }

        private void SenderView_Load(object sender, EventArgs e)
        {
            // lstviewMessage

            lstviewMessage = new StorageMessageListView(_pathListStorageMessages);
            lstviewMessage.Location = new Point(21, 31);
            lstviewMessage.Size = new Size(480, 297);
            lstviewMessage.MultiSelect = false;
            lstviewMessage.GridLines = true;
            lstviewMessage.Scrollable = true;
            lstviewMessage.HideSelection = true;
            lstviewMessage.FullRowSelect = true;
            lstviewMessage.LockColumnSize = true;
            lstviewMessage.AllowColumnReorder = false;
            lstviewMessage.AutoResizeColumns(ColumnHeaderAutoResizeStyle.None);
            lstviewMessage.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstviewMessage.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            lstviewMessage.View = System.Windows.Forms.View.Details;
            lstviewMessage.Font = new Font("Calibri", 9);

            lstviewMessage.MakeColumnHeaders("Message Text", 450, HorizontalAlignment.Center);

            lstviewMessage.ItemSelectionChanged += lstviewMessage_ItemSelectionChanged;

            var squareLen = 0;

            // listviewVideo
            lstviewVideo = new VideoPlayerListView(_pathPlaylistVideoTracks);

            lstviewVideo.DirVideoFiles = dirVideoFiles;

            lstviewVideo.Font     = new Font("Times New Roman", 12);
            lstviewVideo.Size     = new Size(714, 282);
            lstviewVideo.Location = new Point(7, 45);

            lstviewVideo.GridLines          = true;
            lstviewVideo.Scrollable         = true;
            lstviewVideo.MultiSelect        = false;
            lstviewVideo.HideSelection      = true;
            lstviewVideo.FullRowSelect      = true;
            lstviewVideo.LockColumnSize     = true;
            lstviewVideo.AllowColumnReorder = false;

            lstviewVideo.Dock        = DockStyle.None;
            lstviewVideo.View        = ViewDetails.Details;
            lstviewVideo.Anchor      = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            lstviewVideo.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            lstviewVideo.AutoResizeColumns(ColumnHeaderAutoResizeStyle.None);

            var commonColumnWidthVideoTrack = 714;
            lstviewVideo.Size = new Size(commonColumnWidthVideoTrack, 282);

            // Make the column headers.
            lstviewVideo.MakeColumnHeaders(
                "Video Track", 0, HorizontalAlignment.Center,
                "", 0, HorizontalAlignment.Center,
                "", 0, HorizontalAlignment.Center,
                "", 0, HorizontalAlignment.Center);

            lstviewVideo.AddRow("", "", "", "");
            squareLen = lstviewVideo.Items[0].Bounds.Height;
            lstviewVideo.Items.RemoveAt(lstviewVideo.Items.Count - 1);

            lstviewVideo.Columns[0].Width = commonColumnWidthVideoTrack - 3 * squareLen - 2;
            lstviewVideo.Columns[1].Width = squareLen - 1;
            lstviewVideo.Columns[2].Width = squareLen - 1;
            lstviewVideo.Columns[3].Width = squareLen;

            // lstviewSound
            lstviewSound = new MediaPlayerListView(_pathPlaylistSoundTracks);
            lstviewSound.DirAudioFiles = dirSoundFiles;
            lstviewSound.Size = new Size(314, 282);

            lstviewSound.Dock = DockStyle.None;
            lstviewSound.HideSelection = true;
            lstviewSound.FullRowSelect = true;
            lstviewSound.MultiSelect = false;
            lstviewSound.GridLines = true;

            lstviewSound.LockColumnSize = true;
            lstviewSound.AllowColumnReorder = false;

            lstviewSound.Scrollable = true;

            lstviewSound.AutoResizeColumns(ColumnHeaderAutoResizeStyle.None);
            lstviewSound.Font = new Font("Times New Roman", 12);
            lstviewSound.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            lstviewSound.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));

            lstviewSound.View = System.Windows.Forms.View.Details;
            lstviewSound.Location = new Point(7, 45);

            var commonColumnWidthSoundTrack = 314;
            lstviewSound.Size = new Size(commonColumnWidthSoundTrack, 282);

            // Make the column headers.
            lstviewSound.MakeColumnHeaders(
                "Sound Track", 0, HorizontalAlignment.Center,
                "", 0, HorizontalAlignment.Center,
                "", 0, HorizontalAlignment.Center,
                "", 0, HorizontalAlignment.Center);

            lstviewSound.AddRow("", "", "", "");
            squareLen = lstviewSound.Items[0].Bounds.Height;
            lstviewSound.Items.RemoveAt(lstviewSound.Items.Count - 1);

            lstviewSound.Columns[0].Width = commonColumnWidthSoundTrack - 3 * squareLen - 2;
            lstviewSound.Columns[1].Width = squareLen - 1;
            lstviewSound.Columns[2].Width = squareLen - 1;
            lstviewSound.Columns[3].Width = squareLen;

            // lstviewImage
            lstviewImage          = new ImageListView(_pathListBackgroundImages);
            lstviewImage.DirImageFiles = dirImageFiles;

            lstviewImage.Location = new Point(3, 28);
            lstviewImage.Dock     = DockStyle.Fill;
            lstviewImage.Font     = new Font("Times New Roman", 12);

            lstviewImage.HideSelection = true;
            lstviewImage.FullRowSelect = true;
            lstviewImage.MultiSelect   = false;
            lstviewImage.GridLines     = true;

            lstviewImage.LockColumnSize     = true;
            lstviewImage.AllowColumnReorder = false;
            lstviewImage.Scrollable         = true;
            
            lstviewImage.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstviewImage.Anchor      = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            lstviewImage.View        = System.Windows.Forms.View.Details;

            lstviewImage.AutoResizeColumns(ColumnHeaderAutoResizeStyle.None);

            lstviewImage.MakeColumnHeaders(
               "Image", 0, HorizontalAlignment.Center,
               "", 0, HorizontalAlignment.Center,
               "", 0, HorizontalAlignment.Center);

            var commonColumnWidthImage = 383;
            lstviewImage.Size = new Size(commonColumnWidthImage, 286);

            // lstviewMusic
            lstviewMusic                    = new MediaPlayerListView(_pathPlaylistMusicTracks);
            lstviewMusic.DirAudioFiles      = dirMusicFiles;

            lstviewMusic.Dock               = DockStyle.None;
            lstviewMusic.HideSelection      = true;
            lstviewMusic.FullRowSelect      = true;
            lstviewMusic.MultiSelect        = false;
            lstviewMusic.GridLines          = true;

            lstviewMusic.LockColumnSize     = true;
            lstviewMusic.AllowColumnReorder = false;

            lstviewMusic.Scrollable         = true;
            
            lstviewMusic.AutoResizeColumns(ColumnHeaderAutoResizeStyle.None);
            lstviewMusic.Font = new Font("Times New Roman", 12);
            lstviewMusic.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            lstviewMusic.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));

            lstviewMusic.View = System.Windows.Forms.View.Details;
            lstviewMusic.Location = new Point(7, 45);

            var commonColumnWidthMusicTrack = 314;
            lstviewMusic.Size = new Size(commonColumnWidthMusicTrack, 282);

            

            // Make the column headers.
            lstviewMusic.MakeColumnHeaders(
                "Music Track", 0, HorizontalAlignment.Center,
                "",      0, HorizontalAlignment.Center,
                "",      0, HorizontalAlignment.Center,
                "",      0, HorizontalAlignment.Center);

            //-----------------------------------
            lstviewMusic.AddRow("", "", "", "");
            squareLen = lstviewMusic.Items[0].Bounds.Height;
            lstviewMusic.Items.RemoveAt(lstviewMusic.Items.Count - 1);

            lstviewMusic.Columns[0].Width = commonColumnWidthMusicTrack - 3 * squareLen - 2;
            lstviewMusic.Columns[1].Width = squareLen - 1;
            lstviewMusic.Columns[2].Width = squareLen - 1;
            lstviewMusic.Columns[3].Width = squareLen;

            //-----------------------------------
            squareLen = lstviewImage.Items[0].Bounds.Height;
            lstviewImage.Columns[0].Width = commonColumnWidthImage - 2 * squareLen - 1;
            lstviewImage.Columns[1].Width = squareLen - 1;
            lstviewImage.Columns[2].Width = squareLen;

            var butAddNewImage = new AddButton(0);
            butAddNewImage.Size = new Size(squareLen, squareLen);
            butAddNewImage.FlatStyle = FlatStyle.Flat;
            butAddNewImage.FlatAppearance.BorderSize = 0;
            butAddNewImage.ImageList = imageListBackgroundImages;
            butAddNewImage.ImageAlign = ContentAlignment.MiddleCenter;
            butAddNewImage.ImageIndex = 0;

            lstviewImage.AddEmbeddedControl(butAddNewImage, 1, 0, DockStyle.None);

            var butClickItemNewAddImage = new AddButton(0);
            butClickItemNewAddImage.Size = new Size(lstviewImage.Columns[0].Width, squareLen - 1);
            butClickItemNewAddImage.FlatStyle = FlatStyle.Flat;
            butClickItemNewAddImage.FlatAppearance.MouseDownBackColor = Color.DarkGreen;
            butClickItemNewAddImage.FlatAppearance.MouseOverBackColor = Color.DarkGreen;

            butClickItemNewAddImage.MouseLeave += butClickItemNewAddImage_MouseLeave;
            butClickItemNewAddImage.MouseEnter += butClickItemNewAddImage_MouseEnter;

            butClickItemNewAddImage.FlatAppearance.BorderSize = 0;
            butClickItemNewAddImage.Text = "New image";
            butClickItemNewAddImage.Font = new Font("Courier", 8, FontStyle.Italic);
            butClickItemNewAddImage.TextAlign = ContentAlignment.MiddleLeft;

            lstviewImage.AddEmbeddedControl(butClickItemNewAddImage, 0, 0, DockStyle.None);

            butAddNewImage.Click          += butAddNewImage_Click;
            butClickItemNewAddImage.Click += butAddNewImage_Click;

            //---------------------------------------------

            // here is the method that will be work for video tracks
            AddButtonsForInitialVideoTracks(0);
            AddButtonsForInitialSoundTracks(0);
            AddButtonsForInitialMusicTracks(0);
            AddButtonsForInitialImages(1);

            /* Регистрируем наши представления */
            dictionaryListViewPlayers.Add(MediaDataType.Video, lstviewVideo);
            dictionaryListViewPlayers.Add(MediaDataType.Music, lstviewMusic);
            dictionaryListViewPlayers.Add(MediaDataType.Sound, lstviewSound);
            
            /* Add the different playlists into the assigned group box controls */
            grboxVideo.Controls.Add(lstviewVideo);
            grboxMusic.Controls.Add(lstviewMusic);
            grboxSounds.Controls.Add(lstviewSound);
            grBoxBgImages.Controls.Add(lstviewImage);
            grBoxSavedMessages.Controls.Add(lstviewMessage);

            cmbBoxMonitorNumber.DataSource    = Presenter.OnLoadGetMonitors(sender, e);
            cmbBoxMonitorNumber.SelectedIndex = Presenter.OnLoadGetPrimaryMonitor(sender, e);


        }

        void lstviewMessage_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lstviewMessage.SelectedItems.Count != 0)
            {
                if (richtxtboxMessage.Enabled == true)
                {
                    richtxtboxMessage.Text = lstviewMessage.SelectedItems[0].Text;
                    richtxtboxMessage.Tag  = (byte)1;
                }
            }
        }

        void butClickItemNewAddImage_MouseEnter(object sender, EventArgs e)
        {
            var but       = sender as Button;
            but.TextAlign = ContentAlignment.TopLeft;
            but.ForeColor = Color.White;
            but.Font      = new Font("Courier", 9, FontStyle.Italic);
        }

        void butClickItemNewAddImage_MouseLeave(object sender, EventArgs e)
        {
            var but       = sender as Button;
            but.ForeColor = Color.Black;
            but.Font      = new Font("Courier", 8, FontStyle.Italic);
        }

        void butAddNewImage_Click(object sender, EventArgs e)
        {
            AddFileNamesThroughDialog(lstviewImage, AddButtonsForInitialImages);
        }

        private void ListenRemotePlayer()
        {
            try
            {
                // TODO: Replace this text IP address to the IP address in text box; also the port number
                TcpListener senderServer = new TcpListener(IPAddress.Parse(_ipAddressSender), _portSender);     // Создаем сервер-слушатель
                senderServer.Start();                                                                           // Запускаем сервер

                // Бесконечный цикл прослушивания очереди клиентов
                while (!_isFinishCodePath)
                {
                    // Проверяем очередь соединений
                    if (!senderServer.Pending())                                                                // Очередь запросов пуста
                        continue;

                    TcpClient senderClient = senderServer.AcceptTcpClient();                                    // Текущий клиент

                    //
                    //********************* Получение *********************
                    //

                    // Подключаемся к сокету для чтения
                    using (Stream readerStream = senderClient.GetStream())
                    {
                        // Получить объект данных и десериализовать
                        var formatter = new BinaryFormatter();
                        var dataReceived = (AutoPlay)formatter.Deserialize(readerStream);

                        switch (dataReceived.State)
                        {
                            case AutoPlayState.StopTrack:
                                var playableSoundTrackNumber = lstviewSound.Playlist.PlayableTrackNumber;
                                butPlayPauseMediaDataUnit_Click(lstviewSound.GetEmbeddedControl(1, (playableSoundTrackNumber) % lstviewSound.Playlist.TracksCount), new EventArgs());
                                break;
                            case AutoPlayState.NextTrack:
                                var playableMusicTrackNumber = lstviewMusic.Playlist.PlayableTrackNumber;
                                butPlayPauseMediaDataUnit_Click(lstviewMusic.GetEmbeddedControl(1, (++playableMusicTrackNumber) % lstviewMusic.Playlist.TracksCount), new EventArgs());
                                break;
                            case AutoPlayState.StopVideoTrack:
                                
                                var playableVideoTrackNumber = lstviewVideo.Playlist.PlayableTrackNumber;
                                butPlayPauseMediaDataUnit_Click(lstviewVideo.GetEmbeddedControl(1, (playableVideoTrackNumber) % lstviewVideo.Playlist.TracksCount), new EventArgs());
                                break;
                            case AutoPlayState.NothingIsPlaying:
                                _isConnectedToReciver = false;

                                ResetComponentsToInitialValues(new object(), new EventArgs());
                                Connect();

                                break;
                        }
                    }

                    senderClient.Close();
                }

                senderServer.Stop();
            }
            finally
            {
                
            }
        }

        void butPlayPauseSoundFile_Click(object sender, EventArgs e)
        {
            if (!_hasSelectedMode)
            {
                MessageBox.Show("Please, select one of the modes!");
                return;
            }

            var button = sender as StartPlayMediaDataButton;

            if (button == null)
                return;

            /*** Listner an answer ***/

            if (lstviewSound.Playlist.State == PlaylistTrackStates.Pause || lstviewSound.Playlist.State == PlaylistTrackStates.Stop)
            {
                if (threadListenReciever != null && threadListenReciever.ThreadState != System.Threading.ThreadState.Running)
                {
                    goto loopUsualClick;
                }

                threadListenReciever = new Thread(ListenRemotePlayer);
                threadListenReciever.Name = " Sound TCP Listner Sender ---> Reciever";
                threadListenReciever.IsBackground = true;
                threadListenReciever.Start();
            }
            else if (lstviewSound.Playlist.State == PlaylistTrackStates.Play)
            {

            }

            /***********************************************/

            loopUsualClick:

            var numberCurrentTrack  = button.NumberRowItem;
            var numberPlayableTrack = lstviewSound.Playlist.PlayableTrackNumber;

            Command command;
            switch (button.ButtonState)
            {
                case StartPlayMediaDataButton.ButtonStates.Play:

                    if (-1 == numberPlayableTrack && lstviewSound.Playlist.PreviousTrackNumber == numberCurrentTrack && lstviewSound.Playlist.State == PlaylistTrackStates.Pause)
                        command = Command.SoundPausePlay;
                    else if (numberCurrentTrack != numberPlayableTrack && numberPlayableTrack == -1 && lstviewSound.Playlist.State == PlaylistTrackStates.Pause)
                        command = Command.SoundStopPlay;
                    else if (numberCurrentTrack != numberPlayableTrack && numberPlayableTrack != -1 && lstviewSound.Playlist.State == PlaylistTrackStates.Play)
                        command = Command.SoundStopPlay;
                    else
                        command = Command.SoundStopPlay;

                    lstviewSound.Playlist.State = PlaylistTrackStates.Play;

                    break;
                case StartPlayMediaDataButton.ButtonStates.Pause:

                    if (numberCurrentTrack == numberPlayableTrack && lstviewSound.Playlist.State == PlaylistTrackStates.Play)
                    {
                        lstviewSound.Playlist.State = PlaylistTrackStates.Pause;
                        command = Command.SoundPause;
                    }
                    else if (numberCurrentTrack != numberPlayableTrack && lstviewSound.Playlist.State == PlaylistTrackStates.Play)
                    {
                        lstviewSound.Playlist.State = PlaylistTrackStates.Play;
                        command = Command.SoundStopPlay;
                    }
                    else
                    {
                        lstviewSound.Playlist.State = PlaylistTrackStates.Pause;
                        command = Command.SoundStopPlay;
                    }

                    break;
                case StartPlayMediaDataButton.ButtonStates.Continue:

                    goto withoutSendingDataToReciever;

                default:

                    command = Command.Text;

                    break;
            }

            // playing or pausing a music track
            var soundFileName = lstviewSound.Playlist.Tracks[numberCurrentTrack].FullPath;

            DataSending.MediaDataFileName = soundFileName;
            DataSending.Command = command;

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    {
                        if (string.IsNullOrEmpty(senderPlayerSound.URL) || senderPlayerSound.URL !=  soundFileName)
                            senderPlayerSound.URL = soundFileName;

                        switch (command)
                        {
                            case Command.SoundPausePlay:
                                senderPlayerSound.controls.play();
                                break;
                            case Command.SoundPause:
                                senderPlayerSound.controls.pause();
                                break;
                            case Command.SoundStopPlay:
                                senderPlayerSound.controls.stop();
                                senderPlayerSound.controls.play();
                                break;
                        }
                    }
                    break;
                case ModeViewSender.RemoteScreen:
                    {
                        // TODO: Проверить, что файл существует - это важно!
                        if (!string.IsNullOrEmpty(DataSending.MediaDataFileName) && _isConnectedToReciver)
                        {
                            Connect();
                            Presenter.OnButtonSend(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:
                    {

                    }
                    break;
                default:
                    break;
            }

            withoutSendingDataToReciever:

            ChangeButtonState(button);
            lstviewSound.ColorItemForPlayTrack(numberCurrentTrack);



            if (numberPlayableTrack != numberCurrentTrack)
            {
                var previousButton = lstviewSound.GetEmbeddedControl(1, numberPlayableTrack) as StartPlayMediaDataButton;
                var numberPreviousTrack = previousButton != null ? previousButton.NumberRowItem : -1;
                ChangeButtonState(previousButton);



                lstviewSound.ColorItemForNonPlayTrack(numberPreviousTrack);
                lstviewSound.Playlist.PlayableTrackNumber = numberCurrentTrack;
                lstviewSound.Playlist.PreviousTrackNumber = numberCurrentTrack;

                return;
            }

            lstviewSound.ColorItemForNonPlayTrack(numberPlayableTrack);
            lstviewSound.Playlist.PlayableTrackNumber = -1;
        }

        void butPlayPauseMusicFile_Click(object sender, EventArgs e)
        {
            if (!_hasSelectedMode)
            {
                MessageBox.Show("Please, select one of the modes!");
                return;
            }

            var button = sender as StartPlayMediaDataButton;

            if (button == null)
                return;

            /* Listener */

            if (CurrentModeViewSender != ModeViewSender. MonitorFullScreen &&
                (lstviewMusic.Playlist.State == PlaylistTrackStates.Pause || lstviewMusic.Playlist.State == PlaylistTrackStates.Stop))
            { 
                if (threadListenReciever != null && threadListenReciever.ThreadState != System.Threading.ThreadState.Running)
                {
                    goto loopUsualClick;
                }

                threadListenReciever = new Thread(ListenRemotePlayer);
                threadListenReciever.Name = "TCP Listner Sender ---> Reciever";
                threadListenReciever.IsBackground = true;
                threadListenReciever.Start();
            }
            else if (lstviewMusic.Playlist.State == PlaylistTrackStates.Play)
            {

            }

            /************/
            loopUsualClick:

            var numberCurrentTrack  = button.NumberRowItem;
            var numberPlayableTrack = lstviewMusic.Playlist.PlayableTrackNumber;

            Command command;
            switch (button.ButtonState)
            {
                case StartPlayMediaDataButton.ButtonStates.Play:
                    
                    if (-1 == numberPlayableTrack && lstviewMusic.Playlist.PreviousTrackNumber == numberCurrentTrack && lstviewMusic.Playlist.State == PlaylistTrackStates.Pause)
                        command = Command.MusicPausePlay;
                    else if (numberCurrentTrack != numberPlayableTrack && numberPlayableTrack == -1 && lstviewMusic.Playlist.State == PlaylistTrackStates.Pause)
                        command = Command.MusicStopPlay;
                    else if (numberCurrentTrack != numberPlayableTrack && numberPlayableTrack != -1 && lstviewMusic.Playlist.State == PlaylistTrackStates.Play)
                        command = Command.MusicStopPlay;
                    else
                        command = Command.MusicStopPlay;

                    lstviewMusic.Playlist.State = PlaylistTrackStates.Play;

                    break;
                case StartPlayMediaDataButton.ButtonStates.Pause:

                    if (numberCurrentTrack == numberPlayableTrack && lstviewMusic.Playlist.State == PlaylistTrackStates.Play)
                    {
                        lstviewMusic.Playlist.State = PlaylistTrackStates.Pause;
                        command = Command.MusicPause;
                    }
                    else if (numberCurrentTrack != numberPlayableTrack && lstviewMusic.Playlist.State == PlaylistTrackStates.Play)
                    {
                        lstviewMusic.Playlist.State = PlaylistTrackStates.Play;
                        command = Command.MusicStopPlay;
                    }
                    else
                    {
                        lstviewMusic.Playlist.State = PlaylistTrackStates.Pause;
                        command = Command.MusicStopPlay;
                    }

                    break;
                case StartPlayMediaDataButton.ButtonStates.Continue:

                    goto withoutSendingDataToReciever;

                default:

                    command = Command.Text;

                    break;
            }

            // playing or pausing a music track
            var musicFileName = lstviewMusic.Playlist.Tracks[numberCurrentTrack].FullPath;

            DataSending.MediaDataFileName = musicFileName;
            DataSending.Command       = command;

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    {
                        if (string.IsNullOrEmpty(senderPlayerMusic.URL) || senderPlayerMusic.URL != musicFileName)
                            senderPlayerMusic.URL = "" /*musicFileName*/;

                        switch (command)
                        {
                            case Command.MusicPausePlay:
                                senderPlayerMusic.controls.play();
                                break;
                            case Command.MusicPause:
                                senderPlayerMusic.controls.pause();
                                break;
                            case Command.MusicStopPlay:

                                senderPlayerMusic.controls.stop();
                                senderPlayerMusic.close();
                                senderPlayerMusic.URL = musicFileName;
                                senderPlayerMusic.controls.play();

                                break;
                        }
                    }
                    break;
                case ModeViewSender.RemoteScreen:
                    {
                        // TODO: Проверить, что файл существует - это важно!
                        if (!string.IsNullOrEmpty(DataSending.MediaDataFileName) && _isConnectedToReciver)
                        {
                            Connect();
                            Presenter.OnButtonSend(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:
                    {

                    }
                    break;
                default:
                    break;
            }

            withoutSendingDataToReciever:

            ChangeButtonState(button);
            lstviewMusic.ColorItemForPlayTrack(numberCurrentTrack);

            if (numberPlayableTrack != numberCurrentTrack)
            {
                var previousButton = lstviewMusic.GetEmbeddedControl(1, numberPlayableTrack) as StartPlayMediaDataButton;
                var numberPreviousTrack = previousButton != null ? previousButton.NumberRowItem : -1;
                ChangeButtonState(previousButton);



                lstviewMusic.ColorItemForNonPlayTrack(numberPreviousTrack);
                lstviewMusic.Playlist.PlayableTrackNumber = numberCurrentTrack;
                lstviewMusic.Playlist.PreviousTrackNumber = numberCurrentTrack;

                return;
            }

            lstviewMusic.ColorItemForNonPlayTrack(numberPlayableTrack);
            lstviewMusic.Playlist.PlayableTrackNumber = -1;
        }


        /* Создаем универсальный обработчик для следующих типов плейлистов: Video/Musix/Sounds (потом возможно еще Images для слайд-шоу) */

        #region Дополнительный функции для универсализации обработчика выполнения/остановки медиа контента

        private Command GetCommandByContentTypeAndAction(MediaDataType mediaDataType, PlayerActions playerActions)
        {
            var cmd    = mediaDataType.ToString() + playerActions.ToString();
            var retVal = Command.None;

            switch (cmd)
            {
                case "VideoPause":
                    retVal = Command.VideoPause;
                    break;
                case "VideoPausePlay":
                    retVal = Command.VideoPausePlay;
                    break;
                case "VideoStopPlay":
                    retVal = Command.VideoStopPlay;
                    break;

                case "MusicPause":
                    retVal = Command.MusicPause;
                    break;
                case "MusicPausePlay":
                    retVal = Command.MusicPausePlay;
                    break;
                case "MusicStopPlay":
                    retVal = Command.MusicStopPlay;
                    break;

                case "SoundPause":
                    retVal = Command.SoundPause;
                    break;
                case "SoundPausePlay":
                    retVal = Command.SoundPausePlay;
                    break;
                case "SoundStopPlay":
                    retVal = Command.SoundStopPlay;
                    break;

                default:
                    break;
            }

            return retVal;
        }

        private PlayerActions GetActionByCommand(Command cmd)
        {
            var actionString = string.Empty;
            actionString = cmd.ToString().Replace("Video", "");
            actionString = actionString.ToString().Replace("Music", "");
            actionString = actionString.ToString().Replace("Sound", "");

            var retVal = PlayerActions.None;

            switch (actionString)
            {
                case "Pause":
                    retVal = PlayerActions.Pause;
                    break;
                case "PausePlay":
                    retVal = PlayerActions.PausePlay;
                    break;
                case "StopPlay":
                    retVal = PlayerActions.StopPlay;
                    break;
            }

            return retVal;
        }

        private bool IsContentTypeVideo(Command type)
        {
            var typeStr = type.ToString();

            if (typeStr.Contains("Video"))
                return true;

            return false;
        }

        #endregion

        void butPlayPauseMediaDataUnit_Click(object sender, EventArgs e)
        {
            if (!_hasSelectedMode)
            {
                MessageBox.Show("Please, select one of the modes!");
                return;
            }

            var button = sender as StartPlayMediaDataButton;

            if (button == null)
                return;

            var sendingContentType = button.ContentType;
            var listview           = dictionaryListViewPlayers[sendingContentType];
            var senderPlayer       = dictionarySenderPlayers[sendingContentType];

            /* Listener */

            if (CurrentModeViewSender != ModeViewSender.MonitorFullScreen &&
                (listview.Playlist.State == PlaylistTrackStates.Pause || listview.Playlist.State == PlaylistTrackStates.Stop))
            {
                if (threadListenReciever != null && threadListenReciever.ThreadState != System.Threading.ThreadState.Running)
                {
                    goto loopUsualClick;
                }

                threadListenReciever = new Thread(ListenRemotePlayer);
                threadListenReciever.Name = "TCP Listner Sender ---> Reciever";
                threadListenReciever.IsBackground = true;
                threadListenReciever.Start();
            }
            else if (listview.Playlist.State == PlaylistTrackStates.Play)
            {

            }

            /************/
        loopUsualClick:

            var numberCurrentTrack  = button.NumberRowItem;
            var numberPlayableTrack = listview.Playlist.PlayableTrackNumber;

            Command command;
            switch (button.ButtonState)
            {
                case StartPlayMediaDataButton.ButtonStates.Play:

                    if (-1 == numberPlayableTrack && listview.Playlist.PreviousTrackNumber == numberCurrentTrack && listview.Playlist.State == PlaylistTrackStates.Pause)
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.PausePlay);
                    else if (numberCurrentTrack != numberPlayableTrack && numberPlayableTrack == -1 && listview.Playlist.State == PlaylistTrackStates.Pause)
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.StopPlay);
                    else if (numberCurrentTrack != numberPlayableTrack && numberPlayableTrack != -1 && listview.Playlist.State == PlaylistTrackStates.Play)
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.StopPlay);
                    else
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.StopPlay);;

                    listview.Playlist.State = PlaylistTrackStates.Play;

                    break;
                case StartPlayMediaDataButton.ButtonStates.Pause:

                    if (numberCurrentTrack == numberPlayableTrack && listview.Playlist.State == PlaylistTrackStates.Play)
                    {
                        listview.Playlist.State = PlaylistTrackStates.Pause;
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.Pause);
                    }
                    else if (numberCurrentTrack != numberPlayableTrack && listview.Playlist.State == PlaylistTrackStates.Play)
                    {
                        listview.Playlist.State = PlaylistTrackStates.Play;
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.StopPlay);
                    }
                    else
                    {
                        listview.Playlist.State = PlaylistTrackStates.Pause;
                        command = GetCommandByContentTypeAndAction(sendingContentType, PlayerActions.StopPlay);
                    }

                    break;
                case StartPlayMediaDataButton.ButtonStates.Continue:

                    goto withoutSendingDataToReciever;

                default:

                    command = Command.Text;

                    break;
            }

            // playing or pausing a music track
            var mediadataFileName = listview.Playlist.Tracks[numberCurrentTrack].FullPath;

            DataSending.MediaDataFileName = mediadataFileName;
            DataSending.Command = command;

            var actionSenderPlayer = GetActionByCommand(command);

            var isVideo = IsContentTypeVideo(command);

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    {
                        //MessageBox.Show(actionSenderPlayer.ToString());

                        // Коряво проставляются экшены - переделать

                        // Code for video
                        if (isVideo)
                        {
                            switch (actionSenderPlayer)
                            {
                                

                                case PlayerActions.PausePlay:
                                    Presenter.OnButtonPausePlayVideo(sender, e);
                                    break;
                                case PlayerActions.Pause:
                                    Presenter.OnButtonPauseVideo(sender, e);
                                    break;
                                case PlayerActions.StopPlay:
                                    Presenter.OnButtonCloseFormWithTextOrImage(sender, e);
                                    Presenter.OnButtonStopPlayVideo(sender, e);
                                    break;
                            }
                        }
                        else
                        {
                            // Зачем эта строчка
                            if (string.IsNullOrEmpty(senderPlayer.URL) || senderPlayer.URL != mediadataFileName)
                                senderPlayer.URL = "" /*mediadataFileName*/;

                            switch (actionSenderPlayer)
                            {
                                case PlayerActions.PausePlay:
                                    senderPlayer.controls.play();
                                    break;
                                case PlayerActions.Pause:
                                    senderPlayer.controls.pause();
                                    break;
                                case PlayerActions.StopPlay:

                                    senderPlayer.controls.stop(); // для чего это, если есть url = ....
                                    senderPlayer.close();
                                    senderPlayer.URL = mediadataFileName;
                                    senderPlayer.controls.play();

                                    break;
                            }
                        }
                    }
                    break;
                case ModeViewSender.RemoteScreen:
                    {
                        // TODO: Проверить, что файл существует - это важно!
                        if (!string.IsNullOrEmpty(DataSending.MediaDataFileName) && _isConnectedToReciver)
                        {
                            Connect();
                            //butClear_Click(sender, e);
                            Presenter.OnButtonSend(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:
                    {

                    }
                    break;
                default:
                    break;
            }

        withoutSendingDataToReciever:

            ChangeButtonState(button);
            listview.ColorItemForPlayTrack(numberCurrentTrack);

            if (numberPlayableTrack != numberCurrentTrack)
            {
                var previousButton = listview.GetEmbeddedControl(1, numberPlayableTrack) as StartPlayMediaDataButton;
                var numberPreviousTrack = previousButton != null ? previousButton.NumberRowItem : -1;
                ChangeButtonState(previousButton);



                listview.ColorItemForNonPlayTrack(numberPreviousTrack);
                listview.Playlist.PlayableTrackNumber = numberCurrentTrack;
                listview.Playlist.PreviousTrackNumber = numberCurrentTrack;

                return;
            }

            listview.ColorItemForNonPlayTrack(numberPlayableTrack);
            listview.Playlist.PlayableTrackNumber = -1;
        }


        /* ***************************************************************************************************************************** */

        private void ChangeButtonState(StartPlayMediaDataButton button)
        {
            if (button != null)
            {
                var indexImageButton = button.ImageIndex;
                var nextIndexImageButton = checked ((byte)(++indexImageButton) % 2);

                button.ButtonState = checked((StartPlayMediaDataButton.ButtonStates)nextIndexImageButton);
                button.ImageIndex = nextIndexImageButton;
            }
        }

        private void butDeleteMediaDataUnit_Click(object sender, EventArgs e)
        {
            // Delete a track from Playlist
            var button =  sender as DeleteButton;

            if (button == null)
                return;

            var deletedItemRow = button.NumberRowDeletedItem;
            var contentType    = button.ContentType;
            var listview       = dictionaryListViewPlayers[contentType];

            if (deletedItemRow == listview.Playlist.PlayableTrackNumber)
            {
                MessageBox.Show("Don't delete this track because it is playing!\nPlease, pause the track and so it can be deleted.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            else if (deletedItemRow < listview.Playlist.PlayableTrackNumber)
            {
                listview.Playlist.PlayableTrackNumber--;
            }
            else
            {
                // nothing do
            }

            if (deletedItemRow < listview.Playlist.PreviousTrackNumber)
            {
                listview.Playlist.PreviousTrackNumber--;
            }

            var butPlayPauseLV = listview.GetEmbeddedControl(1, deletedItemRow) as StartPlayMediaDataButton;
            var countElems     = listview.Items.Count;

            listview.RemoveEmbeddedControl(button);
            listview.RemoveEmbeddedControl(butPlayPauseLV);
            listview.Items[deletedItemRow].Remove();
            listview.Playlist.Tracks.RemoveAt(deletedItemRow);

            listview.RecountButtonNumbers(deletedItemRow, countElems);
        }

        private void butDeleteMusic_Click(object sender, EventArgs e)
        {
            // Delete a track from Playlist
            var button         =  sender as DeleteButton;
            var deletedItemRow =  button.NumberRowDeletedItem;

            if (deletedItemRow == lstviewMusic.Playlist.PlayableTrackNumber)
            {
                MessageBox.Show("Don't delete this track because it is playing!\nPlease, pause the track and so it can be deleted.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            else if (deletedItemRow < lstviewMusic.Playlist.PlayableTrackNumber)
            {
                lstviewMusic.Playlist.PlayableTrackNumber--;
            }
            else
            {
                // nothing do
            }

            if (deletedItemRow < lstviewMusic.Playlist.PreviousTrackNumber)
            {
                lstviewMusic.Playlist.PreviousTrackNumber--;
            }

            var butPlayPauseLV = lstviewMusic.GetEmbeddedControl(1, deletedItemRow) as StartPlayMediaDataButton;
            var countElems     = lstviewMusic.Items.Count;

            lstviewMusic.RemoveEmbeddedControl(button);
            lstviewMusic.RemoveEmbeddedControl(butPlayPauseLV);
            lstviewMusic.Items[deletedItemRow].Remove();
            lstviewMusic.Playlist.Tracks.RemoveAt(deletedItemRow);

            lstviewMusic.RecountButtonNumbers(deletedItemRow, countElems);
        }

        private void butDeleteSound_Click(object sender, EventArgs e)
        {
            // Delete a track from Playlist
            var button         =  sender as DeleteButton;
            var deletedItemRow =  button.NumberRowDeletedItem;

            if (deletedItemRow == lstviewSound.Playlist.PlayableTrackNumber)
            {
                MessageBox.Show("Don't delete this track because it is playing!\nPlease, pause the track and so it can be deleted.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            else if (deletedItemRow < lstviewSound.Playlist.PlayableTrackNumber)
            {
                lstviewSound.Playlist.PlayableTrackNumber--;
            }
            else
            {
                // nothing do
            }

            if (deletedItemRow < lstviewSound.Playlist.PreviousTrackNumber)
            {
                lstviewSound.Playlist.PreviousTrackNumber--;
            }

            var butPlayPauseLV = lstviewSound.GetEmbeddedControl(1, deletedItemRow) as StartPlayMediaDataButton;
            var countElems     = lstviewSound.Items.Count;

            lstviewSound.RemoveEmbeddedControl(button);
            lstviewSound.RemoveEmbeddedControl(butPlayPauseLV);
            lstviewSound.Items[deletedItemRow].Remove();
            lstviewSound.Playlist.Tracks.RemoveAt(deletedItemRow);

            lstviewSound.RecountButtonNumbers(deletedItemRow, countElems);
        }

        private void linklblAttachImage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.Title = "Выбрать изображение";
                dialog.RestoreDirectory = true;
                dialog.Filter = FileManager.GetImageFilter();

                var dialogResult = dialog.ShowDialog();
                var imageFilePath = string.Empty;
                if (dialogResult == DialogResult.OK)
                {
                    // Get a path to the selected image
                    imageFilePath = dialog.FileName;

                    picboxAttachingImage.ForceImageIntoPictureBox(
                                                MessagePictureBoxSize.Width,
                                                MessagePictureBoxSize.Height,
                                                imageFilePath);

                    DataSending = new MediaData { ImageFileName = imageFilePath, Command = Command.Image };

                    richtxtboxMessage.Enabled = false;
                }
            }
        }

        private void butSend_Click(object sender, EventArgs e)
        {
            if (checkboxSaveMessage.Checked && !string.IsNullOrEmpty(DataSending.Message) && (byte)richtxtboxMessage.Tag != 1)
                lstviewMessage.AddNewMessage(DataSending.Message);

            switch(CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    {
                        if (!string.IsNullOrEmpty(DataSending.Message))
                        {
                            DataSending.Command = Command.Text;
                            Presenter.OnButtonCloseVideoPlayer(sender, e);
                            Presenter.OnButtonCloseFormWithTextOrImage(sender, e);
                            Presenter.OnButtonSendMessageXorImage(sender, e);

                            richtxtboxMessage.Tag = (byte)0;
                            lstviewMessage.SelectedItems.Clear();
                            richtxtboxMessage.Clear();
                        }
                        else if (!string.IsNullOrEmpty(DataSending.ImageFileName))
                        {
                            DataSending.Command = Command.Image;
                            Presenter.OnButtonCloseVideoPlayer(sender, e);
                            Presenter.OnButtonCloseFormWithTextOrImage(sender, e);
                            Presenter.OnButtonSendMessageXorImage(sender, e);

                            butClear_Click(butClear, e);
                        }
                        else
                        {
                            DataSending.Command = Command.CloseAllSecondaryForms;
                            Presenter.OnButtonCloseVideoPlayer(sender, e);
                            Presenter.OnButtonCloseFormWithTextOrImage(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.RemoteScreen:
                    {
                        if (!_isConnectedToReciver)
                        {
                            return;
                        }
                        else if (!string.IsNullOrEmpty(DataSending.Message))
                        {
                            Connect();
                            DataSending.Command = Command.Text;
                            Presenter.OnButtonSend(sender, e);

                            richtxtboxMessage.Clear();
                        }
                        else if (!string.IsNullOrEmpty(DataSending.ImageFileName))
                        {
                            Connect();
                            DataSending.Command = Command.Image;
                            Presenter.OnButtonSend(sender, e);

                            butClear_Click(butClear, new EventArgs());
                        }
                        else if (string.IsNullOrEmpty(DataSending.Message + DataSending.ImageFileName))
                        {
                            Connect();
                            DataSending.Command = Command.CloseAllSecondaryForms;
                            Presenter.OnButtonSend(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:
                    {

                    }
                    break;
                default:
                    MessageBox.Show("Please, select one of the modes!");                    
                    break;
            }
        }

        private void butClear_Click(object sender, EventArgs e)
        {
            picboxAttachingImage.Width  = MessagePictureBoxSize.Width;
            picboxAttachingImage.Height = MessagePictureBoxSize.Height;

            picboxAttachingImage.Image                 = null;
            picboxAttachingImage.BackColor             = Color.Silver;
            picboxAttachingImage.BackgroundImageLayout = ImageLayout.Center;

            DataSending.ImageFileName = string.Empty;
            DataSending.Command       = Command.None;

            richtxtboxMessage.Enabled = true;
        }

        #region Modes

        private void butMonitorFullScreen_Click(object sender, EventArgs e)
        {
            _hasSelectedMode = true;

            GoFromRemoteScreenToLocalOrChromecastScreen();

            CurrentModeViewSender = ModeViewSender.MonitorFullScreen;

            SetButtonToNewState(sender);
            ResetModeButtonToOriginalState(false, true, true);

            #region Метки/комбобоксы/поля/кнопки для режима Monitor Full-Screen

            var deltaX = 0;
            var deltaY = -3;

            lblDisplayNumber.Location = new Point(370 + deltaX, 27 + deltaY);
            lblDisplayNumber.Size = new Size(106, 19);
            cmbBoxMonitorNumber.Location = new Point(478 + deltaX, 27 + deltaY);
            cmbBoxMonitorNumber.Size = new Size(146, 22);

            butStartDisplay.Location = new Point(372 + deltaX, 51 + deltaY);
            butStartDisplay.Size = new Size(97, 23);
            butCloseDisplay.Location = new Point(372 + deltaX, 76 + deltaY);
            butCloseDisplay.Size = new Size(97, 23);
            checkBoxFullscreen.Location = new Point(376 + deltaX, 101 + deltaY);
            checkBoxFullscreen.Size = new Size(78, 19);

            checkBoxFullscreen.CheckState = CheckState.Checked;

            TurnOnOffControls(true, false, false);

            #endregion
        }

        private void butRemoteScreen_Click(object sender, EventArgs e)
        {
            _hasSelectedMode = true;

            GoFromLocalScreenToRemoteOrChromecastScreen(); // TODO: Place to a event handler

            CurrentModeViewSender = ModeViewSender.RemoteScreen;

            SetButtonToNewState(sender);
            ResetModeButtonToOriginalState(true, false, true);

            #region Метки/комбобоксы/поля/кнопки для режима Remote Screen

            var deltaX = 0;
            var deltaY = 0;

            lblIP.Location      = new Point(380 + deltaX, 26 + deltaY);
            lblIP.Size          = new Size(26, 19);
            lblPort.Location    = new Point(380 + deltaX, 51 + deltaY);
            lblPort.Size        = new Size(38, 19);
            txtBoxIP.Location   = new Point(423 + deltaX, 26 + deltaY);
            txtBoxIP.Size       = new Size(145, 20);
            txtBoxPort.Location = new Point(423 + deltaX, 51 + deltaY);
            txtBoxPort.Size     = new Size(145, 20);
            butConnect.Location = new Point(423 + deltaX, 76 + deltaY);
            butConnect.Size     = new Size(68, 23);
            lblResultConnection.Location = new Point(423 + deltaX, 102 + deltaY);
            lblResultConnection.Size = new Size(78, 17);
            lblResultConnection.Text = String.Empty;

            TurnOnOffControls(false, true, false);

            #endregion
        }

        private void butChromecast_Click(object sender, EventArgs e)
        {
            _hasSelectedMode = true;

            CurrentModeViewSender = ModeViewSender.Chromecast;

            SetButtonToNewState(sender);
            ResetModeButtonToOriginalState(true, true, false);

            #region Метки/комбобоксы/поля/кнопки для режима Chromecast

            lblChromecastDevice.Location = new Point(360, 24);
            cmbBoxChromecastDevice.Location = new Point(362, 46);
            cmbBoxChromecastDevice.Size = new Size(259, 22);

            TurnOnOffControls(false, false, true);

            #endregion
        }

        private void SetButtonToNewState(object objectButton)
        {
            var button = objectButton as Button;
            if (button != null)
            {
                button.Font      = new Font("Times New Roman", 13);
                button.BackColor = Color.DodgerBlue;
                button.ForeColor = Color.White;
            }
        }

        private void ResetModeButtonToOriginalState(bool butFS, bool butRS, bool butCC)
        {
            var defaultFont      = new Font("Times New Roman", 11);
            var defaultBackColor = Color.White;
            var defaultForeColor = Color.Black;

            if (butFS) // Monitor Fullscreen
            {
                butMonitorFullscreen.Font      = defaultFont;
                butMonitorFullscreen.BackColor = defaultBackColor;
                butMonitorFullscreen.ForeColor = defaultForeColor;
            }
            if (butRS) // Remote Screen
            {
                butRemoteScreen.Font      = defaultFont;
                butRemoteScreen.BackColor = defaultBackColor;
                butRemoteScreen.ForeColor = defaultForeColor;
            }
            if (butCC) // Chromecast
            {
                butChromecast.Font      = defaultFont;
                butChromecast.BackColor = defaultBackColor;
                butChromecast.ForeColor = defaultForeColor;
            }
        }

        private void TurnOnOffControls(bool blockDisplayNumberVisible, bool blockNetworkIpPortVisible, bool blockChromecastDeviceVisible)
        {
            butStartDisplay.Visible     = blockDisplayNumberVisible;
            butCloseDisplay.Visible     = blockDisplayNumberVisible;
            lblDisplayNumber.Visible    = blockDisplayNumberVisible;
            checkBoxFullscreen.Visible  = blockDisplayNumberVisible;
            cmbBoxMonitorNumber.Visible = blockDisplayNumberVisible;

            lblIP.Visible               = blockNetworkIpPortVisible;
            lblPort.Visible             = blockNetworkIpPortVisible;
            txtBoxIP.Visible            = blockNetworkIpPortVisible;
            txtBoxPort.Visible          = blockNetworkIpPortVisible;
            butConnect.Visible          = blockNetworkIpPortVisible;
            lblResultConnection.Visible = blockNetworkIpPortVisible;

            lblChromecastDevice.Visible    = blockChromecastDeviceVisible;
            cmbBoxChromecastDevice.Visible = blockChromecastDeviceVisible;
        }

        private void linklblReset_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ResetModeButtonToOriginalState(true, true, true);
            TurnOnOffControls(false, false, false);

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    GoFromLocalScreenToRemoteOrChromecastScreen(); // TODO: Place to a event handler
                    break;
                case ModeViewSender.RemoteScreen:
                    GoFromRemoteScreenToLocalOrChromecastScreen(); // TODO: Place to a event handler
                    break;
            }

            CurrentModeViewSender = ModeViewSender.None;
            _hasSelectedMode = false;
        }

        #endregion

        #region Timer

        private void datetimepickerStartValue_ValueChanged(object sender, EventArgs e)
        {

            var datetimeStart    = (sender as DateTimePicker).Value;
            _timer.Hours   = datetimeStart.Hour;
            _timer.Minutes = datetimeStart.Minute;
            _timer.Seconds = datetimeStart.Second;

            var countdown            = DisplayTimeInTimer(_timer.Hours,_timer.Minutes, _timer.Seconds);
            lblDiagitalTimeLeft.Text = countdown;
            lblDiagitalTimeLeft.Tag  = countdown;
        }

        private void butStartTimer_Click(object sender, EventArgs e)
        {
            if (!_hasSelectedMode)
            {
                MessageBox.Show("Please, select one of the modes!");
                return;
            }

            switch (CurrentTimerStatus)
            {
                case TimerStatus.Play:  // The timer is playing
                    {
                        SetUpTimerToPausedState();
                    }
                    break;
                case TimerStatus.None:  // The timer still was not started
                case TimerStatus.Pause: // The timer was paused
                    {
                        SetUpTimerToPlayingState();
                    }
                    break;
            }

            RemoteOrLocalOperation(sender, e);
        }

        private void timerStartTime_Tick(object sender, EventArgs e)
        {
            _timer.Seconds     = _timer.Seconds - 1;
            lblDiagitalTimeLeft.Text = _timer.ToString();

            if (_timer.State == TimerStatus.End)
            {
                SetUpTimerToOriginalState();
                return;
            }

            RemoteOrLocalOperation(sender, e);
        }

        private void butReset_Click(object sender, EventArgs e)
        {
            ResetTimerToOriginalState(sender, e);
        }

        private void butPlusOneMinute_Click(object sender, EventArgs e)
        {
            ++_timer.Minutes;             // set up timer by one minute more
            UpDownOneMinuteInTimer(sender, e);
        }

        private void butMinusOneMinute_Click(object sender, EventArgs e)
        {
            --_timer.Minutes;             // set up timer by one minute less
            UpDownOneMinuteInTimer(sender, e);
        }

        private void ResetTimerToOriginalState(object sender, EventArgs e)
        {
            SetUpTimerToOriginalState();
            RemoteOrLocalOperation(sender, e);
        }

        private void UpDownOneMinuteInTimer(object sender, EventArgs e)
        {
            lblDiagitalTimeLeft.Text = DisplayTimeInTimer(_timer.Hours, _timer.Minutes, _timer.Seconds);

            RemoteOrLocalOperation(sender, e);
        }

        private void SetUpTimerToPlayingState()
        {
            var playingState = TimerStatus.Play;
            SetTimerConfiguration(playingState);
            EnableDisableMainElementsGroupBoxTimer(playingState);

        }

        private void SetUpTimerToPausedState()
        {
            var pausedState = TimerStatus.Pause;
            SetTimerConfiguration(pausedState);
            EnableDisableMainElementsGroupBoxTimer(pausedState);
        }

        /// <summary> 
        /// Set up the timer to the original state. 
        /// </summary>
        private void SetUpTimerToOriginalState()
        {
            timerStartTime.Stop();

            var noneState        = TimerStatus.None;
            CurrentTimerStatus   = noneState;
            _timer.State   = noneState;
            _timer.Hours   = datetimepickerStartValue.Value.Hour;
            _timer.Minutes = datetimepickerStartValue.Value.Minute;
            _timer.Seconds = datetimepickerStartValue.Value.Second;
            
            butStartTimer.Image      = imageListPlayPauseTimer.Images[0];
            lblDiagitalTimeLeft.Text = DisplayTimeInTimer(_timer.Hours, _timer.Minutes, _timer.Seconds);

            EnableDisableMainElementsGroupBoxTimer(noneState);
        }    

        /// <summary> Set up the timer to the specified status. </summary>
        /// <param name="status"> The timer should be set up to the configuration of the specified status. </param>
        private void SetTimerConfiguration(TimerStatus status)
        {
            switch (status)
            {
                case TimerStatus.None:
                    {
                        CurrentTimerStatus = TimerStatus.None;                      // The timer's current status is equal to nothing
                        butStartTimer.Image = imageListPlayPauseTimer.Images[0];    // The Pause image
                        timerStartTime.Start();                                     // Start the timer
                    }
                    break;
                case TimerStatus.Play:
                    {
                        CurrentTimerStatus = TimerStatus.Play;                      // The timer's current status is equal to playing
                        butStartTimer.Image = imageListPlayPauseTimer.Images[1];    // The Play image
                        timerStartTime.Start();                                     // Start the timer
                    }
                    break;
                case TimerStatus.Pause:
                    {
                        timerStartTime.Stop();                                      // Stop the timer
                        butStartTimer.Image = imageListPlayPauseTimer.Images[0];    // The Pause image
                        CurrentTimerStatus = TimerStatus.Pause;                     // The timer's current status is equal to suspended
                    }
                    break;
            }
        }

        /// <summary> Enable or disable some controls on the Group Box form's element. </summary>
        /// <param name="status"> The timer's group box should be set up to the configuration of the specified status. </param>
        private void EnableDisableMainElementsGroupBoxTimer(TimerStatus status)
        {
            switch (status)
            {
                case TimerStatus.None:                              // Nothing
                    {
                        datetimepickerStartValue.Enabled = true;    // The Starting Point datetimepicker is enabled
                        butReset.Enabled                 = false;   // The Reset button is disabled
                        butPlusOneMinute.Enabled         = false;   // The Add One Minute button is disable
                        butMinusOneMinute.Enabled        = false;   // The Substruct One Minute button is disable
                    }
                    break;
                case TimerStatus.Play:                              // Playing
                case TimerStatus.Pause:                             // Paused
                    {
                        datetimepickerStartValue.Enabled = false;   // is disabled
                        butReset.Enabled                 = true;    // is enabled
                        butPlusOneMinute.Enabled         = true;    // is enabled
                        butMinusOneMinute.Enabled        = true;    // is enabled
                    }
                    break;
            }
        }

        #endregion

        #region Remote computer/Local computer/Chromecast device

        /// <summary> Complete local operations and remote operations alike. </summary>
        /// <param name="sender"> The object that has initiate an event. </param>
        /// <param name="e"> A context of the event. </param>
        private void RemoteOrLocalOperation(object sender, EventArgs e)
        {
            TimerSettings = new TimerData(_timer);

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:  // Fullscreen Mode
                    {
                        Presenter.OnButtonStartTimer(sender, e);
                    }
                    break;
                case ModeViewSender.RemoteScreen:       // Remote Mode
                    {
                        if (_isConnectedToReciver)
                        {
                            Connect();
                            Presenter.OnButtonStartOrResetTimer(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:         // Chromecast Mode
                    {
                        // some code
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Additional methods

        private string AddZeroOrNot(int hh_min_or_sec)
        {
            if (0 <= hh_min_or_sec && hh_min_or_sec <= 9)
                return "0" + hh_min_or_sec.ToString();
            else
                return hh_min_or_sec.ToString();
        }

        private string DisplayTimeInTimer(int hours, int minutes, int seconds)
        {
            return String.Format("{0}:{1}:{2}", AddZeroOrNot(hours), AddZeroOrNot(minutes), AddZeroOrNot(seconds));
        }

        #endregion

        private void butCloseDisplay_Click(object sender, EventArgs e)
        {
            Presenter.OnButtonCloseDisplayForm(sender, e);
        }

        private void butStartDisplay_Click(object sender, EventArgs e)
        {
            Presenter.OnButtonStartDisplayForm(sender, e, checkBoxFullscreen.CheckState);
        }

        private void butConnect_Click(object sender, EventArgs e)
        {
            var host = txtBoxIP.Text;

            ushort port;
            if (ushort.TryParse(txtBoxPort.Text, out port))
            {
                _connParams.IPAddress = host;
                _connParams.Port = port;
            }

           Connect();


            if (lblResultConnection.ForeColor == Color.Green)
            {
                _isConnectedToReciver = true;
                Presenter.OnButtonSendIPAddressSender(_ipAddressSender);
                //await Task.Run( () => Presenter.OnButtonSendIPAddressSender(_ipAddressSender) );
            }
            else
                _isConnectedToReciver = false;
        }

        private async void Connect()
        {
            var resultConnection     =  Presenter.OnButtonConnect(_connParams.IPAddress, _connParams.Port);
            lblResultConnection.Text = await resultConnection;

            // TODO: Many not needed work
            if (lblResultConnection.Text.Contains("Not"))
            {
                //_isConnectedToReciver = false;
                lblResultConnection.ForeColor = Color.Red;
            }
            else
            {
                //_isConnectedToReciver = true;
                lblResultConnection.ForeColor = Color.Green;
            }
        }

        private void butAddNewSounds_Click(object sender, EventArgs e)
        {
            AddFileNamesThroughDialog(lstviewSound, AddButtonsForInitialSoundTracks);
        }

        private void butAddNewMusic_Click(object sender, EventArgs e)
        {
            AddFileNamesThroughDialog(lstviewMusic, AddButtonsForInitialMusicTracks);
        }

        private void butAddNewVideo_Click(object sender, EventArgs e)
        {
            AddFileNamesThroughDialog(lstviewVideo, AddButtonsForInitialVideoTracks);
        }

        // переписать на player
        private void AddFileNamesThroughDialog(ListView listview, AddElementsToListWithButtons methodAddElemsWithButtons)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.Multiselect      = true;
                dialog.CheckPathExists  = true;
                dialog.CheckFileExists  = true;

                var listviewIsMediaPlayerListView = listview as MediaPlayerListView;
                var listviewIsVideoPlayerLIstView = listview as VideoPlayerListView;
                var listviewIsImageListView       = listview as ImageListView;

                if (listviewIsMediaPlayerListView != null)
                    dialog.Filter = FileManager.GetAudioFilter();
                else if (listviewIsImageListView != null)
                    dialog.Filter = FileManager.GetImageFilter();
                else if (listviewIsVideoPlayerLIstView != null)
                    dialog.Filter = FileManager.GetVideoFilter();

                if (DialogResult.OK == dialog.ShowDialog())
                {
                    string[] filePaths = dialog.FileNames;
                    int currentCountItemsListView;

                    if (listviewIsMediaPlayerListView != null)
                    {
                        var addedElems = listviewIsMediaPlayerListView.AddNewElementsToList<Track>(filePaths, out currentCountItemsListView);
                        listviewIsMediaPlayerListView.Playlist.AddRangeTracks(addedElems);
                        listviewIsMediaPlayerListView.Refresh(addedElems);
                        methodAddElemsWithButtons(currentCountItemsListView);

                        return;
                    }

                    if (listviewIsImageListView != null)
                    {
                        var addedElems = listviewIsImageListView.AddNewElementsToList<string>(filePaths, out currentCountItemsListView);
                        listviewIsImageListView.Images.AddRange(addedElems);
                        listviewIsImageListView.Refresh(addedElems);
                        methodAddElemsWithButtons(currentCountItemsListView);

                        return;
                    }

                    if (listviewIsVideoPlayerLIstView != null)
                    {
                        var addedElems = listviewIsVideoPlayerLIstView.AddNewElementsToList<Track>(filePaths, out currentCountItemsListView);
                        listviewIsVideoPlayerLIstView.Playlist.AddRangeTracks(addedElems);
                        listviewIsVideoPlayerLIstView.Refresh(addedElems);
                        methodAddElemsWithButtons(currentCountItemsListView);

                        return;
                    }
                }
            }
        }

        private void AddButtonsForInitialVideoTracks(int startRow)
        {
            if (lstviewVideo.Items.Count == 0)
                return;

            var squareLen = lstviewVideo.Items[0].Bounds.Height;

            //Add icons to the sub-items.
            for (int r = startRow; r < lstviewVideo.Items.Count; r++)
            {
                // Set the sub-item indices.
                for (int c = 1; c < lstviewVideo.Columns.Count; c++)
                {
                    if (c == 3)
                    {
                        var label = new Label();
                        label.Text = "";

                        lstviewVideo.AddEmbeddedControl(label, c, r, DockStyle.Fill);
                    }
                    else if (c == 1)
                    {
                        var butPlayPause = new StartPlayMediaDataButton();
                        butPlayPause.Name = "butPlayPauseVideoTrack";
                        butPlayPause.NumberRowItem = r;
                        butPlayPause.Size = new Size(squareLen, squareLen);
                        butPlayPause.FlatStyle = FlatStyle.Flat;
                        butPlayPause.FlatAppearance.BorderSize = 0;
                        butPlayPause.ImageList = imageListPlayPauseIcons;
                        butPlayPause.ImageAlign = ContentAlignment.MiddleCenter;
                        butPlayPause.ImageIndex = c;
                        butPlayPause.ContentType = MediaDataType.Video;
                        butPlayPause.HandlerClick = butPlayPauseMediaDataUnit_Click;

                        butPlayPause.Click += butPlayPauseMediaDataUnit_Click;
                        lstviewVideo.AddEmbeddedControl(butPlayPause, c, r, DockStyle.None);
                    }
                    else if (c == 2)
                    {
                        var butDelete = new DeleteButton();
                        butDelete.NumberRowDeletedItem = r;
                        butDelete.Size = new Size(squareLen, squareLen);
                        butDelete.FlatStyle = FlatStyle.Flat;
                        butDelete.FlatAppearance.BorderSize = 0;
                        butDelete.ImageList = imageListPlayPauseIcons;
                        butDelete.ImageAlign = ContentAlignment.MiddleCenter;
                        butDelete.ImageIndex = c;
                        butDelete.Tag = lstviewVideo.Playlist.Tracks[r];
                        butDelete.ContentType = MediaDataType.Video;

                        butDelete.Click += butDeleteMediaDataUnit_Click;
                        lstviewVideo.AddEmbeddedControl(butDelete, c, r, DockStyle.None);
                    }
                }
            }
        }

        private void AddButtonsForInitialSoundTracks(int startRow)
        {
            if (lstviewSound.Items.Count == 0)
                return;

            var squareLen = lstviewSound.Items[0].Bounds.Height;

            //Add icons to the sub-items.
            for (int r = startRow; r < lstviewSound.Items.Count; r++)
            {
                // Set the sub-item indices.
                for (int c = 1; c < lstviewSound.Columns.Count; c++)
                {
                    if (c == 3)
                    {
                        var label = new Label();
                        label.Text = "";

                        lstviewSound.AddEmbeddedControl(label, c, r, DockStyle.Fill);
                    }
                    else if (c == 1)
                    {
                        var butPlayPause = new StartPlayMediaDataButton();
                        butPlayPause.Name = "butPlayPauseSoundTrack";
                        butPlayPause.NumberRowItem = r;
                        butPlayPause.Size = new Size(squareLen, squareLen);
                        butPlayPause.FlatStyle = FlatStyle.Flat;
                        butPlayPause.FlatAppearance.BorderSize = 0;
                        butPlayPause.ImageList = imageListPlayPauseIcons;
                        butPlayPause.ImageAlign = ContentAlignment.MiddleCenter;
                        butPlayPause.ImageIndex = c;
                        butPlayPause.ContentType = MediaDataType.Sound;

                        butPlayPause.Click += butPlayPauseMediaDataUnit_Click;
                        lstviewSound.AddEmbeddedControl(butPlayPause, c, r, DockStyle.None);
                    }
                    else if (c == 2)
                    {
                        var butDelete = new DeleteButton();
                        butDelete.NumberRowDeletedItem = r;
                        butDelete.Size = new Size(squareLen, squareLen);
                        butDelete.FlatStyle = FlatStyle.Flat;
                        butDelete.FlatAppearance.BorderSize = 0;
                        butDelete.ImageList = imageListPlayPauseIcons;
                        butDelete.ImageAlign = ContentAlignment.MiddleCenter;
                        butDelete.ImageIndex = c;
                        butDelete.Tag = lstviewSound.Playlist.Tracks[r];
                        butDelete.ContentType = MediaDataType.Sound;

                        butDelete.Click += butDeleteMediaDataUnit_Click;
                        lstviewSound.AddEmbeddedControl(butDelete, c, r, DockStyle.None);
                    }
                }
            }
        }

        private void AddButtonsForInitialMusicTracks(int startRow)
        {
            if (lstviewMusic.Items.Count == 0)
                return;

            var squareLen = lstviewMusic.Items[0].Bounds.Height;
            
            //Add icons to the sub-items.
            for (int r = startRow; r < lstviewMusic.Items.Count; r++)
            {
                // Set the sub-item indices.
                for (int c = 1; c < lstviewMusic.Columns.Count; c++)
                {
                    if (c == 3)
                    {                        
                        var label = new Label();
                        label.Text = "";

                        lstviewMusic.AddEmbeddedControl(label, c, r, DockStyle.Fill);
                    }
                    else if (c == 1)
                    {
                        var butPlayPause = new StartPlayMediaDataButton();
                        butPlayPause.Name = "butPlayPauseMusicTrack";
                        butPlayPause.NumberRowItem = r;
                        butPlayPause.Size = new Size(squareLen, squareLen);
                        butPlayPause.FlatStyle = FlatStyle.Flat;
                        butPlayPause.FlatAppearance.BorderSize = 0;
                        butPlayPause.ImageList = imageListPlayPauseIcons;
                        butPlayPause.ImageAlign = ContentAlignment.MiddleCenter;
                        butPlayPause.ImageIndex = c;
                        butPlayPause.ContentType = MediaDataType.Music;

                        butPlayPause.Click += butPlayPauseMediaDataUnit_Click;
                        lstviewMusic.AddEmbeddedControl(butPlayPause, c, r, DockStyle.None);
                    }
                    else if (c == 2)
                    {
                        var butDelete = new DeleteButton();
                        butDelete.NumberRowDeletedItem = r;
                        butDelete.Size = new Size(squareLen, squareLen);
                        butDelete.FlatStyle = FlatStyle.Flat;
                        butDelete.FlatAppearance.BorderSize = 0;
                        butDelete.ImageList = imageListPlayPauseIcons;
                        butDelete.ImageAlign = ContentAlignment.MiddleCenter;
                        butDelete.ImageIndex = c;
                        butDelete.Tag = lstviewMusic.Playlist.Tracks[r];
                        butDelete.ContentType = MediaDataType.Music;

                        butDelete.Click += butDeleteMediaDataUnit_Click;
                        lstviewMusic.AddEmbeddedControl(butDelete, c, r, DockStyle.None);
                    }
                }
            }
        }

        private void AddButtonsForInitialImages(int startRow)
        {
            if (lstviewImage.Items.Count == 0)
                return;

            var squareLen = lstviewImage.Items[0].Bounds.Height;

            //Add icons to the sub-items.
            for (int r = startRow; r < lstviewImage.Items.Count; r++)
            {
                // Set the sub-item indices.
                for (int c = 1; c < lstviewImage.Columns.Count; c++)
                {
                    if (c == 2)
                    {
                        var label = new Label();
                        label.BackColor = Color.White;
                        label.Text = "";

                        lstviewImage.AddEmbeddedControl(label, c, r, DockStyle.Fill);
                    }
                    else if (c == 1)
                    {
                        var butDelete = new DeleteButton();
                        butDelete.NumberRowDeletedItem = r;
                        butDelete.Size = new Size(squareLen, squareLen);
                        butDelete.FlatStyle = FlatStyle.Flat;
                        butDelete.FlatAppearance.BorderSize = 0;
                        butDelete.ImageList = imageListBackgroundImages;
                        butDelete.ImageAlign = ContentAlignment.MiddleCenter;
                        butDelete.ImageIndex = c;
                        butDelete.Tag = r;

                        butDelete.Click += butDeleteImage_Click;
                        lstviewImage.AddEmbeddedControl(butDelete, c, r, DockStyle.None);
                    }
                }
            }
        }

        private void butDeleteImage_Click(object sender, EventArgs e)
        {
            var button = sender as DeleteButton;

            var numberImageRow = (int)button.Tag;
            lstviewImage.Images.RemoveAt(numberImageRow - 1);
            lstviewImage.RemoveEmbeddedControl(button);

            var sourceCountElems = lstviewImage.Items.Count;
            lstviewImage.Items.RemoveAt(numberImageRow);

            RecountDeleteButtonNumbersForImages(numberImageRow, sourceCountElems);
        }

        private void butUpItemSoundList_Click(object sender, EventArgs e)
        {
            Expression<Func<int, int>> expDecriment = (x) => x - 1;
            var valueDecriment                      = expDecriment.Compile();
            Expression<Func<int, int, int>> expr = (x, y) => valueDecriment(x) >= 0 ? valueDecriment(x) : y;

            lstviewSound.GetIndexByExpression(expr, butPlayPauseMediaDataUnit_Click);
        }

        private void butDownItemSoundList_Click(object sender, EventArgs e)
        {
            Expression<Func<int, int>> expIncrement = (x) => x + 1;
            var valueIncrement                      = expIncrement.Compile();
            Expression<Func<int, int, int>> expr    = (x, y) => valueIncrement(x) <= y ? valueIncrement(x) : 0;

            lstviewSound.GetIndexByExpression(expr, butPlayPauseMediaDataUnit_Click);
        }

        private void butUpItemMusicList_Click(object sender, EventArgs e)
        {
            Expression<Func<int, int>> expDecriment = (x) => x - 1;
            var valueDecriment                      = expDecriment.Compile();
            Expression<Func<int, int, int>> expr = (x, y) => valueDecriment(x) >= 0 ? valueDecriment(x) : y;

            lstviewMusic.GetIndexByExpression(expr, butPlayPauseMediaDataUnit_Click);
        }

        private void butDownItemMusicList_Click(object sender, EventArgs e)
        {
            Expression<Func<int, int>> expIncrement = (x) => x + 1;
            var valueIncrement                      = expIncrement.Compile() ;
            Expression<Func<int, int, int>> expr    = (x, y) => valueIncrement(x) <= y ? valueIncrement(x) : 0;

            lstviewMusic.GetIndexByExpression(expr, butPlayPauseMediaDataUnit_Click);
        }

        private void butUpItemVideoList_Click(object sender, EventArgs e)
        {
            Expression<Func<int, int>> expDecriment = (x) => x - 1;
            var valueDecriment                      = expDecriment.Compile();
            Expression<Func<int, int, int>> expr = (x, y) => valueDecriment(x) >= 0 ? valueDecriment(x) : y;

            lstviewVideo.GetIndexByExpression(expr, butPlayPauseMediaDataUnit_Click);
        }

        private void butDownItemVideoList_Click(object sender, EventArgs e)
        {
            Expression<Func<int, int>> expIncrement = (x) => x + 1;
            var valueIncrement                      = expIncrement.Compile();
            Expression<Func<int, int, int>> expr    = (x, y) => valueIncrement(x) <= y ? valueIncrement(x) : 0;

            lstviewVideo.GetIndexByExpression(expr, butPlayPauseMediaDataUnit_Click);
        }

        /* TODO: Переписать нахрен этот говногод */

        private void RecountDeleteButtonNumbersForImages(int startRow, int sourceCountElements)
        {
            for (int row = startRow + 1; row < sourceCountElements; row++)                              // + 1, because we don't consider the row which was deleted
            {
                for (int col = 1; col < lstviewImage.Columns.Count - 1; col++)
                {
                    if (col == 1)                                                                       // The Delete button
                    {
                        var but = lstviewImage.GetEmbeddedControl(col, row) as DeleteButton;
                        but.NumberRowDeletedItem = row - 1;
                        but.Tag = (int)(row - 1);
                        lstviewImage.RemoveEmbeddedControl(lstviewImage.GetEmbeddedControl(col, row));
                        lstviewImage.AddEmbeddedControl(but, col, row - 1);
                    }
                }

            }
        }

        private void SizeLastColumn(ListView listView)
        {
            var countColumns = listView.Columns.Count;
            if (countColumns == 0)
                return;

            listView.Columns[countColumns - 1].Width = -2;
        }

        private void SenderView_FormClosing(object sender, FormClosingEventArgs e)
        {
            var listImages      = lstviewImage.Images;
            var listMessages    = lstviewMessage.Messages;
            var listVideoTracks = lstviewVideo.Playlist.Tracks;
            var listSoundTracks = lstviewSound.Playlist.Tracks;
            var listMusicTracks = lstviewMusic.Playlist.Tracks;

            listImages.SerializeListData(_pathListBackgroundImages);
            listMessages.SerializeListData(_pathListStorageMessages);
            listVideoTracks.SerializeListData(_pathPlaylistVideoTracks);
            listSoundTracks.SerializeListData(_pathPlaylistSoundTracks);
            listMusicTracks.SerializeListData(_pathPlaylistMusicTracks);

            SerializeManager.SerializeListData<Settings>(ConfigurationSettings, _pathListStorageSettings);
        }

        private void richtxtboxMessage_TextChanged(object sender, EventArgs e)
        {
            var richTextBox = sender as RichTextBox;

            var lengthMessage = richTextBox.Text.Length;
            if (lengthMessage < 1)
            {
                linklblAttachImage.Enabled = true;
                DataSending.Message = richTextBox.Text;
            }
            else if (lengthMessage >= 1)
            {
                linklblAttachImage.Enabled = false;
                DataSending.Message = richTextBox.Text;
            }
        }

        private void butTextTypeItalic_Click(object sender, EventArgs e)
        {
            if (butTextTypeItalic.Tag == null)
                butTextTypeItalic.Tag = 0;

            if ((int)butTextTypeItalic.Tag == 0)
            {
                butTextTypeItalic.Tag = 1;
                butTextTypeItalic.BackColor = Color.DodgerBlue;
                butTextTypeItalic.ForeColor = Color.White;
            }
            else
            {
                butTextTypeItalic.BackColor = Color.White;
                butTextTypeItalic.ForeColor = Color.Black;
                butTextTypeItalic.Tag = 0;
            }
        }

        private void butTextTypeBold_Click(object sender, EventArgs e)
        {
            if (butTextTypeBold.Tag == null)
                butTextTypeBold.Tag = 0;

            if ((int)butTextTypeBold.Tag == 0)
            {
                butTextTypeBold.Tag = 1;
                butTextTypeBold.BackColor = Color.DodgerBlue;
                butTextTypeBold.ForeColor = Color.White;
            }
            else
            {
                butTextTypeBold.BackColor = Color.White;
                butTextTypeBold.ForeColor = Color.Black;
                butTextTypeBold.Tag = 0;
            }
        }

        private void butAcceptTextSettings_Click(object sender, EventArgs e)
        {
            ConfigurationSettings.FontFamilyText = cmbBoxTextFont.SelectedItem.ToString();
            ConfigurationSettings.SizeText       = (int)numUpDownTextSize.Value;
            ConfigurationSettings.ForeColorText  = cmbBoxListColors.SelectedItem.ToString();

            if ((int)butTextTypeItalic.Tag == 1 &&  (int)butTextTypeBold.Tag == 1)
                ConfigurationSettings.TextStyle = TypeText.ItalicBold;
            else if ((int)butTextTypeItalic.Tag == 1 && (int)butTextTypeBold.Tag == 0)
                ConfigurationSettings.TextStyle = TypeText.Italic;
            else if ((int)butTextTypeItalic.Tag == 0 && (int)butTextTypeBold.Tag == 1)
                ConfigurationSettings.TextStyle = TypeText.Bold;
            else if ((int)butTextTypeItalic.Tag == 0 && (int)butTextTypeBold.Tag == 0)
                ConfigurationSettings.TextStyle = TypeText.None;

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:  // Fullscreen Mode
                    {
                        // some code
                    }
                    break;
                case ModeViewSender.RemoteScreen:       // Remote Mode
                    {
                        if (_isConnectedToReciver)
                        {
                            Connect();
                            Presenter.OnButtonAccept(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:         // Chromecast Mode
                    {
                        // some code
                    }
                    break;
                default:
                    break;
            }
        }

        private void butAcceptOtherSettings_Click(object sender, EventArgs e)
        {
            if (CurrentTimerStatus == TimerStatus.Play || CurrentTimerStatus == TimerStatus.Pause)
            {
                MessageBox.Show("Please, try again after the timer will be stopped!");
                return;
            }

            ConfigurationSettings.StartTimer = datetimepickerStartValue.Value = dateTimePickerStartTimeByDefault.Value;

            #region Валидация введенных данных в блок Other Settings

            IPAddress remoteConnectionIPAddress;

            var remoteConnectionPort     = _portReceiver;
            var chromecastConnectionPort = _portChromecast;

            var strBuilder = new StringBuilder("Errors:");
            strBuilder.AppendLine();

            if (ushort.TryParse(txtBoxRemoteConnPortByDefault.Text, out remoteConnectionPort))
            {
                ConfigurationSettings.RemoteConnectionPort = remoteConnectionPort;
                txtBoxPort.Text = remoteConnectionPort.ToString();
            }
            else
            {
                strBuilder.AppendLine(String.Format("An incorrect port of the remote machine: {0}", txtBoxRemoteConnPortByDefault.Text));
                txtBoxRemoteConnPortByDefault.Text = ConfigurationSettings.RemoteConnectionPort.ToString();
            }

            if (ushort.TryParse(txtBoxChromecastConnPortByDefault.Text, out chromecastConnectionPort))
            {
                ConfigurationSettings.ChromecastConnectionPort = chromecastConnectionPort;
            }
            else
            {
                strBuilder.AppendLine(String.Format("An incorrect port of the chromecast device: {0}", txtBoxChromecastConnPortByDefault.Text));
                txtBoxChromecastConnPortByDefault.Text = ConfigurationSettings.ChromecastConnectionPort.ToString();
            }

            if (IPAddress.TryParse(txtboxIpAddressRemoteConnByDefault.Text, out remoteConnectionIPAddress))
            {
                ConfigurationSettings.IpAddressRemoteMachine = remoteConnectionIPAddress;
                txtBoxIP.Text = remoteConnectionIPAddress.ToString();
            }
            else
            {
                strBuilder.AppendLine(String.Format("An incorrect ip address of the remote machine: {0}", txtboxIpAddressRemoteConnByDefault.Text));
                txtboxIpAddressRemoteConnByDefault.Text = ConfigurationSettings.IpAddressRemoteMachine.ToString();
            }

            if (strBuilder.ToString().Length > 10)
                MessageBox.Show(strBuilder.ToString());

            #endregion
        }

        private void butResetTextSettings_Click(object sender, EventArgs e)
        {
            // TODO: сделать через конструктор
            ConfigurationSettings.FontFamilyText = @"Times New Roman";
            ConfigurationSettings.SizeText       = 22;
            ConfigurationSettings.TextStyle      = TypeText.None;
            ConfigurationSettings.ForeColorText = "Black";

            numUpDownTextSize.Value       = ConfigurationSettings.SizeText;

            var colors     = Enum.GetValues(typeof(KnownColor));
            var dictColors = new Dictionary<string, KnownColor>();

            foreach (var knowColor in colors)
            {
                dictColors[knowColor.ToString()] = (KnownColor)knowColor;
            }

            cmbBoxListColors.SelectedItem = dictColors[ConfigurationSettings.ForeColorText];

            cmbBoxTextFont.SelectedItem   = ConfigurationSettings.FontFamilyText;

            butTextTypeItalic.BackColor = Color.White;
            butTextTypeItalic.ForeColor = Color.Black;
            butTextTypeItalic.Tag       = 0;

            butTextTypeBold.BackColor = Color.White;
            butTextTypeBold.ForeColor = Color.Black;
            butTextTypeBold.Tag       = 0;
        }

        private void butResetOtherSettings_Click(object sender, EventArgs e)
        {
            ConfigurationSettings.StartTimer               = new DateTime(2000, 1, 1, 1, 0, 0, 0); // TODO: Create a datetime value by default (Rework)
            ConfigurationSettings.RemoteConnectionPort     = _portReceiver;
            ConfigurationSettings.ChromecastConnectionPort = _portChromecast;

            // TODO: Get an IP address from a configuration file
            txtBoxPort.Text          = ConfigurationSettings.RemoteConnectionPort.ToString();
            txtBoxIP.Text            = ConfigurationSettings.IpAddressRemoteMachine.ToString();
            txtboxIpAddressRemoteConnByDefault.Text = txtBoxIP.Text;

            dateTimePickerStartTimeByDefault.Value = ConfigurationSettings.StartTimer;
            txtBoxRemoteConnPortByDefault.Text     = ConfigurationSettings.RemoteConnectionPort.ToString();
            txtBoxChromecastConnPortByDefault.Text = ConfigurationSettings.ChromecastConnectionPort.ToString();
        }

        private static void CreateFileIfNotExist(string path, string directory)
        {
            if (!Directory.Exists(directory)) 
                Directory.CreateDirectory(directory);

            using (StreamWriter sw = File.AppendText(path))
            {
                sw.Write(string.Empty);
            }
        }

        private void butGoToPreview_Click(object sender, EventArgs e)
        {
            if (lstviewImage.Images.Count == 0 || lstviewImage.SelectedItems.Count == 0 || (lstviewImage.SelectedItems.Count == 1 && lstviewImage.SelectedItems[0].Index == 0))
                return;

            lstviewImage.Items[lstviewImage.SelectedItems[0].Index].Selected = true;
            lstviewImage.Select();

            var index                  = lstviewImage.SelectedItems[0].Index;
            var nameImage              = lstviewImage.Images[index - 1];
            var imageFilePath          = dirImageFiles + @"\" + nameImage;
            picboxBgImagePreview.Image = new Bitmap(imageFilePath);
            
            picboxBgImagePreview.ForceImageIntoPictureBox(
                                         BackgroundImagePictureBoxSize.Width,
                                         BackgroundImagePictureBoxSize.Height,
                                         imageFilePath);

            picboxBgImagePreview.Tag = imageFilePath;
        }

        private void lblResetBgImage_Click(object sender, EventArgs e)
        {
            picboxBgImagePreview.Width     = BackgroundImagePictureBoxSize.Width;
            picboxBgImagePreview.Height    = BackgroundImagePictureBoxSize.Height;
            picboxBgImagePreview.Image     = null;
            picboxBgImagePreview.BackColor = Color.DarkRed;

            if (lstviewImage.Images.Count != 0 && lstviewImage.SelectedItems.Count >= 1)
            {
                lstviewImage.SelectedItems[0].Selected = false;
                lstviewImage.Select();
            }

            picboxBgImagePreview.Tag = null;

            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    {
                        Presenter.OnButtonResetBackImageLocalReceiver();
                    }
                    break;
                case ModeViewSender.RemoteScreen:
                    {
                        Presenter.OnButtonResetBgImageRemoteReceiver();
                    }
                    break;
                case ModeViewSender.Chromecast:
                    {
                        // your code
                    }
                    break;
                default:
                    //if (!_hasSelectedMode) MessageBox.Show("Please, select one of the modes!");
                    break;
            }
        }

        private void butSendBackImage_Click(object sender, EventArgs e)
        {
            switch (CurrentModeViewSender)
            {
                case ModeViewSender.MonitorFullScreen:
                    {
                        var imageFileName = (string)picboxBgImagePreview.Tag;
                        if (!string.IsNullOrEmpty(imageFileName))
                        {
                            Presenter.OnButtonSetAsBackgroundImage(sender, new BackgroundImageEventArgs(imageFileName));
                        }
                    }
                    break;
                case ModeViewSender.RemoteScreen:
                    {
                        var imageFileName = (string)picboxBgImagePreview.Tag;
                        if (!string.IsNullOrEmpty(imageFileName) && _isConnectedToReciver)
                        {
                            Connect();
                            DataSending.ImageFileName = imageFileName;
                            DataSending.Command = Command.BackgroundImage;
                            Presenter.OnButtonSetBackgroundImage(sender, e);
                        }
                    }
                    break;
                case ModeViewSender.Chromecast:
                    {

                    }
                    break;
                default:
                    MessageBox.Show("Please, select one of the modes!");
                    break;
            }
        }

        private void butDeleteMessage_Click(object sender, EventArgs e)
        {
            lstviewMessage.SuspendLayout();

            if (lstviewMessage.SelectedItems.Count != 0)
            {
                var selectedItemIndex = lstviewMessage.SelectedIndices[0];
                lstviewMessage.Messages.RemoveAt(selectedItemIndex);
                lstviewMessage.SelectedItems[0].Remove();
            }

            lstviewMessage.ResumeLayout();
        }

        private void checkBoxOnOffScreen_CheckStateChanged(object sender, EventArgs e)
        {
            if (!checkBoxOnOffScreen.Checked)
                linklblReset_LinkClicked(sender, new LinkLabelLinkClickedEventArgs(linklblResetAllModes.Links[0]));

            foreach (Control control in tabsOther.Controls)
            {
                control.Enabled = checkBoxOnOffScreen.Checked;
            }

            foreach (Control control in grboxTimer.Controls)
            {
                control.Enabled = checkBoxOnOffScreen.Checked;
            }

            foreach (Control control in grBoxSendMessage.Controls)
            {
                control.Enabled = checkBoxOnOffScreen.Checked;
            }

            foreach (Control control in grboxParameters.Controls)
            {
                control.Enabled = checkBoxOnOffScreen.Checked;
            }

            // Temporary code
            butChromecast.Enabled = false;
        }

        private void GoFromLocalScreenToRemoteOrChromecastScreen()
        {
            var eventArgs   = new EventArgs();
            var obj         = new object();

            ResetComponentsToInitialValues(obj, eventArgs);
            Presenter.OnButtonCloseFormWithTextOrImage(obj, eventArgs);
            Presenter.OnButtonCloseDisplayForm(obj, eventArgs);
        }

        private void GoFromRemoteScreenToLocalOrChromecastScreen()
        {
            var eventArgs   = new EventArgs();
            var obj         = new object();

            // Send message to reciver in order to close all audio

            // May be asynchronly start

            if (_isConnectedToReciver)
            {
                //await Task.Run( () => Presenter.OnButtonCloseAllSecondaryForms(obj, eventArgs) ); вызывает ошибку

                Presenter.OnButtonCloseAllSecondaryForms(obj, eventArgs);
                Presenter.OnButtonCloseAllAudio(obj, eventArgs);

                // Сброс кнопок должен пройти здесь!
                //var playableVideoTrackNumber = lstviewVideo.Playlist.PlayableTrackNumber;
                //butPlayPauseMediaDataUnit_Click(lstviewVideo.GetEmbeddedControl(1, (playableVideoTrackNumber) % lstviewVideo.Playlist.TracksCount), new EventArgs());

                _isConnectedToReciver = false;
            }
            
            ResetComponentsToInitialValues(obj, eventArgs);
        }

        private void ResetComponentsToInitialValues(object obj, EventArgs eventArgs)
        {
            richtxtboxMessage.Text = string.Empty;

            butClear_Click(obj, eventArgs);

            // Доделать!!!
            lblResetBgImage_Click(obj, eventArgs);
           
            //butResetTextSettings_Click(obj, eventArgs);
            //butResetOtherSettings_Click(obj, eventArgs);
           
            SetUpTimerToOriginalState();

            butAcceptOtherSettings_Click(obj, eventArgs);

            var mediaDataTypes = new[] { MediaDataType.Video, MediaDataType.Music, MediaDataType.Sound };

            foreach (var contentType in mediaDataTypes)
            {
                var listview     = dictionaryListViewPlayers[contentType];
                var senderPlayer = dictionarySenderPlayers[contentType];

                if (listview.Playlist.State == PlaylistTrackStates.Play)
                {
                    var currentMediaData = listview.Playlist.PlayableTrackNumber;
                    butPlayPauseMediaDataUnit_Click(listview.GetEmbeddedControl(1, currentMediaData), eventArgs);
                }

                senderPlayer.controls.stop();
            }
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

        private void cmbBoxMonitorNumber_SelectedIndexChanged(object sender, EventArgs e)
        {
            Presenter.OnChangeSelectedMonitor(sender, e);
        }

        private void tabsOther_Selected(object sender, TabControlEventArgs e)
        {
            var tabControl =  sender as System.Windows.Forms.TabControl;

            foreach (Control control in tabControl.Controls)
            {
                control.Refresh();
            }
        }

        //private void txtboxIpAddressRemoteConnByDefault_KeyPress(object sender, KeyPressEventArgs e)
        //{

        //    (sender as TextBox).HasOnlyIpAddress(e);

        //    /* Некоторые замечания:
        //       1. Символ с номером 8 - это Backspace (удаление или стирание символа)
        //    */

        //    // Значение текстового поля
        //    string txtBoxValue = (sender as TextBox).Text;
        //    // Регулярное выражение (нужный шаблон)
        //    var _template = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";

        //    var _templ  = @"[0-9]*|[.]";

        //    // Нажатый символ
        //    var keyPressValue = e.KeyChar;
        //    // Код нажатого символа
        //    var keyPressCharCode = (int)keyPressValue;

        //    // Проверка, является ли значение в текстовом поле строкой символов как в шаблоне
        //    var isMatchTemplate = Regex.Match(txtBoxValue + keyPressValue, _templ).Success;

        //    if (Regex.Match(txtBoxValue, @"[.]").Length == 4 & txtBoxValue.Length <= 15 & txtBoxValue.Length >= 13)
        //    {
        //        isMatchTemplate = Regex.Match(txtBoxValue + keyPressValue, _template).Success;
        //    }

        //    // Проверка условий:

        //    // Не Backspace и текущее значение поля вместе с нажатым символом соотвествует шаблону
        //    if (keyPressCharCode != 8 && isMatchTemplate)
        //    {
        //        // Указываем, что выполнить действие нажатой клавиши до конца
        //        e.Handled = false;
        //    }
        //    // Если Backspace, то ...
        //    else if ((keyPressCharCode == 8))
        //    {
        //        // ... выполнить действие нажатой клавиши до конца
        //        e.Handled = false;
        //    }
        //    // Иначе, ...
        //    else
        //    {
        //        // не выполнять действие нажатой клавиши, считаем, что поле обработано
        //        e.Handled = true;
        //    }
        //}
    }
}