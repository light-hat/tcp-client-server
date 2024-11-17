using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Главный класс программы
    /// </summary>
    internal class Program
    {
        #region Открытые поля главного класса

        /// <summary>
        /// Версия сервера
        /// </summary>
        public static byte ServerVersion;

        /// <summary>
        /// Максимальное количество подключенных клиентов
        /// </summary>
        public static byte MaxConnections = 3;

        /// <summary>
        /// Имя файла журнала
        /// </summary>
        public static string LogFileName = "log.txt";

        /// <summary>
        /// Состояние сервера
        /// </summary>
        public static ServerState State;

        /// <summary>
        /// Список подключённых к серверу клиентов
        /// </summary>
        public static List<ClientObject> Clients = new List<ClientObject>();

        #endregion

        #region Закрытые поля главного класса

        /// <summary>
        /// Слушающий сокет, принимающий входящие соединения
        /// </summary>
        private static TcpListener listener;

        #endregion

        /// <summary>
        /// Точка входа в программу
        /// </summary>
        /// <param name="args">Аргументы. Передаётся только один аргумент - порт, на котором слушать.</param>
        public static void Main(string[] args)
        {
            // Устанавливаем кодировку в командной строке
            Console.OutputEncoding = Encoding.UTF8;

            // Если передали не два аргумента командной строки
            if (args.Length != 2)
            {
                Console.WriteLine($"Usage: Server.exe <port> <version>");
            }

            else
            {
                #region Проверка правильности аргументов командной строки

                int port = 280; // Инициализируем переменную для порта

                try
                {
                    port = Int32.Parse(args[0]); // Пробуем перевести порт из строки в целое число типа int
                    ServerVersion = Byte.Parse(args[1]); // Пробуем перевести версию из строки в целое число типа short
                }

                catch (FormatException) // Если произошла ошибка при форматировании
                {
                    Helpers.PrintError("Неверно указан порт. Укажите корректный номер порта.");

                    // Завершение процесса с кодом ошибки
                    Environment.Exit(-1);
                }

                catch (OverflowException) // Если произошло переполнение
                {
                    Helpers.PrintError("Вы указали слишком длинное число. Максимально допустимое значение - 65535.");
                    Environment.Exit(-1);
                }

                // Проверяем, правильно ли пользователь указал версию работы сервера
                if (ServerVersion != 1 && ServerVersion != 2)
                {
                    Helpers.PrintError("Неверная версия работы сервера. Допустимы значения 1 или 2.");
                    Environment.Exit(-1);
                }

                #endregion

                #region Запуск сервера и приём соединений

                try
                {
                    // Запускаем слушатель на порту, который указал пользователь
                    listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                    listener.Start();

                    // Выводим сообщение о старте сервера (подсвечиваем другим цветом)
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("[+] Сервер запущен. Ожидание подключений...");
                    Console.WriteLine();
                    Console.ResetColor();

                    while (true)
                    {
                        // Приём входящих соединений, текущий поток блокируется до нового подключения
                        TcpClient client = listener.AcceptTcpClient();

                        // Создаём объект клиента, чтобы обработать входящее подключение
                        ClientObject clientObject = new ClientObject(client);

                        // Создаём новый поток для обработки входящего подключения
                        Thread clientThread = new Thread(new ThreadStart(clientObject.Process));

                        // Запускаем созданный поток
                        clientThread.Start();
                    }
                }

                catch (Exception ex)
                {
                    // Вывод сообщения об ошибке
                    Helpers.PrintError(ex.Message);
                }

                finally
                {
                    // Останавливаем слушающий сокет
                    if (listener != null)
                        listener.Stop();

                    // Очистка мусора
                    GC.Collect();
                }

                #endregion
            }
        }
    }
}
