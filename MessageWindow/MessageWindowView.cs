/* Библиотеки фреймфорка дот нет  */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Drawing.Text;

namespace MessageWindow
{
    /* Библиотеки проекта */
    using MessageWindow.ExControls;
    using CommonTypes;

    public partial class MessageWindowForm : Form
    {
        #region Информация

        /* 1pt (типографский пункт) = 4/3 px */
        /* 1cm (см) = 38px                   */

        #endregion

        #region Различные параметры

        /// <summary>
        /// Начальное количество слов в первой строке информационного окна с сообщением
        /// </summary>
        private const int _startCountWordsInFirstRow = 10;

        /// <summary>
        /// Ожидаемое количество строк в информационном окне с сообщением
        /// </summary>
        private int    _numberRows     = 0;

        /// <summary>
        /// Сообщение
        /// </summary>
        private string _textMessage    = string.Empty;

        /// <summary>
        /// Шрифт текста, отображаемого в информационном окне с сообщением
        /// </summary>
        private Font   _fontMessage;

        /// <summary>
        /// Цвет текста, отображаемого в информационном окне сообщения
        /// </summary>
        private string _foreColorText;

        /// <summary>
        /// Ширина экрана, на котором будет показано сообщение
        /// </summary>
        private readonly int _screenWidth;

        /// <summary>
        /// Высота экрана, на котором будет показано  сообщение
        /// </summary>
        private readonly int _screenHeight;

        /// <summary>
        /// Количество слов в первой строке сообщения
        /// </summary>
        private int _countWordsInFirstRowInfoMessage = 0;

        #endregion

        #region Конструкторы

        /// <summary>
        /// Конструктор по умолчанию (без параметров)
        /// </summary>
        public MessageWindowForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор с параметром
        /// </summary>
        /// <param name="text"> Текст для отображения в сообщении </param>
        public MessageWindowForm(string text) : this(text, null)
        {
            // По умолчанию фоновый цвет формы азурный
            this.BackColor = Color.Azure;
        }

        /// <summary>
        /// Констркутор с двумя параметрами
        /// </summary>
        /// <param name="text"> Текст для отображения в информационном окне </param>
        /// <param name="settings"> Настройки для установки параметров сообщения </param>
        public MessageWindowForm(string text, Settings settings) : this()
        {
            // Ширина и высота экрана, на котором будем отображать наше информационное сообщение
            _screenWidth  = Screen.AllScreens.First().Bounds.Width;
            _screenHeight = Screen.AllScreens.First().Bounds.Height;

            // Подписываемся на события отрисовки и изменения информационного окна с сообщением
            this.Paint  += MessageWindowForm_Paint;
            this.Resize += MessageWindowForm_Resize;

            // Устанавливаем текст сообщения
            _textMessage = text;
            
            // Устанавливаем параметры сообщения: шрифт, размер, стиль 
            if (settings != null)
            {
                var fontFamily = settings.FontFamilyText;           // шрифт
                var size       = (float)settings.SizeText;          // размер

                FontStyle style;                                    // стиль:
                switch(settings.TextStyle)
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

                _fontMessage = new Font(fontFamily, size, style);

                _foreColorText = settings.ForeColorText;
            }

            // Елси текстового сообщения не задано, но задаем параметры для отображения картинки
            if (string.IsNullOrEmpty(_textMessage))
                this.StartPosition = FormStartPosition.CenterScreen;
        }

        #endregion

        #region Обработчики событий формы или окна

        /// <summary>
        /// Обработчки события Изменение размеров формы или окна
        /// </summary>
        /// <param name="sender"> Объект, который сообщил о произошедшем событии </param>
        /// <param name="e"></param>
        private void MessageWindowForm_Resize(object sender, EventArgs e)
        {
            // Обновить размеры (перерисовать форму или окно)
            // this.Refresh();
        }

        /// <summary>
        /// Обработчик события Отрисовка формы или окна
        /// </summary>
        /// <param name="sender"> Объект, который сообщил о произошедшем событии </param>
        /// <param name="e"></param>
        private void MessageWindowForm_Paint(object sender, PaintEventArgs e)
        {
            //  Если сообщения нет, то заканчиваем отрисовку (если условие TRUE, то рисовалась картинка)
            //if (string.IsNullOrEmpty(_textMessage))
            //    return;

            // Отрисовать текст в форме или окне
            //DrawTextInForm(e);
        }

        #endregion

        #region Дополнительные методы

        /// <summary>
        /// Метод получения корректных ширины и высоты прямоугольника, в котором будет отображен текст
        /// </summary>
        /// <param name="initialWidth"> Начальная ширина прямоугольника </param>
        /// <param name="initialHeight"> Начальная высота прямоугольника </param>
        /// <param name="textByRows"> Текст, который нужно правильно разбить по строкам </param>
        /// <param name="e"> Параметры рисования формы </param>
        private void GetCorrectWidthAndHeightMessageWindow(ref int initialWidth, ref int initialHeight, out string textByRows, PaintEventArgs e)
        {
            // Начальное значение для вхождения в цикл
            _countWordsInFirstRowInfoMessage = _startCountWordsInFirstRow + 1;

            do
            {
                _countWordsInFirstRowInfoMessage--;

                // Получаем текст разбитый по строкам относительно количество слов в первой строке
                textByRows = GetMessageByRows(_textMessage, _countWordsInFirstRowInfoMessage);

                // Определить размер области, которую займет текст, при выводе его c заданном шрифтом
                initialWidth  = (int)e.Graphics.MeasureString(textByRows, _fontMessage).Width  + 1;
                initialHeight = (int)e.Graphics.MeasureString(textByRows, _fontMessage).Height + 1;

            } while (!ValidatorMessageWindow(initialWidth, initialHeight) && _countWordsInFirstRowInfoMessage != 0);
        }

        /// <summary>
        /// Метод, который валидирует ширину и высоту информационного сообщения
        /// </summary>
        /// <param name="width"> Ширина прямоугольника с сообщением </param>
        /// <param name="height"> Высота прямоугольника с сообщением </param>
        /// <returns></returns>
        private bool ValidatorMessageWindow(int width, int height)
        {
            if (width <= _screenWidth && height <= _screenHeight) // если прямоугольник вписывается в размеры экрана
                return true;

            return false;
        }

        /// <summary>
        /// Метод, который рисует прямоугольник с сообщением
        /// </summary>
        /// <param name="e"> Параметры отрисовки формы или окна </param>
        private void DrawTextInForm(PaintEventArgs e)
        {
            /* ClientSize.Width - ширина внутренней области окна */

            // Координаты формы или окна
            int xPointF = 0, yPointF = 0;

            // Ширина и высота области-прямоугольника занимаемой текстом
            int w = 0, h = 0;

            // Начальное значение текста сообщения разбитого по строкам
            var textByRows = string.Empty;

            // Производим выставление корректных ширины и высоты
            GetCorrectWidthAndHeightMessageWindow(ref w, ref h, out textByRows, e);

            // TODO: вынести проверку на сендер
            if (!ValidatorMessageWindow(w, h))
            {
                //MessageBox.Show("Сообщение не может поместиться на экран с указанными параметрами!");
            }

            // Рисуем строку на форме или окне
            var brush = new SolidBrush(Color.FromName(_foreColorText));
            e.Graphics.DrawString(textByRows, _fontMessage, brush, 0, 0);

            // Устанавливаем размеры формы или окна с сообщением (изменение размеров вызывает событие отрисовки)
            this.Width  = w ;
            this.Height = h ;

            // Вычислить координату x и y так, чтобы текст был размещен в центре окна
            xPointF = (int)(_screenWidth  - w)/2 + 1;
            yPointF = (int)(_screenHeight - h)/2 + 1;

            // Устанавливаем позицию формы или окна
            this.Location = new Point(xPointF, yPointF);
        }

        /// <summary>
        /// Удалить двойные, тройные и т.д. пробелы
        /// </summary>
        /// <param name="message"> Сообщение, которое требуется отформатировать </param>
        /// <returns> Отформатированная строка </returns>
        private string DeletePluralWhitespace(string message)
        {
            var regex = new Regex(@"\s+");      // шаблон форматирования, написанный на языке регулярный выражений

            return regex.Replace(message, " "); // заменяем все те части текста на пробел, которые совпали с шаблоном
        }
  
        /// <summary>
        /// Метод, который разбивает указанный текст по строкам, ориентируясь на количество слов в первой строке
        /// </summary>
        /// <param name="message"> Сообщение, которое требуется отформатировать </param>
        /// <param name="countWordsInFirstRow"> Количество слов, которое должно быть в первой сторке </param>
        /// <returns></returns>
        private string GetMessageByRows(string message, int countWordsInFirstRow)
        {
            _numberRows   = 0;              // начальное ожидаемое количество строк в тексте
            var width     = 0;              // ширина первой строки в символах
            var rowInText = string.Empty;   // начальное значение строки в тексте
            var count     = 0;              // счетчик перебираемых слов

            // Построитель текста
            var newText = new StringBuilder(rowInText);

            // Находим все слова в тексте
            var wordsInMessage = DeletePluralWhitespace(message).Split(new char[ ] { ' ' });

            // Перебираем слова
            foreach(var word in wordsInMessage)
            {
                // Инкрементриуем по выбранному слову
                count++;

                // Если ширина первой строки еще не задана, то
                if (width == 0)
                {
                    // смотрим, равен ли счетчик установленному нормативу по количеству слов в первой строке,
                    // и если да, то
                    if (count == countWordsInFirstRow)
                    {
                        newText.Append(word + "\n\r");    // добавляем к уже созданной строке слово и перенос на другую строку и перенос курсора
                        width = newText.Length;           // устанавливаем ширину первой строки
                        _numberRows++;                    // инкрементируем счетчик по каждой новой строке
                    }
                    // если нет, то продолжаем строить первую строку, и
                    else
                    {
                        newText.Append(word + " ");       // добавляем к уже созданной строке слово и пробел
                    }
                }
                // иначе, если ширина первой строки уже установлена, то
                else if (width != 0)
                {
                    // Поменять

                    // смотрим, если конструируемая строка + новое слово имеет длину, которая преввышает длину первой строки, то
                    if ((rowInText + word).Length > width)
                    {
                        // добавляем к новой строке символы переноса на другую строку и переноса курсора
                        rowInText = rowInText.Trim() + "\n\r";  
                        // добавляем построенную строку к нашему тексту с переносом по строкам
                        newText.Append(rowInText);
                        // говорим, что построили еще одну строку
                        _numberRows++;
                        // сбрасываем значение к начальному, чтобы начать строить следующую строку, если требуется
                        rowInText = string.Empty;
                    }

                    // иначе, если длина первой строки еще не превышена, то добавляем следующее слово и пробел
                    rowInText += word + " ";
                }
            }

            // Если слова закончились и мы так и не превысили длину первой строки или норму в десять слов, то добаляем тот кусочек, который успели построить
            newText.Append(rowInText.Trim());
            // Сообщаем, что добавили еще одну строку, хоть и не полную
            _numberRows++;

            // Возвращаем результат в виде исходного текста разбитого по строкам
            return newText.ToString();
        }

        #endregion
    }
}