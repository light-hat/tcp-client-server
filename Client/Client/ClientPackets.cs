using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    /// <summary>
    /// Класс, описывающий сообщения, которые отправляет клиент
    /// </summary>
    public class ClientPackets
    {
        #region Вспомогательная часть

        /// <summary>
        /// Разделитель в передаваемых и принимаемых сообщениях.
        /// </summary>
        public static byte[] Delimeter = Encoding.UTF8.GetBytes("<EOM>");

        /// <summary>
        /// Обозначает конец передаваемого сообщения.
        /// </summary>
        public static byte[] End = Encoding.UTF8.GetBytes("<EOF>");

        /// <summary>
        /// Интерфейс, описывающий обязательные для пакета методы
        /// </summary>
        public interface IClientPackets
        {
            /// <summary>
            /// Переводит отправляемое сообщение из структуры в двоичный вид (сериализация)
            /// </summary>
            /// <returns>Список байт для передачи по сети</returns>
            List<byte> Serialize();
        }

        #endregion

        #region Сообщение 0 (авторизация)

        /// <summary>
        /// Класс, описывающий пакет авторизации
        /// </summary>
        public class Auth : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static readonly byte MessageId = 0;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Поле класса, хранящее хеш от пароля пользователя
            /// </summary>
            private static string PasswordHash;

            /// <summary>
            /// Конструктор класса сообщения (запрос на авторизацию).
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="passwordHash">Хеш от пароля клиента</param>
            public Auth(string clientId, string passwordHash)
            {
                ClientID = clientId;
                PasswordHash = passwordHash;
            }

            public List<byte> Serialize()
            {
                // Создаем возвращаемый список байт
                List<byte> packet = new List<byte>();

                // Переводим в байты и добавляем в список ID клиента
                packet.AddRange(Encoding.UTF8.GetBytes(ClientID));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Добавляем в конец списка один байт, отвечающий за ID сообщения
                packet.Add(MessageId);

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Переводим в байты и добавляем в конец списка хеш от пароля
                packet.AddRange(Encoding.UTF8.GetBytes(PasswordHash));

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Сообщение 1 (отправка файла)

        /// <summary>
        /// Класс, описывающий сообщение для передачи файла пользователем
        /// </summary>
        public class SendFile : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static readonly byte MessageID = 1;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Поле класса, хранящее имя файла
            /// </summary>
            private static string FileName;

            /// <summary>
            /// Поле класса, хранящее размер файла
            /// </summary>
            private static int FileSize;

            /// <summary>
            /// Поле класса, хранящее содержимое файла
            /// </summary>
            private static byte[] FileData;

            /// <summary>
            /// Поле, хранящее версию сервера, для которого
            /// передается файл.
            /// </summary>
            private static byte ServerVersion;

            /// <summary>
            /// Конструктор класса сообщения передачи файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="fileName">Имя файла</param>
            /// <param name="fileSize">Размер файла</param>
            /// <param name="fileData">Содержимое файла</param>
            /// <param name="serverVersion">Версия сервера, для которого отправляется файл</param>
            public SendFile(string clientId,
                string fileName,
                int fileSize,
                byte[] fileData,
                byte serverVersion)
            {
                ClientID = clientId;
                FileName = fileName;
                FileSize = fileSize;
                FileData = fileData;
                ServerVersion = serverVersion;
            }

            public List<byte> Serialize()
            {
                // Создаем возвращаемый список байт
                List<byte> packet = new List<byte>();

                // Переводим в байты и добавляем в список ID клиента
                packet.AddRange(Encoding.UTF8.GetBytes(ClientID));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Добавляем в конец списка один байт, отвечающий за ID сообщения
                packet.Add(MessageID);

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Переводим в байты и добавляем в конец списка имя файла
                packet.AddRange(Encoding.UTF8.GetBytes(FileName));

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(Delimeter);

                // Переводим в байты и добавляем в конец списка размер файла
                packet.AddRange(BitConverter.GetBytes(FileSize));

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(Delimeter);

                // Переводим в байты и добавляем в конец списка содержимое файла
                packet.AddRange(FileData);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(Delimeter);

                // Добавляем версию сервера, для которого отправляется файл
                packet.Add(ServerVersion);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Сообщение 2 (запрос лог-файла)

        /// <summary>
        /// Класс, описывающий сообщение запроса на редактирование лог-файла
        /// </summary>
        public class LogRequest : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static readonly byte MessageID = 2;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            public LogRequest(string clientId)
            {
                ClientID = clientId;
            }

            public List<byte> Serialize()
            {
                // Создаем возвращаемый список байт
                List<byte> packet = new List<byte>();

                // Переводим в байты и добавляем в список ID клиента
                packet.AddRange(Encoding.UTF8.GetBytes(ClientID));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Добавляем в конец списка один байт, отвечающий за ID сообщения
                packet.Add(MessageID);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Сообщение 3 (отправка изменений лога)

        /// <summary>
        /// Класс, описывающий сообщение с изменениями для лог-файла
        /// </summary>
        public class LogChanges : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static readonly byte MessageID = 3;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Поле класса, хранящее новое содержимое лог-файла
            /// </summary>
            private static byte[] LogData;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="logData">Новые данные лог-файла</param>
            public LogChanges(string clientId, byte[] logData)
            {
                ClientID = clientId;
                LogData = logData;
            }

            public List<byte> Serialize()
            {
                // Создаем возвращаемый список байт
                List<byte> packet = new List<byte>();

                // Переводим в байты и добавляем в список ID клиента
                packet.AddRange(Encoding.UTF8.GetBytes(ClientID));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Добавляем в конец списка один байт, отвечающий за ID сообщения
                packet.Add(MessageID);

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Переводим в байты и добавляем в конец списка данные лога
                packet.AddRange(LogData);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Сообщение 4 (запрос версии сервера)

        /// <summary>
        /// Класс, описывающий запрос версии сервера
        /// </summary>
        public class Version : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static readonly byte MessageID = 4;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            public Version(string clientId)
            {
                ClientID = clientId;
            }

            public List<byte> Serialize()
            {
                // Создаем возвращаемый список байт
                List<byte> packet = new List<byte>();

                // Переводим в байты и добавляем в список ID клиента
                packet.AddRange(Encoding.UTF8.GetBytes(ClientID));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Добавляем в конец списка один байт, отвечающий за ID сообщения
                packet.Add(MessageID);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Сообщение 5 (завершение соединения)

        /// <summary>
        /// Класс, описывающий сообщение клиента об отключении от сервера
        /// </summary>
        public class EndConnection : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static readonly byte MessageID = 5;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            public EndConnection(string clientId)
            {
                ClientID = clientId;
            }

            public List<byte> Serialize()
            {
                // Создаем возвращаемый список байт
                List<byte> packet = new List<byte>();

                // Переводим в байты и добавляем в список ID клиента
                packet.AddRange(Encoding.UTF8.GetBytes(ClientID));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Добавляем в конец списка один байт, отвечающий за ID сообщения
                packet.Add(MessageID);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion
    }
}
