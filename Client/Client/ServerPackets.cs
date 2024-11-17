using System;
using System.Text;
using System.Collections.Generic;

namespace Client
{
    /// <summary>
    /// Класс, описывающий сообщения, которые отправляет сервер.
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
        /// Интерфейс для описания сообщения, принимаемого по сети.
        /// </summary>
        public interface IServerPackets
        {
            /// <summary>
            /// Возвращает идентификатор сообщения
            /// </summary>
            /// <returns>Число, служащее идентификатором сообщения</returns>
            byte GetMessageId();

            /// <summary>
            /// Возвращает код ответа сервера, если он предусмотрен в сообщении.
            /// Если нет - возвращает -1.
            /// </summary>
            /// <returns>Код ответа сервера</returns>
            short GetStatusCode();
        }

        #endregion

        #region Двоичная десериализация данных, полученных по сети

        /// <summary>
        /// Метод класса, преобразующий двоичный массив в структуру сообщения (выполняющий десериализацию)
        /// </summary>
        /// <param name="binary_data">Двоичные данные, принятые по сети</param>
        /// <returns>Структура сообщения, понятная этой программе</returns>
        public static IServerPackets Deserialize(byte[] binary_data)
        {
            try
            {
                // Выходной класс сообщения
                IServerPackets result = null;

                // Разделение по разделителям End (для получения исходного массива) и
                // Delimeter (для получения полей класса сообщения)
                List<byte[]> list = Separate(Separate(binary_data, End)[0], Delimeter);

                // Смотрим на второе полученное поле,
                // что отвечает за тип сообщения
                switch (list[1][0])
                {
                    case 0: // Авторизация
                    case 1: // Отправка файла
                    case 3: // Отправка изменений лога
                    case 5: // Завершение соединения
                        // Создаём экземпляр класса сообщения, на вход конструктора
                        // передаём то, что получили после десериализации
                        result = new Response(Encoding.UTF8.GetString(list[0]),
                            list[1][0],
                            BitConverter.ToInt16(list[2], 0));

                        break;

                    case 2: // Запрос лог-файла
                        try
                        {
                            result = new LogData(Encoding.UTF8.GetString(list[0]),
                                list[1][0],
                                BitConverter.ToInt16(list[2], 0),
                                list[3]);
                        }

                        catch
                        {
                            // В случае ошибки, возвращаем сообщение с массивом-заглушкой
                            result = new LogData(Encoding.UTF8.GetString(list[0]),
                                list[1][0],
                                BitConverter.ToInt16(list[2], 0),
                                new byte[] { 0x00 });
                        }

                        break;

                    case 4: // Запрос версии сервера
                        result = new Version(Encoding.UTF8.GetString(list[0]),
                            list[1][0],
                            list[2][0]);

                        break;
                }

                return result;
            }

            catch (Exception ex)
            {
                Helpers.PrintError(string.Concat("Необработанное исключение: ", ex.Message));
                return null;
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

        #region Универсальный ответ сервера

        /// <summary>
        /// Класс, описывающий сообщение, принятое по сети
        /// </summary>
        public class Response : IServerPackets
        {
            /// <summary>
            /// Хранит ID сообщения
            /// </summary>
            private byte MessageID;

            /// <summary>
            /// Хранит ID клиента
            /// </summary>
            private string ClientID;

            /// <summary>
            /// Хранит код ответа
            /// </summary>
            private short StatusCode;

            /// <summary>
            /// Конструктор класса
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            /// <param name="statusCode">Код ответа сервера</param>
            public Response(string clientId, byte messageId, short statusCode)
            {
                ClientID = clientId;

                MessageID = messageId;

                StatusCode = statusCode;
            }
            
            public byte GetMessageId()
            {
                return MessageID;
            }

            public short GetStatusCode()
            {
                return StatusCode;
            }
        }

        #endregion

        #region Ответ сервера на запрос лог-файла

        /// <summary>
        /// Класс, описывающий сообщение с содержимым лог-файла
        /// </summary>
        public class LogData : IServerPackets
        {
            /// <summary>
            /// Хранит ID сообщения
            /// </summary>
            private byte MessageID;

            /// <summary>
            /// Хранит ID клиента
            /// </summary>
            private string ClientID;

            /// <summary>
            /// Код ответа сервера
            /// </summary>
            private short StatusCode;

            /// <summary>
            /// Хранит данные лог-файла в двоичном виде
            /// </summary>
            private byte[] LogFileData;

            /// <summary>
            /// Конструктор класса
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            /// <param name="statusCode">Код ответа</param>
            /// <param name="logData">Содержимое лог-файла</param>
            public LogData(string clientId, byte messageId, short statusCode, byte[] logData)
            {
                ClientID = clientId;
                MessageID = messageId;
                StatusCode = statusCode;
                LogFileData = logData;
            }

            /// <summary>
            /// Возвращает данные лога
            /// </summary>
            /// <returns>Данные лога в двоичном виде</returns>
            public byte[] GetLogFileData()
            {
                return LogFileData;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            public short GetStatusCode()
            {
                return StatusCode;
            }
        }

        #endregion

        #region Ответ сервера на запрос версии

        /// <summary>
        /// Класс, описывающий сообщение с версией сервера
        /// </summary>
        public class Version : IServerPackets
        {
            /// <summary>
            /// Хранит ID сообщения
            /// </summary>
            private byte MessageID;

            /// <summary>
            /// Хранит ID клиента
            /// </summary>
            private string ClientID;

            /// <summary>
            /// Хранит версию сервера
            /// </summary>
            private byte VersionCode;

            /// <summary>
            /// Конструктор класса
            /// </summary>
            /// <param name="clientId">Идентификатор клиента</param>
            /// <param name="messageId">Идентификатор сообщения</param>
            /// /// <param name="versionCode">Код версии сервера</param>
            public Version(string clientId, byte messageId, byte versionCode)
            {
                ClientID = clientId;

                MessageID = messageId;

                VersionCode = versionCode;
            }

            public byte GetMessageId()
            {
                return MessageID;
            }

            /// <summary>
            /// Возвращает код версии сервера
            /// </summary>
            /// <returns>Код ответа сервера</returns>
            public byte GetVersionCode()
            {
                return VersionCode;
            }

            public short GetStatusCode()
            {
                return -1;
            }
        }

        #endregion
    }
}
