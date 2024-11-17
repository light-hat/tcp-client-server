using System;
using System.IO;
using System.Text;

namespace Server
{
    /// <summary>
    /// Класс для обработки принятых сообщений в зависимости от их типа
    /// </summary>
    public static class PacketHandler
    {
        /// <summary>
        /// Метод класса, который принимает на вход структуру принятого по сети сообщения,
        /// и в зависимости от типа сообщения и его содержимого, выполняет необходимые
        /// действия.
        /// </summary>
        /// <param name="clientObject">Класс подключенного клиента</param>
        /// <param name="packet">Структура принятого по сети сообщения</param>
        public static void HandlePacket(ClientObject clientObject, ClientPackets.IClientPackets packet)
        {
            // Объект для синхронизации потоков
            object locker = new object();

            // Определяем тип принятого сообщения
            switch (packet.GetMessageId())
            {
                case 1:
                    {
                        try
                        {
                            // Вывод сообщения
                            Helpers.PrintMessage(string.Concat("Началась передача файла от клиента ", clientObject.ClientId));

                            // Приводим типы
                            ClientPackets.SendFile file = (ClientPackets.SendFile)packet;

                            // Сравниваем версии сервера (из сообщения с текущей)
                            if (file.GetServerVersion() == Program.ServerVersion)
                            {
                                // Если принимаем текстовый файл
                                if (Program.ServerVersion == 1)
                                    File.WriteAllText(file.GetFileName(), Encoding.UTF8.GetString(file.GetFileData()));

                                // Если принимаем двоичный файл
                                else if (Program.ServerVersion == 2)
                                    File.WriteAllBytes(file.GetFileName(), file.GetFileData());

                                // Синхронизация потоков
                                lock (locker)
                                {
                                    // Делаем запись в файл журнала
                                    using (StreamWriter sw = File.AppendText(Program.LogFileName))
                                    {
                                        sw.WriteLine(string.Concat(
                                                "[",
                                                DateTime.Now,
                                                "]\t\t",
                                                clientObject.ClientId,
                                                "\t\t",
                                                clientObject.IpAddress,
                                                "\t\t",
                                                file.GetFileName(),
                                                "\t\t",
                                                file.GetFileSize(),
                                                " bytes"
                                            ));
                                    }
                                }

                                // Выводим сообщение
                                Helpers.PrintSuccess(string.Concat("Клиент ",
                                    clientObject.ClientId,
                                    " передал файл ",
                                    file.GetFileName(),
                                    " объемом ",
                                    file.GetFileSize(),
                                    " байт"));

                                // Отправка сообщения об успехе
                                clientObject.SendSuccessResponse(clientObject.ClientId, 1);
                            }

                            else
                            {
                                // Выводим сообщение
                                Helpers.PrintWarning(string.Concat("Клиент ",
                                    clientObject.ClientId,
                                    " попытался отправить файл для другой версии сервера"));

                                // Шлём ошибку bad request
                                clientObject.SendError(clientObject.ClientId, 1, 400);
                            }
                        }

                        catch (Exception e)
                        {
                            Helpers.PrintError(string.Concat("Ошибка при приёме файла: ", e.Message));
                        }
                    }

                    break;

                case 2:
                    {
                        // Синхронизация потоков
                        lock (locker)
                        {
                            // Выводим сообщение
                            Helpers.PrintMessage(string.Concat("Клиент ",
                                clientObject.ClientId,
                                " начал редактирование журнала"));

                            // Проверяем права доступа
                            if (clientObject.ClientRole == ClientRole.Admin)
                            {
                                // Читаем файл журнала
                                clientObject.SendLog(clientObject.ClientId, File.ReadAllBytes(Program.LogFileName));
                            }

                            else
                            {
                                // Выводим сообщение
                                Helpers.PrintWarning(string.Concat("Клиент ",
                                    clientObject.ClientId,
                                    " запросил журнал. Прав недостаточно. В доступе отказано."));

                                // Прав недостаточно, шлём ошибку
                                clientObject.SendError(clientObject.ClientId, 2, 403);
                            }
                        }
                    }

                    break;

                case 3:
                    {
                        // Синхронизация потоков
                        lock (locker)
                        {
                            if (clientObject.ClientRole == ClientRole.Admin)
                            {
                                // Приводим типы
                                ClientPackets.LogChanges new_log = (ClientPackets.LogChanges)packet;

                                try
                                {
                                    // Перезаписываем лог-файл
                                    File.WriteAllText(Program.LogFileName,
                                        Encoding.UTF8.GetString(new_log.GetLogData()));

                                    // Выводим сообщение об успехе
                                    Helpers.PrintSuccess(string.Concat("Клиент ",
                                        clientObject.ClientId,
                                        " внёс правки в журнал"));

                                    // Отправляем сообщение об успехе
                                    clientObject.SendSuccessResponse(clientObject.ClientId, 3);
                                }

                                catch (Exception ex) // Обработка возможных исключений
                                {
                                    // Вывод сообщения
                                    Helpers.PrintError(string.Concat("Ошибка при приёме журнала: ", ex.Message));

                                    // Отправляем сообщение с ошибкой
                                    clientObject.SendError(clientObject.ClientId, 3, 500);
                                }

                                finally
                                {
                                    // Переводим сервер в обычное состояние
                                    Program.State = ServerState.Normal;

                                    // Очистка мусора
                                    GC.Collect();
                                }
                            }
                        }
                    }

                    break;

                case 4:
                    {
                        // Вывод сообщения
                        Helpers.PrintMessage(string.Concat("Клиент ",
                            clientObject.ClientId,
                            " запросил версию сервера"));

                        // Отправляем клиенту текущую версию сервера
                        clientObject.SendVersion(clientObject.ClientId);
                    }

                    break;

                case 5:
                    {
                        // Вывод сообщения
                        Helpers.PrintMessage(string.Concat("Клиент ",
                            clientObject.ClientId,
                            " отключился."));

                        // Закрытие клиентского сокета
                        clientObject.Client.Close();
                    }

                    break;
            }
        }
    }
}
