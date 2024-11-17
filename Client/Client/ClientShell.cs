using System;
using System.IO;
using System.Net.Sockets;

namespace Client
{
    /// <summary>
    /// Класс для интерпретации команд, вводимых
    /// пользователем.
    /// </summary>
    public static class ClientShell
    {
        /// <summary>
        /// Приватный объект сетевого потока для передачи данных на сервер
        /// </summary>
        private static NetworkStream networkStream;

        /// <summary>
        /// Выполнение команды в терминале клиента
        /// </summary>
        /// <param name="cmd">Команда в виде строки</param>
        public static void Execute(string cmd)
        {
            try
            {
                // Выполняем действия, в зависимости от введённой команды
                switch (cmd.ToLower())
                {
                    #region Пропуск команды

                    case "":
                        break;

                    #endregion

                    #region Краткая справка

                    case "help":
                        Helpers.Light("help");
                        Console.WriteLine("\t\tвызов справки;");

                        Helpers.Light("version");
                        Console.WriteLine("\t\tзапрос версии сервера;");

                        Helpers.Light("send");
                        Console.WriteLine("\t\tотправка файла на сервер;");

                        Helpers.Light("editlog");
                        Console.WriteLine("\t\tредактирование журнала на сервере;");

                        Helpers.Light("clear");
                        Console.WriteLine("\t\tочистить консольное окно;");

                        Helpers.Light("exit");
                        Console.WriteLine("\t\tвыход.");

                        break;

                    #endregion

                    #region Запрос версии сервера

                    case "version":
                        // Создание объекта сообщения
                        ClientPackets.Version version = new ClientPackets.Version(Program.UserName);

                        // Сериализация сообщения
                        byte[] data = version.Serialize().ToArray();

                        // Отправка сообщения
                        networkStream = Program.tcpClient.GetStream();
                        networkStream.Write(data, 0, data.Length);

                        // Переводим программу в режим ожидания
                        Program.clientState = ClientState.Waiting;

                        break;

                    #endregion

                    #region Запрос редактирования журнала

                    case "editlog":
                        // Создание объекта сообщения
                        ClientPackets.LogRequest log = new ClientPackets.LogRequest(Program.UserName);

                        // Сериализация сообщения
                        data = log.Serialize().ToArray();

                        // Отправка сообщения
                        networkStream = Program.tcpClient.GetStream();
                        networkStream.Write(data, 0, data.Length);

                        // Переходим в режим ожидания
                        Program.clientState = ClientState.Waiting;

                        break;

                    #endregion

                    #region Запрос отключения и выхода

                    case "exit":
                        // Создание объекта сообщения
                        ClientPackets.EndConnection end = new ClientPackets.EndConnection(Program.UserName);

                        // Сериализация сообщения
                        data = end.Serialize().ToArray();

                        // Отправка сообщения
                        networkStream = Program.tcpClient.GetStream();
                        networkStream.Write(data, 0, data.Length);

                        // Выход из программы
                        Environment.Exit(0);

                        break;

                    #endregion

                    #region Очистка экрана терминала

                    case "clear":
                        Console.Clear();

                        break;

                    #endregion

                    #region Команды более сложные

                    default:

                        #region Команда отправки файла

                        if (cmd.Split(' ')[0] == "send")
                        {
                            // Разделяем команду по пробелам
                            string[] argv = cmd.Split(' ');

                            // Количество аргументов "командной строки"
                            int argc = argv.Length;

                            #region Если команда корректна

                            if (argv.Length == 3 && (argv[argc - 1] == "1" || argv[argc - 1] == "2"))
                            {
                                // Устанавливаем ждущий режим программы
                                Program.clientState = ClientState.Waiting;

                                // Проверяем наличие файла
                                if (!File.Exists(argv[1]))
                                {
                                    Helpers.PrintError("Файл не найден.");

                                    // Возврат в исходное состояние
                                    Program.clientState = ClientState.Ready;
                                    return;
                                }

                                try
                                {
                                    // Чтение файла
                                    byte[] file_data = File.ReadAllBytes(argv[1]);

                                    // Создание объекта сообщения
                                    ClientPackets.SendFile sendFile = new ClientPackets.SendFile(Program.UserName,
                                        argv[1],
                                        file_data.Length,
                                        file_data,
                                        Byte.Parse(argv[2]));

                                    // Сериализация сообщения
                                    byte[] bin_file_request = sendFile.Serialize().ToArray();

                                    // Отправка сообщения
                                    networkStream = Program.tcpClient.GetStream();
                                    networkStream.Write(bin_file_request, 0, bin_file_request.Length);
                                }

                                catch (IOException) // Если файл открыт в другой программе
                                {
                                    Helpers.PrintError("Нет доступа к файлу. Возможно, он открыт в другой программе.");

                                    // Возвращаем программу в исходное состояние
                                    Program.clientState = ClientState.Ready;
                                    return;
                                }
                            }

                            #endregion

                            #region Если команда введена неверно, возвращаем краткую справку по ней

                            else
                            {
                                Console.WriteLine("usage: send <filename> <version>");

                                Helpers.Light("<filename>");
                                Console.WriteLine("\tПуть к файлу");

                                Helpers.Light("<version>");
                                Console.WriteLine("\tВерсия сервера");

                                Console.WriteLine("Обратите внимание, путь к файлу не должен содержать пробелов!");
                            }

                            #endregion
                        }

                        #endregion

                        #region Некорректная/неизвестная команда

                        else
                        {
                            Helpers.PrintError("Команда не распознана!");
                        }

                        #endregion

                        break;

                    #endregion
                }
            }

            catch (Exception ex)
            {
                // Обработка неотслеживаемых в программе исключений
                Helpers.PrintError(string.Concat("Необработанное исключение: ", ex.Message));
            }

            finally
            {
                // Очистка мусора
                GC.Collect();
            }
        }
    }
}
