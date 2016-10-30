using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommonTypes
{
    [Serializable]
    public enum TypeText : sbyte
    {
        None       = 0,
        Italic     = 1,
        Bold       = 2,
        ItalicBold = 3
    }

    [Serializable]
    public class Settings
    {
        #region Text Settings

        public string    FontFamilyText           { get; set; }

        public int       SizeText                 { get; set; }

        public TypeText  TextStyle                { get; set; }

        public string    ForeColorText            { get; set; }

        public DateTime  StartTimer               { get; set; }

        #endregion

        #region Connection Settings

        public ushort    RemoteConnectionPort     { get; set; }

        public ushort    ChromecastConnectionPort { get; set; }

        public IPAddress IpAddressRemoteMachine   { get; set; }

        #endregion

        #region Timer Settings

        /// <summary>
        /// Шрифт цифр таймера
        /// </summary>
        public string FontFamilyTimer { get; set; }

        /// <summary>
        /// Размер цифр таймера
        /// </summary>
        public int    SizeTimer       { get; set; }

        /// <summary>
        /// Фон цифр таймера
        /// </summary>
        public string BackColorTimer  { get; set; }

        /// <summary>
        /// Цвет цифр таймера
        /// </summary>
        public string ForeColorTimer  { get; set; }

        #endregion

        private static readonly string    _fontFamilyText;
        private static readonly int       _sizeText;
        private static readonly TypeText  _textStyle;
        public  static readonly string    _foreColorText;
        
        private static readonly ushort    _portRemoteConnection;
        private static readonly ushort    _portChromecastConnection;
        private static readonly IPAddress _ipAddressRemoteMachine;

        private static readonly DateTime  _startTimer;
        private static readonly string    _fontFamilyTimer;
        private static readonly int       _sizeTimer;
        public  static readonly string    _backColorTimer;
        public  static readonly string    _foreColorTimer;

        static Settings()
        {
            _fontFamilyText           = @"Times New Roman";
            _sizeText                 = 24;
            _textStyle                = TypeText.None;
            _foreColorText            = "Black";
            
            _portRemoteConnection     = 12000;
            _portChromecastConnection = 8008;
            _ipAddressRemoteMachine   = IPAddress.Parse("127.0.0.1");
            
            _startTimer               = new DateTime(2000, 1, 1, 0, 0, 0, 0);
            _fontFamilyTimer          = @"Arial";
            _sizeTimer                = 48;
            _backColorTimer           = "Black";
            _foreColorTimer           = "White";
        }

        public Settings()
        {
            SetTextValuesByDefault();
            SetConnectionValuesByDefault();
            SetTimerValuesByDefault();
        }

        public void SetTextValuesByDefault()
        {
            FontFamilyText = _fontFamilyText;
            SizeText       = _sizeText;
            TextStyle      = _textStyle;
            ForeColorText  = _foreColorText;

           
        }

        public void SetConnectionValuesByDefault()
        {
            RemoteConnectionPort     = _portRemoteConnection;
            ChromecastConnectionPort = _portChromecastConnection;
            IpAddressRemoteMachine   = _ipAddressRemoteMachine;

        }

        public void SetTimerValuesByDefault()
        {
            StartTimer      = _startTimer;
            FontFamilyTimer = _fontFamilyTimer;
            SizeTimer       = _sizeTimer;
            BackColorTimer  = _backColorTimer;
            ForeColorTimer  = _foreColorTimer;
        }

        public static Settings DeserializeSettings(string pathSerializableDatFileWithSettings)
        {
            // todo: ответить на вопрос, загружается ли файл вместе с объектом потока в память или только хэндл на файл?
            using (Stream fs = new FileStream(pathSerializableDatFileWithSettings, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                if (fs.Length == 0)
                    return null;

                var formatter = new BinaryFormatter();
                var settings  = formatter.Deserialize(fs) as Settings;

                return settings;
            }
        }

        private FontStyle GetStyleTextByTypeText(TypeText typeTxt)
        {
            FontStyle style;                                    // стиль:
            switch (typeTxt)
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

            return style;
        }

        public Font GetFontForMessage()
        {
            return new Font(FontFamilyText, SizeText, GetStyleTextByTypeText(TextStyle));
        }

        public Font GetFontForTimer()
        {
            return new Font(FontFamilyTimer, SizeTimer, FontStyle.Regular);
        }
    }
}