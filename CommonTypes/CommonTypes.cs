using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    /// <summary>
    /// Перечисление команд, которые могут передаваться серверу на исполнение
    /// </summary>
    [Serializable()]
    public  enum Command
    {
        /// <summary> Отсутствие команды </summary>
        None,
        /// <summary> Добавить сообщение в журнал </summary>
        AddTextInLog,
        /// <summary> Прислать журнал </summary>
        Log,
        /// <summary> Удалить журнал </summary>
        DeleteLog,
        /// <summary> Прислать сообщение </summary>
        Text,

        /// <summary> Начать воспроизведение звука </summary>
        SoundPlay,
        /// <summary> Приостановить воспроизведение звука </summary>
        SoundPause,
        /// <summary> Продолжить воспроизведение звука </summary>
        SoundPausePlay,
        /// <summary> Остановить и начать воспроизведение звука сначала </summary>
        SoundStopPlay,
        /// <summary> Остановить воспроизведение звука </summary>
        SoundStop,

        /// <summary> Прислать трек и воспроизвести его </summary>
        MusicPlay,
        /// <summary> Приостановить текущий трек ресивера </summary>
        MusicPause,
        /// <summary> Снова воспроизвести приостановленный трек ресивера </summary>
        MusicPausePlay,
        /// <summary> Остановить текущий трек на ресивере и начать воспроизводить новый </summary>
        MusicStopPlay,
        /// <summary> Остановить текущий трек на ресивере </summary>
        MusicStop,

        /// <summary> Прислать трек и воспроизвести его </summary>
        VideoPlay,
        /// <summary> Приостановить текущий трек ресивера </summary>
        VideoPause,
        /// <summary> Снова воспроизвести приостановленный трек ресивера </summary>
        VideoPausePlay,
        /// <summary> Остановить текущий трек на ресивере и начать воспроизводить новый </summary>
        VideoStopPlay,
        /// <summary> Остановить текущий трек на ресивере </summary>
        VideoStop,


        /// <summary> Прислать картинку </summary>
        Image,
        /// <summary> Передать фоновое изображение </summary>
        BackgroundImage,
        
        /// <summary>Время</summary>
        Time,
        /// <summary> Закрыть все формы, порожденные от первичной формы </summary>
        CloseAllSecondaryForms,
        /// <summary> Прекратить звучание и убрать фоновые картинки</summary>
        StopSoundAndDeleteBackgroundImage,
        /// <summary> Прислать настройки для формы с картинкой или текста </summary>
        Settings,

        /// <summary> Сбросить фоновое изображение </summary>
        ResetBgImage
    }

    public enum PlayerActions : byte
    {
        None,
        Pause,
        PausePlay,
        StopPlay
    }
}
