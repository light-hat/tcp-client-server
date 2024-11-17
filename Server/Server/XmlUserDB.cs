using System.IO;
using System.Text;
using System.Xml;
using System;

namespace Server
{
    /// <summary>
    /// Класс для создания и авторизации пользователей. 
    /// Также в данном классе реализован функционал работы
    /// с xml-файлом для хранения данных пользователей.
    /// </summary>
    public static class XmlUserDB
    {
        #region Поля класса

        /// <summary>
        /// Имя xml-файла для хранения данных
        /// </summary>
        private static string DatabaseFileName = "users.xml";

        /// <summary>
        /// Класс XML-документа
        /// </summary>
        private static XmlDocument xDoc;

        /// <summary>
        /// Класс для корневого тега в документе
        /// </summary>
        private static XmlElement xRoot;

        #endregion

        #region Работа с XML-файлом для хранения данных пользователей

        /// <summary>
        /// Создаёт xml-файл, выполняющий роль базы данных (условной), 
        /// если таковой не существует.
        /// </summary>
        private static void InitDB()
        {
            // Проверяем, существует ли файл с пользовательскими данными
            if (!File.Exists(DatabaseFileName))
            {
                // Создание файла базы данных
                using (FileStream fs = File.Create(DatabaseFileName))
                {
                    // Создаём XML-файл в начальном виде, добавляем корневые теги
                    string data = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                        "<clients>\n" +
                        "</clients>\n";

                    // Записываем данные в файл
                    fs.Write(Encoding.UTF8.GetBytes(data), 0, data.Length);
                }
            }

            // Инициализация полей класса для документа
            if (xDoc == null || xRoot == null)
            {
                // Создаем объект документа
                xDoc = new XmlDocument();

                // Загружаем документ
                xDoc.Load(DatabaseFileName);

                // Создаем корневой элемент документа
                xRoot = xDoc.DocumentElement;
            }
        }

        #endregion

        #region Интерфейсная часть класса

        /// <summary>
        /// Создаёт пользователя и добавляет его в xml-файл.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="userHash">Хеш от пароля пользователя</param>
        /// <param name="userRole">Права доступа пользователя</param>
        public static void CreateUser(string userId, string userHash, ClientRole userRole)
        {
            // Инициализация
            InitDB();

            // Создаем корневой тег для клиента
            XmlElement client = xDoc.CreateElement("client");

            // Создаём атрибут для клиентского тега (права доступа)
            XmlAttribute attr = xDoc.CreateAttribute("role");

            // Создание дочерних тегов для клиентского тега
            XmlElement idElem = xDoc.CreateElement("id");
            XmlElement hashElem = xDoc.CreateElement("hash");

            // Формируем данные для атрибута для клиентского тега (права доступа)
            XmlText attrText = xDoc.CreateTextNode(userRole.ToString());

            // Формируем данные для дочерних тегов для клиентского тега (id и хеш)
            XmlText idText = xDoc.CreateTextNode(userId);
            XmlText hashText = xDoc.CreateTextNode(userHash);

            // Добавляем данные в атрибут прав доступа
            attr.AppendChild(attrText);

            // Добавляем в дочерние теги данные пользователя
            idElem.AppendChild(idText);
            hashElem.AppendChild(hashText);

            // Добавляем в тег клиента атрибут прав доступа и дочерние теги (id и хеш)
            client.Attributes.Append(attr);
            client.AppendChild(idElem);
            client.AppendChild(hashElem);

            // Добавляем в корневой тег тот, что мы только что создали
            xRoot.AppendChild(client);

            // Сохраняем XML-документ
            xDoc.Save(DatabaseFileName);
        }

        /// <summary>
        /// Ищет пользователя в XML-файле и возвращает
        /// его данные в виде структуры.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns>Структура с данными пользователя</returns>
        public static Credential GetUser(string userId)
        {
            // Инициализация
            InitDB();

            // Цикл по клиентским тегам в документе
            foreach (XmlNode xnode in xRoot)
            {
                Credential user = new Credential();

                // Получаем атрибут тега, который отвечает за права доступа
                XmlNode attr = xnode.Attributes.GetNamedItem("role");
                
                // Записываем права пользователя в структуру
                user.Role = (ClientRole)Enum.Parse(typeof(ClientRole), attr.Value);

                // Цикл по дочерним тегам клиентского тега
                foreach (XmlNode childnode in xnode.ChildNodes)
                {
                    // Запись ID в структуру
                    if (childnode.Name == "id")
                    {
                        user.ClientId = childnode.InnerText;
                    }

                    // Запись хеша в структуру
                    if (childnode.Name == "hash")
                    {
                        user.PasswordHash = childnode.InnerText;
                    }
                }

                // Сравниваем входной ID с текущим элементом в xml-файле
                if (userId == user.ClientId)
                    return user;
            }

            // Пустое значение, если клиент не найден
            return null;
        }

        #endregion
    }
}
