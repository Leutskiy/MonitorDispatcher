using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelpfulMethods
{
    public static class TextManager
    {
        /// <summary>
        /// Ширина экрана, на котором будет показано сообщение
        /// </summary>
        private static readonly int _screenWidth;

        /// <summary>
        /// Высота экрана, на котором будет показано  сообщение
        /// </summary>
        private static readonly int _screenHeight;

        private static readonly int _deltaHeight;

        /// <summary>
        /// Ожидаемое количество строк в информационном окне с сообщением
        /// </summary>
        private static int          _numberRows;
            
        /// <summary>
        /// Начальное количество слов в первой строке информационного окна с сообщением
        /// </summary>
        private const int _startCountWordsInFirstRow = 10;

        static TextManager()
        {
            // Ширина и высота экрана, на котором будем отображать наше информационное сообщение
            _screenWidth  = Screen.AllScreens.First().Bounds.Width;
            _screenHeight = Screen.AllScreens.First().Bounds.Height;

            _deltaHeight = 200;
        }

        public static bool ValidateBoundsScreen(Form form, int widthCheck, int heightCheck)
        {
            var timerName = @"simpleTimerReciver";

            var timer = form.Controls.Find(timerName, true).FirstOrDefault();

            var widthTimer  = _screenWidth;
            var heightTimer = _screenHeight;

            if (timer != null)
            {
                widthTimer  = timer.Width;
                heightTimer = timer.Height;
            }

            if (widthCheck < _screenWidth && heightCheck < _screenHeight - heightTimer)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Метод получения корректных ширины и высоты прямоугольника, в котором будет отображен текст
        /// </summary>
        /// <param name="initialWidth"> Начальная ширина прямоугольника </param>
        /// <param name="initialHeight"> Начальная высота прямоугольника </param>
        /// <param name="textByRows"> Текст, который нужно правильно разбить по строкам </param>
        /// <param name="e"> Параметры рисования формы </param>
        public static void GetCorrectWidthAndHeightMessageWindow(string _textMessage, Font _fontMessage, ref int initialWidth, ref int initialHeight, out string textByRows, PaintEventArgs e)
        {
            // Начальное значение для вхождения в цикл
            var _countWordsInFirstRowInfoMessage = _startCountWordsInFirstRow + 1;

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
        private static bool ValidatorMessageWindow(int width, int height)
        {
            if (width <= _screenWidth && height <= _screenHeight - _deltaHeight) // если прямоугольник вписывается в размеры экрана
                return true;

            return false;
        }

        /// <summary>
        /// Метод, который рисует прямоугольник с сообщением
        /// </summary>
        /// <param name="e"> Параметры отрисовки формы или окна </param>
        public static void DrawTextInForm(string _textMessage, Font _fontMessage, Brush _brushDrawText, PaintEventArgs e)
        {
            /* ClientSize.Width - ширина внутренней области окна */

            // Координаты формы или окна
            float xPointF = 0, yPointF = 0;

            // Ширина и высота области-прямоугольника занимаемой текстом
            int w = 0, h = 0;

            // Начальное значение текста сообщения разбитого по строкам
            var textByRows = string.Empty;

            // Производим выставление корректных ширины и высоты
            GetCorrectWidthAndHeightMessageWindow(_textMessage, _fontMessage, ref w, ref h, out textByRows, e);

            // TODO: вынести проверку на сендер
            if (!ValidatorMessageWindow(w, h))
            {
                return;
                //MessageBox.Show("Сообщение не может поместиться на экран с указанными параметрами!");
            }

            // Вычислить координату x и y так, чтобы текст был размещен в центре окна
            xPointF = (_screenWidth  - w) / 2 + 1;
            yPointF = (_screenHeight - h) / 2 + 1;

            // Рисуем строку на форме или окне
            e.Graphics.DrawString(textByRows, _fontMessage, _brushDrawText, xPointF, yPointF);
        }

        /// <summary>
        /// Удалить двойные, тройные и т.д. пробелы
        /// </summary>
        /// <param name="message"> Сообщение, которое требуется отформатировать </param>
        /// <returns> Отформатированная строка </returns>
        private static string DeletePluralWhitespace(string message)
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
        private static string GetMessageByRows(string message, int countWordsInFirstRow)
        {
            _numberRows = 0;                // начальное ожидаемое количество строк в тексте
            var width     = 0;              // ширина первой строки в символах
            var rowInText = string.Empty;   // начальное значение строки в тексте
            var count     = 0;              // счетчик перебираемых слов

            // Построитель текста
            var newText = new StringBuilder(rowInText);

            // Находим все слова в тексте
            var wordsInMessage = DeletePluralWhitespace(message).Split(new char[ ] { ' ' });

            // Перебираем слова
            foreach (var word in wordsInMessage)
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
    }
}
