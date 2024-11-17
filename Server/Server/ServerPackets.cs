using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    /// <summary>
    /// Класс, описывающий сообщения, которые отправляет сервер
    /// </summary>
    public class ServerPackets
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
        /// Интерфейс, описывающий обязательные для класса сообщения методы
        /// </summary>
        public interface IServerPackets
        {
            /// <summary>
            /// Переводит отправляемое сообщение из структуры в двоичный вид (сериализация)
            /// </summary>
            /// <returns>Список байт для передачи по сети</returns>
            List<byte> Serialize();
        }

        #endregion

        #region Универсальный ответ сервера

        /// <summary>
        /// Класс, описывающий пакет авторизации
        /// </summary>
        public class Response : IServerPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static byte MessageId;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Поле класса, хранящее код ответа сервера
            /// </summary>
            private static short StatusCode;

            /// <summary>
            /// Конструктор класса ответа сервера пользователю.
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения, на которое идёт ответ</param>
            /// <param name="statusCode">Код ответа сервера</param>
            public Response(string clientId, byte messageId, short statusCode)
            {
                ClientID = clientId;
                MessageId = messageId;
                StatusCode = statusCode;
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

                // Переводим в байты и добавляем в конец списка код ответа сервера
                packet.AddRange(BitConverter.GetBytes(StatusCode));

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Ответ сервера на запрос лог-файла

        /// <summary>
        /// Класс, описывающий пакет передачи данных лог-файла клиенту
        /// </summary>
        public class LogData : IServerPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static byte MessageId;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Код ответа сервера
            /// </summary>
            private static short StatusCode;

            /// <summary>
            /// Поле класса, хранящее данные лог-файла
            /// </summary>
            private static byte[] LogFileData;

            /// <summary>
            /// Конструктор класса ответа сервера пользователю.
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения, на которое идёт ответ</param>
            /// <param name="statusCode">Код ответа сервера</param>
            /// <param name="logData">Текущие данные лог-файла</param>
            public LogData(string clientId, byte messageId, short statusCode, byte[] logData)
            {
                ClientID = clientId;
                MessageId = messageId;
                StatusCode = statusCode;
                LogFileData = logData;
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

                // Добавляем в конец списка код ответа сервера
                packet.AddRange(BitConverter.GetBytes(StatusCode));

                // Добавляем в конец списка массив-разделитель
                packet.AddRange(Delimeter);

                // Переводим в байты и добавляем в конец списка данные лог-файла
                packet.AddRange(LogFileData);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion

        #region Ответ сервера на запрос версии

        /// <summary>
        /// Класс, описывающий пакет версии сервера
        /// </summary>
        public class Version : IServerPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static byte MessageId;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Поле класса, хранящее код ответа сервера
            /// </summary>
            private static byte VersionCode;

            /// <summary>
            /// Конструктор класса ответа сервера пользователю.
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения, на которое идёт ответ</param>
            /// <param name="versionCode">Версия сервера</param>
            public Version(string clientId, byte messageId, byte versionCode)
            {
                ClientID = clientId;
                MessageId = messageId;
                VersionCode = versionCode;
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

                // Переводим в байты и добавляем в конец списка версию сервера
                packet.Add(VersionCode);

                // Добавляем массив, сигнализирующий о конце сообщения
                packet.AddRange(End);

                return packet;
            }
        }

        #endregion
    }
}
