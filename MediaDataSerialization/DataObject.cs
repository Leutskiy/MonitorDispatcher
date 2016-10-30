using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace MediaDataSerialization
{
    using CommonTypes;

    public enum AutoPlayState : byte
    {
        NothingIsPlaying = 0,
        NextTrack        = 1,
        StopTrack        = 2,
        StopVideoTrack   = 3
    }

    [Serializable]
    public struct AutoPlay
    {
        public AutoPlayState State;

        public AutoPlay(AutoPlayState state)
        {
            State = state;
        }

        public override string ToString()
        {
            return State.ToString();
        }
    }

    public enum TimerStatus : byte
    {
        None  = 0,
        Play  = 1,
        Pause = 2,
        End   = 3
    }

    [Serializable]
    public class Timer : ICloneable
    {
        private int _hours;
        private int _minutes;
        private int _seconds;

        public TimerStatus State     { get; set; }
        public Command     command   { get; set; }

        public Timer()
            : this(0, 0, 0)
        {

        }
        public Timer(int h, int min, int sec)
            : this(h, min, sec, Command.Time)
        {

        }

        public Timer(int h, int min, int sec, Command cmd) 
            : this(h, min, sec, cmd, TimerStatus.None)
        {
           
        }

        public Timer(int h, int min, int sec, Command cmd, TimerStatus state)
        {
            _hours   = h;
            _minutes = min;
            _seconds = sec;
            command  = cmd;
            State    =  state;
        }

        public int Hours
        {
            get { return _hours; }
            set
            {
                if (0 <= value && value < 60)
                    _hours = value;
                else if (value == 60)
                    _hours = value % 60;
                else if (value < 0)
                    _hours = 0;
            }
        }

        public int Minutes
        {
            get { return _minutes; }
            set
            {
                if (0 <= value && value < 60)
                    _minutes = value;
                else if (value == 60)
                {
                    _minutes = value % 60;
                    if (_hours == 59)
                        _hours = _minutes;
                    else
                        ++_hours;
                }
                else if (value < 0)
                {
                    --_hours;
                    if (_hours < 0)
                    {
                        _hours++;
                        _minutes = 0;
                    }
                    else
                        _minutes = 59;
                }
            }
        }

        public int Seconds
        {
            get { return _seconds; }
            set
            {
                if (0 <= value && value < 60)
                    _seconds = value;
                else if (value == 60)
                {
                    _seconds = value % 60;
                    if (_minutes == 59)
                        _minutes = _seconds;
                    else
                        ++_minutes;
                }
                else if (value < 0)
                {
                    --_minutes;
                    if (_minutes < 0)
                    {
                        --_hours;

                        if (_hours < 0)
                        {
                            _hours++;
                            _minutes++;
                            _seconds = 0;

                            State = TimerStatus.End;
                        }
                        else
                        {
                            _minutes = 59;
                            _seconds = 59;
                        }
                    }
                    else
                        _seconds = 59;
                }
            }
        }

        /// <summary>
        /// Оригинальный метод C#
        /// Реализация интерфейса ICloneable для создания копии объекта
        /// </summary>
        public Object Clone()
        {
            // Создаем клон
            Timer clone = new Timer();

            clone.command = this.command;
            clone.Hours   = this.Hours;
            clone.Minutes = this.Minutes;
            clone.Seconds = this.Seconds;

            return clone;
        }

        public override string ToString()
        {
            var time = "";

            if (0 <= _hours && _hours <= 9)
                time += "0" + _hours.ToString();
            else
                time += _hours.ToString();

            time += ":";

            if (0 <= _minutes && _minutes <= 9)
                time += "0" + _minutes.ToString();
            else
                time += _minutes.ToString();

            time += ":";

            if (0 <= _seconds && _seconds <= 9)
                time += "0" + _seconds.ToString();
            else if (_seconds > 9)
                time += _seconds.ToString();
            else if (_seconds < 0)
                time = "00:00:00";

            return time;
        }
    }

    /// <summary>
    /// Сериализуемый объект для транспортировки
    /// </summary>
    [Serializable()]
    public class DataObject
    {
        // Поля с сохраняемыми данными
        Command command;        // Идентификатор команды
        string displayCommand;  // Представление команды для пользователя
        string text;            // Для передачи текстовых данных
        string logName;         // Имя файла с журналом
        string imageName;       // Имя файла с рисунком
        string mediaDataName;       // Имя файла с медиа данными (музыка/видео/звук)
        Timer time;             // Время таймера
        Stream image;           // Сам рисунок
        Stream mediaContent;    // Сами медиа данные

        // Свойства доступа к полям
        public Timer Time
        {
            get { return time; }
            set { time = value; }
        }
        // Свойства доступа к полям
        public Command Command
        {
            get { return command; }
            set { command = value; }
        }
        // Строковый тип
        public string DisplayCommand
        {
            get { return displayCommand; }
            set { displayCommand = value; }
        }
        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        public string LogName
        {
            get { return logName; }
            set { logName = value; }
        }
        public string ImageName
        {
            get { return imageName; }
            set { imageName = value; }
        }
        public string MediaDataName
        {
            get { return mediaDataName; }
            set { mediaDataName = value; }
        }

        // Потоковый тип
        public Stream Image
        {
            get { return image; }
            set { image = value; }
        }
        public Stream MediaData
        {
            get { return mediaContent; }
            set { mediaContent = value; }
        }

        /// <summary>
        /// Альтернативный метод создания копии объекта 
        /// на основе шаблона, потока памяти и сериализации.
        /// А он мне нравится, нравится, нравится...!!!!!!!
        /// </summary>
        public T BinaryClone<T>()
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var binaryFormatter = new System.Runtime.Serialization.
                    Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, this);
                stream.Position = 0;

                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}
