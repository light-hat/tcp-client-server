using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Класс, описывающий объект подключенного к серверу клиента
    /// </summary>
    public class ClientObject
    {
        #region Открытые поля класса

        /// <summary>
        /// Клиентский сокет для приёма данных от клиента
        /// </summary>
        public TcpClient Client;

        /// <summary>
        /// IP-адрес клиента
        /// </summary>
        public string IpAddress;

        /// <summary>
        /// Идентификатор клиента
        /// </summary>
        public string ClientId;

        /// <summary>
        /// Права пользователя
        /// </summary>
        public ClientRole ClientRole;

        #endregion

        #region Закрытые поля класса

        /// <summary>
        /// Сетевой поток для передачи данных
        /// </summary>
        private NetworkStream NetStream;

        /// <summary>
        /// Максимальный размер буфера для отправляемых/принятых данных
        /// </summary>
        private const int BUFFER_LENGTH = 64;

        #endregion

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="tcpClient">Клиентский сокет</param>
        public ClientObject(TcpClient tcpClient)
        {
            this.Client = tcpClient;
            this.IpAddress = tcpClient.Client.RemoteEndPoint.ToString();
        }

        #region Публичные методы

        /// <summary>
        /// Метод класса для обработки входящего подключения.
        /// </summary>
        public void Process()
        {
            // Сетевой поток для принятия данных
            NetworkStream stream = null;

            try
            {
                // Получаем объект потока для входящего соединения
                stream = Client.GetStream();

                // Промежуточный буфер для сетевого потока и потока памяти
                byte[] data = new byte[BUFFER_LENGTH];

                // Структура сообщения от клиента
                ClientPackets.IClientPackets received_packet;

                while (true)
                {
                    // Создаём поток памяти, куда будем сохранять принятые данные
                    MemoryStream ms = new MemoryStream();
                    ms.Position = 0;

                    // Количество принятых данных в байтах
                    int bytes = 0;

                    do
                    {
                        try
                        {
                            // Читаем данные из сетевого потока
                            bytes = stream.Read(data, 0, data.Length);
                        }

                        // Если сетевой поток закрыт, то завершаем функцию
                        catch (ObjectDisposedException) { return; }

                        // И записываем их в поток памяти
                        ms.Write(data, 0, bytes);
                    }

                    while (stream.DataAvailable);

                    // Если в потоке есть данные
                    if (ms.ToArray().Length > 0)
                    {
                        try
                        {
                            // Десериализуем принятые двоичные данные в структуру сообщения
                            received_packet = ClientPackets.Deserialize(ms.ToArray());
                        }

                        catch (Exception e)
                        {
                            // Вывод сообщения об ошибке
                            Helpers.PrintError(string.Concat("Ошибка десериализации: ",
                                e.Message));

                            // Отправляем клиенту ошибку с кодом 400 (bad request)
                            this.SendError("?", 0);

                            return;
                        }

                        // Небольшая задержка, чтобы у пользователя отображался эффект загрузки
                        Thread.Sleep(500);

                        // Если код сообщения равен 0, то нужно авторизовать клиента
                        if (received_packet.GetMessageId() == 0)
                            AuthClient((ClientPackets.Auth)received_packet);

                        // Если нет, то обрабатываем сообщения
                        else
                        {
                            // В обычном состоянии - просто обрабатываем пакеты
                            if (Program.State == ServerState.Normal)
                                PacketHandler.HandlePacket(this, received_packet);

                            // Если мы ждем изменения лога - пропускаем пакеты только с ID равным 3
                            else if (Program.State == ServerState.RecieveLog &&
                                received_packet.GetMessageId() == 3)
                                PacketHandler.HandlePacket(this, received_packet);

                            // Если сообщение - не изменения лога
                            else
                            {
                                // Проверяем, не стало ли состояние сервера "нормальным"
                                while (Program.State != ServerState.Normal)
                                {
                                    // Делаем паузу в 1 секунду
                                    Thread.Sleep(1000);
                                }

                                // При выходе из цикла обрабатываем сообщение
                                PacketHandler.HandlePacket(this, received_packet);
                            }
                        }

                    }
                }
            }

            catch (IOException) // Обработка аварийного отключения
            {
                Helpers.PrintWarning(string.Concat("Клиент ", ClientId, " аварийно отключился!"));
            }

            finally
            {
                // Закрываем сетевой поток
                if (stream != null)
                    stream.Close();

                // Закрываем клиентский сокет
                if (Client != null)
                    Client.Close();

                // Очищаем мусор
                GC.Collect();
            }
        }

        #region Отправка сообщений клиенту

        /// <summary>
        /// Отправляет клиенту успешный ответ, подтверждение
        /// </summary>
        /// <param name="clientId">Строка-идентификатор клиента</param>
        /// <param name="messageId">Строка-идентификатор сообщения</param>
        public void SendSuccessResponse(string clientId, byte messageId)
        {
            // Создаем и формируем сообщение ответа сервера
            ServerPackets.Response success_response = new ServerPackets.Response(clientId, messageId, 200);
            byte[] data = success_response.Serialize().ToArray();

            // Отправка сообщения
            NetStream = Client.GetStream();
            NetStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Отправляет клиенту ошибку
        /// </summary>
        /// <param name="clientId">Строка-идентификатор клиента</param>
        /// <param name="messageId">Строка-идентификатор сообщения</param>
        /// <param name="errorCode">Код ошибки (по умолчанию - 400, bad request, т.е. не распознан запрос)</param>
        public void SendError(string clientId, byte messageId, short errorCode = 400)
        {
            // Создаем и формируем сообщение ответа сервера
            ServerPackets.Response error_response = new ServerPackets.Response(clientId, messageId, errorCode);
            byte[] data = error_response.Serialize().ToArray();

            // Отправка сообщения
            NetStream = Client.GetStream();
            NetStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Отправляет клиенту версию сервера
        /// </summary>
        /// <param name="clientId">Строка-идентификатор клиента</param>
        public void SendVersion(string clientId)
        {
            // Создаем и формируем сообщение ответа сервера
            ServerPackets.Version version = new ServerPackets.Version(clientId, 4, Program.ServerVersion);
            byte[] data = version.Serialize().ToArray();

            // Отправка сообщения
            NetStream = Client.GetStream();
            NetStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Отправляет клиенту данные файла журнала
        /// </summary>
        /// <param name="clientId">Идентификатор клиента</param>
        /// <param name="logData">Данные лог-файла в двоичном виде</param>
        public void SendLog(string clientId, byte[] logData)
        {
            // Создаем и формируем сообщение ответа сервера
            ServerPackets.LogData log = new ServerPackets.LogData(clientId, 2, 200, logData);
            byte[] data = log.Serialize().ToArray();

            // Отправка сообщения
            NetStream = Client.GetStream();
            NetStream.Write(data, 0, data.Length);
        }

        #endregion

        #endregion

        #region Авторизация клиента

        /// <summary>
        /// Метод, авторизующий клиента
        /// </summary>
        /// <param name="auth_packet">Сообщение от клиента</param>
        private void AuthClient(ClientPackets.Auth auth_packet)
        {
            // Записываем ID клиента
            this.ClientId = auth_packet.GetClientId();

            // Получаем данные клиента из базы
            Credential cred = XmlUserDB.GetUser(this.ClientId);

            // Не достигнуто ли максимальное количество подключений?
            if (Program.Clients.Count < Program.MaxConnections)
            {
                // Если клиент найден в базе
                if (cred != null)
                {
                    // Устанавливаем права доступа для пользователя
                    this.ClientRole = cred.Role;

                    // Сравниваем хеши
                    if (cred.PasswordHash == auth_packet.GetPasswordHash())
                    {
                        // Выводим сообщение
                        Helpers.PrintSuccess(string.Concat("Подключился клиент ",
                            auth_packet.GetClientId(),
                            " с адреса ",
                            IpAddress));

                        // Отправляем ответ, что авторизация успешна
                        SendSuccessResponse(ClientId, 0);

                        // Добавляем клиента в список подключенных
                        Program.Clients.Add(this);
                    }

                    else
                    {
                        // Выводим сообщение
                        Helpers.PrintWarning(string.Concat("Неудачная попытка авторизации от клиента ",
                            auth_packet.GetClientId(),
                            " с адреса ",
                            IpAddress));

                        // Шлём ошибку авторизации (403 - доступ запрещён)
                        SendError(ClientId, 0, 403);
                    }
                }

                else
                {
                    // Спрашиваем, надо ли зарегистрировать неизвестного пользователя
                    bool flag = Helpers.PrintDialog(string.Concat("Подключается неизвестный клиент",
                        auth_packet.GetClientId(),
                        ". Зарегистрировать его?"));

                    // Если надо регистрировать
                    if (flag)
                    {
                        // Спрашиваем, присвоить ли ему админ права
                        bool admin_flag = Helpers.PrintDialog("Установить права администратора для этого пользователя?");

                        // Регистрация пользователя
                        XmlUserDB.CreateUser(auth_packet.GetClientId(),
                            auth_packet.GetPasswordHash(),
                            admin_flag ? ClientRole.Admin : ClientRole.User);

                        // Отправляем ответ, что авторизация успешна
                        SendSuccessResponse(ClientId, 0);

                        // Добавляем клиента в список подключенных
                        Program.Clients.Add(this);

                        // Выводим сообщение об успехе
                        Helpers.PrintSuccess(string.Concat("Новый клиент ",
                            auth_packet.GetClientId(),
                            " успешно зарегистрирован"));
                    }

                    // Если отказано в регистрации
                    else
                    {
                        // Шлём ошибку авторизации (403 - доступ запрещён)
                        SendError(ClientId, 0, 403);
                    }
                }
            }

            else
            {
                // Выводим сообщение
                Helpers.PrintWarning(string.Concat("Отказано в подключении клиенту ",
                    auth_packet.GetClientId(), 
                    ", так как подключений уже ", 
                    Program.MaxConnections));

                // Шлём ошибку, что максимальное количество подключений достигнуто
                SendError(ClientId, 0, 429);
            }
        }

        #endregion
    }
}