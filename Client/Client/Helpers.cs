using System;
using System.Threading;

namespace Client
{
    /// <summary>
    /// Класс, где реализованы вспомогательные методы, используемые программой.
    /// </summary>
    public static class Helpers
    {
        #region Вывод сообщений на экран

        /// <summary>
        /// Выводит на экран сообщение с ошибкой
        /// </summary>
        /// <param name="message">Текст сообщения об ошибке</param>
        public static void PrintError(string message)
        {
            // Устанавливаем красный цвет текста в консоли
            Console.ForegroundColor = ConsoleColor.Red;

            // Выводим сообщение
            Console.WriteLine(string.Concat("[-] ", message));

            // Возвращаем прежний цвет текста
            Console.ResetColor();
        }

        /// <summary>
        /// Выводит сообщение об успехе на экран
        /// </summary>
        /// <param name="message">Текст выводимого сообщения</param>
        public static void PrintSuccess(string message)
        {
            // Устанавливаем зелёный цвет текста в консоли
            Console.ForegroundColor = ConsoleColor.Green;

            // Выводим сообщение
            Console.WriteLine(string.Concat("[+] ", message));

            // Возвращаем прежний цвет текста
            Console.ResetColor();
        }

        #endregion

        #region Вспомогательные методы для работы с консолью

        /// <summary>
        /// Метод для выделения текста в консоли
        /// другим цветом
        /// </summary>
        /// <param name="message">Выделяемый текст</param>
        /// <param name="color">Цвет (по умолчанию Cyan)</param>
        public static void Light(string message, ConsoleColor color = ConsoleColor.Cyan)
        {
            // Ставим заданный цвет
            Console.ForegroundColor = color;

            // Выводим сообщение
            Console.Write(message);

            // Возвращаем цвет
            Console.ResetColor();
        }

        /// <summary>
        /// Очищает строку, где выводится загрузка или её результат.
        /// Примитивная, но рабочая реализация.
        /// </summary>
        public static void ClearString()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("                                                         ");
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        #endregion

        #region Анимированное сообщение о загрузке

        /// <summary>
        /// Создаёт эффект загрузки в консоли
        /// </summary>
        /// <param name="pause">Время задержки между сменой кадра, влияет на скорость анимации</param>
        public static void ShowLoadingAnimation(string message, int pause)
        {
            while (true)
            {
                // Проверяем состояние программы
                if (Program.clientState == ClientState.Waiting)
                {
                    // Выводим очередной символ в анимации и через пробел сообщение
                    Console.Write(String.Concat('|', ' ', message));

                    // Задерживаем выполение потока на заданное количество миллисекунд (делаем паузу)
                    Thread.Sleep(pause);

                    // Устанавливаем позицию в буфере консоли туда, где мы вывели символ (чтобы следующий символ перетёр текущий)
                    Console.SetCursorPosition(0, Console.CursorTop);

                    Console.Write(String.Concat('/', ' ', message));
                    Thread.Sleep(pause);
                    Console.SetCursorPosition(0, Console.CursorTop);

                    Console.Write(String.Concat('-', ' ', message));
                    Thread.Sleep(pause);
                    Console.SetCursorPosition(0, Console.CursorTop);

                    Console.Write(String.Concat('\\', ' ', message));
                    Thread.Sleep(pause);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                else
                    break;
            }
        }

        #endregion
    }
}
