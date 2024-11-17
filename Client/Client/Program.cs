using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Client
{
    /// <summary>
    /// Главный класс программы
    /// </summary>
    internal class Program
    {
        #region Открытые поля главного класса

        /// <summary>
        /// Перечисление, определяющее состояние TCP
        /// клиента в данной программе.
        /// </summary>
        public static ClientState clientState = ClientState.Disconnected;

        /// <summary>
        /// Имя пользователя, отображаемое в командной оболочке.
        /// Оно же - идентификатор клиента.
        /// </summary>
        public static string UserName;

        /// <summary>
        /// Клиентский сокет, отвечающий за соединение
        /// с сервером по протоколу TCP.
        /// </summary>
        public static TcpClient tcpClient;

        #endregion

        #region Закрытые поля главного класса

        /// <summary>
        /// Поток для отображения эффекта загрузки
        /// </summary>
        private static Thread loadThread;

        /// <summary>
        /// Максимальный размер буфера для отправляемых/принятых данных
        /// </summary>
        private const int BUFFER_LENGTH = 64;

        /// <summary>
        /// Буфер для работы с файлом журнала
        /// </summary>
        private static byte[] LogDataBuffer;

        #endregion

        /// <summary>
        /// Точка входа в программу.
        /// </summary>
        /// <param name="args">
        /// Список аргументов.
        /// Передаётся два аргумента - ip-адрес и порт.
        /// </param>
        public static void Main(string[] args)
        {
            // Устанавливаем кодировку в командной строке
            Console.OutputEncoding = Encoding.UTF8;

            // Если передали не два аргумента командной строки
            if (args.Length != 2)
            {
                Console.WriteLine($"Usage: Client.exe <ip> <port>");
            }

            else
            {
                #region Проверка правильности аргументов командной строки

                // Записываем ip-адрес в переменную
                string ip = args[0];

                // Инициализируем переменную для порта
                int port = 280;

                try
                {
                    // Пробуем перевести порт из строки в int число
                    port = Int32.Parse(args[1]);
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

                    // Завершение процесса с кодом ошибки
                    Environment.Exit(-1);
                }

                #endregion

                try
                {
                    // Инициализируем объект TCP клиента (клиентский сокет)
                    tcpClient = new TcpClient(ip, port);

                    // Создаём сетевой поток для передачи и приёма данных
                    NetworkStream stream = tcpClient.GetStream();

                    // В бесконечном цикле определяем состояние программы и выбираем действия
                    while (true)
                    {
                        // Проверяем состояние данной программы-клиента
                        switch (clientState)
                        {
                            #region Если клиент отключен

                            case ClientState.Disconnected:
                                {
                                    Console.Write("Введите идентификатор клиента: ");
                                    string client_id = Console.ReadLine().Replace(" ", "");
                                    UserName = client_id;

                                    Console.Write("Введите пароль: ");
                                    string password = Console.ReadLine();

                                    // Запрошенные у пользователя данные мы оборачиваем в
                                    // структуру сообщения, которое будем отправлять по сети
                                    ClientPackets.Auth auth = new ClientPackets.Auth(client_id, Hash.GetSha512(password));

                                    // Переводим сообщение из структуры в двоичный массив
                                    byte[] data = auth.Serialize().ToArray();

                                    // Отправка сообщения с учётными данными
                                    stream.Write(data, 0, data.Length);

                                    // Устанавливаем состояние ожидания ответа от сервера
                                    clientState = ClientState.Waiting;
                                }

                                break;

                            #endregion

                            #region Если ожидаем ответ от сервера

                            case ClientState.Waiting:
                                {
                                    // Создаём новый поток для отображения эффекта загрузки
                                    loadThread = new Thread(() =>
                                        Helpers.ShowLoadingAnimation("Загрузка...", 30));

                                    try
                                    {
                                        // Если курсор виден, отключаем его
                                        if (Console.CursorVisible)
                                            Console.CursorVisible = false;

                                        // Запускаем поток с анимацией загрузки
                                        loadThread.Start();

                                        #region Приём данных от сервера

                                        // Создаём поток памяти, куда будем сохранять принятые данные
                                        MemoryStream ms = new MemoryStream();
                                        ms.Position = 0;

                                        // Буфер для приёма данных
                                        byte[] data = new byte[BUFFER_LENGTH];

                                        // Количество принятых байт
                                        int bytes = 0;

                                        do
                                        {
                                            // Принимаем данные по сети
                                            bytes = stream.Read(data, 0, data.Length);

                                            // И записываем их в поток памяти
                                            ms.Write(data, 0, bytes);
                                        }

                                        while (stream.DataAvailable);

                                        // Останавливаем поток с анимацией загрузки
                                        loadThread.Abort();

                                        if (ms.ToArray().Length > 0)
                                        {
                                            // Десериализуем принятые данные
                                            ServerPackets.IServerPackets response = ServerPackets.Deserialize(ms.ToArray());

                                            #endregion

                                            #region Обработка кода ответа сервера

                                            #region Если пришёл ответ для авторизации

                                            if (response.GetMessageId() == 0 &&
                                                response.GetStatusCode() == 200)
                                            {
                                                // Сообщение об успехе
                                                Console.SetCursorPosition(0, Console.CursorTop);
                                                Helpers.ClearString();
                                                Helpers.PrintSuccess("Добро пожаловать на сервер!");

                                                // Выводим приветствие
                                                Console.WriteLine("Командная оболочка клиента.");
                                                Console.WriteLine("Для справки используйте команду help.");

                                                // Переходим в состояние, когда клиент
                                                // готов принимать от пользователя команды
                                                clientState = ClientState.Ready;
                                            }

                                            else if (response.GetMessageId() == 0 &&
                                                response.GetStatusCode() == 403)
                                            {
                                                // Сообщение об ошибке
                                                Console.SetCursorPosition(0, Console.CursorTop);
                                                Helpers.ClearString();
                                                Helpers.PrintError("Ошибка авториазции! Подключение провалено.");

                                                // Закрываем сокет
                                                tcpClient.Dispose();
                                                tcpClient.Close();

                                                // Завершаем программу
                                                Environment.Exit(0);
                                            }

                                            else if (response.GetMessageId() == 0 &&
                                                response.GetStatusCode() == 429)
                                            {
                                                // Сообщение об ошибке
                                                Console.SetCursorPosition(0, Console.CursorTop);
                                                Helpers.ClearString();
                                                Helpers.PrintError("Достигнуто максимальное количество подключений. Повторите попытку позже.");

                                                // Закрываем сокет
                                                tcpClient.Dispose();
                                                tcpClient.Close();

                                                // Завершаем программу
                                                Environment.Exit(0);
                                            }

                                            #endregion

                                            #region Если пришёл ответ для отправки файла

                                            else if (response.GetMessageId() == 1 && response.GetStatusCode() == 200)
                                            {
                                                Console.SetCursorPosition(0, Console.CursorTop);
                                                Helpers.ClearString();
                                                Helpers.PrintSuccess("Файл передан успешно.");

                                                clientState = ClientState.Ready;
                                            }

                                            else if (response.GetMessageId() == 1 && response.GetStatusCode() == 400)
                                            {
                                                Console.SetCursorPosition(0, Console.CursorTop);
                                                Helpers.ClearString();
                                                Helpers.PrintError("Некорректная версия сервера!");

                                                clientState = ClientState.Ready;
                                            }

                                            #endregion

                                            #region Если пришёл ответ для запроса лог-файла

                                            else if (response.GetMessageId() == 2 &&
                                                response.GetStatusCode() == 200)
                                            {
                                                Helpers.ClearString();

                                                ServerPackets.LogData packet = (ServerPackets.LogData)response;
                                                LogDataBuffer = packet.GetLogFileData();

                                                clientState = ClientState.TextEditor;
                                            }

                                            else if (response.GetMessageId() == 2 &&
                                                response.GetStatusCode() == 403)
                                            {
                                                Helpers.ClearString();
                                                Helpers.PrintError("Ошибка! У вас недостаточно прав доступа для редактирования журнала.");

                                                // Переводим программу в режим приема команд
                                                clientState = ClientState.Ready;
                                            }

                                            #endregion

                                            #region Если пришёл ответ для отправки изменений лог-файла

                                            else if (response.GetMessageId() == 3 &&
                                                response.GetStatusCode() == 200)
                                            {
                                                Helpers.ClearString();
                                                Helpers.PrintSuccess("Изменения были сохранены.");

                                                // Переводим программу в режим приема команд
                                                clientState = ClientState.Ready;
                                            }

                                            else if (response.GetMessageId() == 3 &&
                                                response.GetStatusCode() == 500)
                                            {
                                                Helpers.ClearString();
                                                Helpers.PrintError("Серверная ошибка. Данные не были сохранены.");

                                                // Переводим программу в режим приема команд
                                                clientState = ClientState.Ready;
                                            }

                                            #endregion

                                            #region Если пришёл ответ для запроса версии сервера

                                            else if (response.GetMessageId() == 4)
                                            {
                                                ServerPackets.Version version = (ServerPackets.Version)response;
                                                Helpers.ClearString();
                                                Console.WriteLine(string.Concat("Версия сервера: " + version.GetVersionCode()));

                                                // Переходим в состояние, когда клиент
                                                // готов принимать от пользователя команды
                                                clientState = ClientState.Ready;
                                            }

                                            #endregion
                                        }

                                        #endregion
                                    }

                                    #region Обработка аварийного отключения сервера
                                    
                                    catch (System.IO.IOException)
                                    {
                                        // Закрываем поток с анимацией загрузки
                                        loadThread.Abort();

                                        // Закрываем сокет
                                        tcpClient.Dispose();
                                        tcpClient.Close();

                                        // Выводим сообщение об ошибке
                                        Console.SetCursorPosition(0, Console.CursorTop);
                                        Helpers.PrintError("Сервер аварийно отключился!");
                                        
                                        // Завершаем программу
                                        Environment.Exit(0);
                                    }

                                    #endregion
                                }

                                break;

                            #endregion

                            #region Если клиент подключен к серверу

                            case ClientState.Ready:
                                {
                                    // Включаем курсор обратно, если он не включен
                                    if (!Console.CursorVisible)
                                        Console.CursorVisible = true;

                                    // Ставим синий цвет
                                    Console.ForegroundColor = ConsoleColor.Blue;

                                    // Выводим имя пользователя и приглашение для команды
                                    // Затем возвращаем цвет
                                    Console.WriteLine();
                                    Console.Write(string.Concat(UserName, "> "));
                                    Console.ResetColor();

                                    // Читаем команду и выполняем её
                                    string command = Console.ReadLine();
                                    ClientShell.Execute(command);
                                }

                                break;

                            #endregion

                            #region Если редактируем лог-файл сервера

                            case ClientState.TextEditor:
                                {
                                    // Временный файл лога
                                    string tmp_log = "log.tmp.txt";

                                    // Запись данных в него
                                    File.WriteAllText(tmp_log, Encoding.UTF8.GetString(LogDataBuffer));

                                    #region Запуск и отслеживание процесса редактирования лога

                                    // Создаем объект ProcessStartInfo и указываем временный файл лога
                                    ProcessStartInfo editLogFile = new ProcessStartInfo();
                                    editLogFile.FileName = tmp_log;

                                    // Запускаем процесс редактирования временного лог-файла в текстовом редакторе
                                    Process p = Process.Start(editLogFile);

                                    Console.WriteLine("Данные лога открыты в текстовом редакторе. Программа продолжит работу после завершения его работы.");

                                    try
                                    {
                                        // Отслеживаем завершение процесса редактирования временного лог-файла
                                        p.WaitForExit();

                                        // Записываем данные из временного файла в буфер для данных лога
                                        LogDataBuffer = Encoding.UTF8.GetBytes(File.ReadAllText(tmp_log));
                                    }

                                    catch (NullReferenceException)
                                    {
                                        Helpers.PrintError("Не удалось открыть временный файл журнала. Возможно, не выбран редактор. Повторите попытку.");
                                    }

                                    // Удаляем временный файл лога
                                    File.Delete(tmp_log);

                                    #endregion

                                    #region Отправка изменений файла журнала

                                    // Создание объекта сообщения
                                    ClientPackets.LogChanges changes = new ClientPackets.LogChanges(UserName, LogDataBuffer);

                                    // Сериализация сообщения
                                    byte[] data = changes.Serialize().ToArray();

                                    // Отправка сообщения
                                    NetworkStream ns = tcpClient.GetStream();
                                    ns.Write(data, 0, data.Length);

                                    // Переводим программу в режим ожидания
                                    clientState = ClientState.Waiting;

                                    #endregion
                                }

                                break;

                            #endregion
                        }
                    }
                }

                catch (SocketException)
                {
                    Helpers.PrintError("Не удалось подключиться к серверу.");

                    // Завершение процесса с кодом ошибки
                    Environment.Exit(-1);
                }

                finally
                {
                    // Прекращаем работу сокета
                    if (tcpClient != null)
                        tcpClient.Close();

                    // Очистка мусора
                    GC.Collect();
                }
            }
        }
    }
}
