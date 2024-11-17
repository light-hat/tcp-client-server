using System;

namespace Server
{
    /// <summary>
    /// Класс, где реализованы вспомогательные методы
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Выводит на экран простое сообщение
        /// </summary>
        /// <param name="message">Выводимое сообщение</param>
        public static void PrintMessage(string message)
        {
            Console.WriteLine(string.Concat("[*]", " [", DateTime.Now, "] ", message));
        }

        /// <summary>
        /// Выводит на экран сообщение с ошибкой
        /// </summary>
        /// <param name="message">Текст сообщения об ошибке</param>
        public static void PrintError(string message)
        {
            // Устанавливаем красный цвет текста в консоли
            Console.ForegroundColor = ConsoleColor.Red;

            // Выводим сообщение
            Console.WriteLine(string.Concat("[-]", " [", DateTime.Now, "] ", message));

            // Возвращаем прежний цвет текста
            Console.ResetColor();
        }

        /// <summary>
        /// Выводит на экран предупреждение (что-то не так делает пользователь)
        /// </summary>
        /// <param name="message">Выводимое сообщение</param>
        public static void PrintWarning(string message)
        {
            // Устанавливаем желтый цвет текста в консоли
            Console.ForegroundColor = ConsoleColor.Yellow;

            // Выводим сообщение
            Console.WriteLine(string.Concat("[!]", " [", DateTime.Now, "] ", message));

            // Возвращаем прежний цвет текста
            Console.ResetColor();
        }

        /// <summary>
        /// Выводит на экран сообщение об успехе
        /// </summary>
        /// <param name="message">Выводимое сообщение</param>
        public static void PrintSuccess(string message)
        {
            // Устанавливаем зеленый цвет текста в консоли
            Console.ForegroundColor = ConsoleColor.Green;

            // Выводим сообщение
            Console.WriteLine(string.Concat("[+]", " [", DateTime.Now, "] ", message));

            // Возвращаем прежний цвет текста
            Console.ResetColor();
        }

        /// <summary>
        /// Диалог с пользователем.
        /// True - пользователь согласился.
        /// False - пользователь отказался.
        /// </summary>
        /// <param name="message">Выводимое сообщение для диалога</param>
        /// <returns>Булево значение, ответ пользователя</returns>
        public static bool PrintDialog(string message)
        {
            // Бесконечно запрашиваем, пока пользователь не введёт корректное значение
            while (true)
            {
                // Устанавливаем красный цвет текста в консоли
                Console.ForegroundColor = ConsoleColor.Blue;

                // Выводим сообщение
                Console.Write(string.Concat("[?] ", message, " [Y/N] "));

                // Возвращаем прежний цвет текста
                Console.ResetColor();

                // Читаем ответ
                ConsoleKeyInfo answer = Console.ReadKey();

                // Нажата клавиша Y
                if (answer.KeyChar == 89)
                {
                    Console.WriteLine();

                    return true;
                }

                // Нажата клавиша N
                else if (answer.KeyChar == 78)
                {
                    Console.WriteLine();

                    return false;
                }

                // Переход на новую строчку
                Console.WriteLine();
            }
        }
    }
}
