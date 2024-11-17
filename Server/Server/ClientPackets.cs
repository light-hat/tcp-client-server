using System;
using System.Text;
using System.Collections.Generic;

namespace Server
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
            /// Метод класса, возвращающий идентификатор
            /// для определения типа принятого сообщения
            /// </summary>
            /// <returns>Тип сообщения</returns>
            byte GetMessageId();

            /// <summary>
            /// Метод класса, возвращающий ID клиента
            /// </summary>
            /// <returns>Идентификатор клиента</returns>
            string GetClientId();
        }

        #endregion

        #region Десериализация и обработка принятых данных

        /// <summary>
        /// Метод класса, преобразующий двоичный массив в структуру сообщения (выполняющий десериализацию)
        /// </summary>
        /// <param name="binary_data">Двоичные данные, принятые по сети</param>
        /// <returns>Структура сообщения, понятная этой программе</returns>
        public static IClientPackets Deserialize(byte[] binary_data)
        {
            try
            {
                // Выходной класс сообщения
                IClientPackets result = null;

                // Разделение по разделителям End (для получения исходного массива) и
                // Delimeter (для получения полей класса сообщения)
                List<byte[]> list = Separate(Separate(binary_data, End)[0], Delimeter);

                // Смотрим на второе полученное поле,
                // что отвечает за тип сообщения
                switch (list[1][0])
                {
                    case 0:
                        // Создаём экземпляр класса сообщения, на вход конструктора
                        // передаём то, что получили после десериализации
                        result = new Auth(Encoding.UTF8.GetString(list[0]),
                            list[1][0],
                            Encoding.UTF8.GetString(list[2]));

                        break;

                    case 1:
                        result = new SendFile(Encoding.UTF8.GetString(list[0]),
                            list[1][0],
                            Encoding.UTF8.GetString(list[2]),
                            BitConverter.ToInt32(list[3], 0),
                            list[4],
                            list[5][0]);

                        break;

                    case 2:
                        result = new LogRequest(Encoding.UTF8.GetString(list[0]), list[1][0]);

                        break;

                    case 3:
                        result = new LogChanges(Encoding.UTF8.GetString(list[0]),
                            list[1][0],
                            list[2]);

                        break;

                    case 4:
                        result = new Version(Encoding.UTF8.GetString(list[0]), list[1][0]);

                        break;

                    case 5:
                        result = new EndConnection(Encoding.UTF8.GetString(list[0]), list[1][0]);

                        break;
                }

                return result;
            }

            catch (Exception ex) // Если поймали исключение, выбрасываем его вызвавшему методу
            {
                throw ex;
            }
        }

        /// <summary>
        /// Разделяет массив на подмассивы по последовательности-разделителю.
        /// </summary>
        /// <param name="source">Исходный массив</param>
        /// <param name="separator">Разделитель</param>
        /// <returns>Список массивов байт, которые получились после разделения</returns>
        private static List<byte[]> Separate(byte[] source, byte[] separator)
        {
            // Возвращаемый список массивов байт
            List<byte[]> list = new List<byte[]>();

            // Смещение в массиве после найденного разделителя
            int num = 0;

            // Временный массив для той части, что между разделителей
            byte[] array;

            // Цикл по длине исходного массива
            for (int i = 0; i < source.Length; i++)
            {
                // Если в исходном массиве найден подмассив-разделитель,
                // начиная с индекса i
                if (Equals(source, separator, i))
                {
                    // Задаем размер временного массива
                    array = new byte[i - num];

                    // Записываем данные из исходного массива во временный по смещению
                    Array.Copy(source, num, array, 0, array.Length);

                    // Добавляем в возвращаемый список
                    list.Add(array);

                    // Устанавливаем новое смещение
                    num = i + separator.Length;

                    // Индекс по исходному массиву смещаем на
                    // значение, которое идет после разделителя
                    i += separator.Length - 1;
                }
            }

            // Выделение подмассива, который находится после последнего разделителя
            array = new byte[source.Length - num];

            // Копирование его значения по смещению
            Array.Copy(source, num, array, 0, array.Length);

            // Добавление его в возвращаемый список
            list.Add(array);

            return list;
        }

        /// <summary>
        /// Ищет в исходном массиве подмассив-разделитель, 
        /// начиная с указанного индекса в исходном массиве.
        /// </summary>
        /// <param name="source">Исходный массив</param>
        /// <param name="separator">Массив-разделитель</param>
        /// <param name="index">Индекс в исходном массиве, поиск начинается с него</param>
        /// <returns>Булево значение, найден разделитель или нет</returns>
        private static bool Equals(byte[] source, byte[] separator, int index)
        {
            // Цикл по массиву-разделителю
            for (int i = 0; i < separator.Length; i++)
            {
                // Если длина массива + индекс в исходном будет больше длины исходного массива, 
                // Или при посимвольном сравнении не совпали значения байт
                if (index + i >= source.Length || source[index + i] != separator[i])
                {
                    // Сообщаем, что не найден разделитель
                    return false;
                }
            }

            // Сообщаем, что разделитель найден
            return true;
        }

        #endregion

        #region Сообщение 0 (авторизация)

        /// <summary>
        /// Класс, описывающий запрос на авторизацию
        /// </summary>
        public class Auth : IClientPackets
        {
            /// <summary>
            /// Поле класса, хранящее Id сообщения
            /// </summary>
            private static byte MessageID;

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
            /// <param name="messageId">Идентификатор типа сообщения</param>
            /// <param name="passwordHash">Хеш от пароля клиента</param>
            public Auth(string clientId, byte messageId, string passwordHash)
            {
                ClientID = clientId;
                MessageID = messageId;
                PasswordHash = passwordHash;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public string GetClientId()
            {
                return ClientID;
            }

            /// <summary>
            /// Метод класса, возвращающий SHA-512 хеш от пользовательского пароля, принятый по сети
            /// </summary>
            /// <returns>Хеш от пароля пользователя</returns>
            public string GetPasswordHash()
            {
                return PasswordHash;
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
            private static byte MessageID;

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
            /// Для какой версии сервера передаётся файл
            /// </summary>
            private static byte ServerVersion;

            /// <summary>
            /// Конструктор класса сообщения передачи файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            /// <param name="fileName">Имя файла</param>
            /// <param name="fileSize">Размер файла</param>
            /// <param name="fileData">Содержимое файла</param>
            /// <param name="serverVersion">Версия сервера</param>
            public SendFile(string clientId,
                byte messageId,
                string fileName,
                int fileSize,
                byte[] fileData,
                byte serverVersion)
            {
                ClientID = clientId;
                MessageID = messageId;
                FileName = fileName;
                FileSize = fileSize;
                FileData = fileData;
                ServerVersion = serverVersion;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public string GetClientId()
            {
                return ClientID;
            }

            /// <summary>
            /// Возвращает имя файла
            /// </summary>
            /// <returns>Строка с именем файла</returns>
            public string GetFileName()
            {
                return FileName;
            }

            /// <summary>
            /// Возвращает размер файла в байтах
            /// </summary>
            /// <returns>Число типа int соответствующее размеру файла</returns>
            public int GetFileSize()
            {
                return FileSize;
            }

            /// <summary>
            /// Возвращает содержимое файла в двоичном виде
            /// </summary>
            /// <returns>Двоичный массив с содержимым файла</returns>
            public byte[] GetFileData()
            {
                return FileData;
            }

            /// <summary>
            /// Возвращает версию сервера, для которого передается файл
            /// </summary>
            /// <returns>Версия файла</returns>
            public byte GetServerVersion()
            {
                return ServerVersion;
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
            private static byte MessageID;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            public LogRequest(string clientId, byte messageId)
            {
                MessageID = messageId;
                ClientID = clientId;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public string GetClientId()
            {
                return ClientID;
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
            private static byte MessageID;

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
            /// <param name="messageId">Идентификатор сообщения</param>
            /// <param name="logData">Новые данные лог-файла</param>
            public LogChanges(string clientId, byte messageId, byte[] logData)
            {
                MessageID = messageId;
                ClientID = clientId;
                LogData = logData;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public string GetClientId()
            {
                return ClientID;
            }

            /// <summary>
            /// Возвращает данные измененного лог-файла
            /// </summary>
            /// <returns>Двоичный массив с новыми данными для лога</returns>
            public byte[] GetLogData()
            {
                return LogData;
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
            private static byte MessageID;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            public Version(string clientId, byte messageId)
            {
                MessageID = messageId;
                ClientID = clientId;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public string GetClientId()
            {
                return ClientID;
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
            private static byte MessageID;

            /// <summary>
            /// Поле класса, хранящее Id клиента
            /// </summary>
            private static string ClientID;

            /// <summary>
            /// Конструктор класса запроса на редактирование лог-файла
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            public EndConnection(string clientId, byte messageId)
            {
                MessageID = messageId;
                ClientID = clientId;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public string GetClientId()
            {
                return ClientID;
            }
        }

        #endregion
    }
}