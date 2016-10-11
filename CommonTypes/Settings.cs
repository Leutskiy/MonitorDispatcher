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
        public string    FontFamilyText           { get; set; }

        public int       SizeText                 { get; set; }

        public TypeText  TextStyle                { get; set; }

        public DateTime  StartTimer               { get; set; }

        public ushort    RemoteConnectionPort     { get; set; }

        public ushort    ChromecastConnectionPort { get; set; }

        public IPAddress IpAddressRemoteMachine   { get; set; }

        public string     ForeColorText           { get; set; }

        private static readonly string    _fontFamilyText;
        private static readonly int       _sizeText;
        private static readonly TypeText  _textStyle;
        private static readonly DateTime  _startTimer;
        private static readonly ushort    _portRemoteConnection;
        private static readonly ushort    _portChromecastConnection;
        private static readonly IPAddress _ipAddressRemoteMachine;
        public  static readonly string     _foreColorText;

        static Settings()
        {
            _fontFamilyText           = @"Times New Roman";
            _sizeText                 = 24;
            _textStyle                = TypeText.None;
            _startTimer               = new DateTime(2000, 1, 1, 0, 0, 0, 0);
            _portRemoteConnection     = 12000;
            _portChromecastConnection = 8008;
            _ipAddressRemoteMachine   = IPAddress.Parse("127.0.0.1");
            _foreColorText            = "Black";
        }

        public Settings()
        {
            SetValuesByDefault();
        }

        public void SetValuesByDefault()
        {
            FontFamilyText           = _fontFamilyText;
            SizeText                 = _sizeText;
            TextStyle                = _textStyle;
            StartTimer               = _startTimer;
            RemoteConnectionPort     = _portRemoteConnection;
            ChromecastConnectionPort = _portChromecastConnection;
            IpAddressRemoteMachine   = _ipAddressRemoteMachine;
            ForeColorText            = _foreColorText;
        }

        public static Settings DeserializeSettings(string pathSerializableDatFileWithSettings)
        {
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
    }
}
