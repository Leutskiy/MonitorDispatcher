using System;
using System.Collections.Generic;
using System.Drawing;

namespace HelpfulMethods
{
    /// <summary>
    /// Помогает настроить оформление UI
    /// </summary>
    public static class FormalizationManager
    {
        /// <summary>
        /// Получает словарь цветов: по строковому наименованию цвета получаем цвет Color
        /// </summary>
        /// <returns>Универсальный словарь типа <see cref=\"Dictionary<string, KnownColor>\"/></returns>
        public static Dictionary<string, KnownColor> GetDictionaryColors()
        {
            // получаем известные цвета из перечисления
            var colors = Enum.GetValues(typeof(KnownColor));
            // создаем словарь с заданным объекмом, чтобы сократить издержки на изменение размера во время выполнения
            var dictColors = new Dictionary<string, KnownColor>(colors.Length);

            foreach (var knowColor in colors)
            {
                dictColors[knowColor.ToString()] = (KnownColor)knowColor;
            }

            return dictColors;
        }
    }
}