using System.Security.Cryptography;
using System.Text;

namespace Client
{
    /// <summary>
    /// Класс для вычисления хеша от строки
    /// </summary>
    public static class Hash
    {
        /// <summary>
        /// Создаёт SHA-512 хеш от введённой парольной строки
        /// </summary>
        /// <param name="password">Парольная строка</param>
        /// <returns>Хеш в виде строки</returns>
        public static string GetSha512(string password)
        {
            // Парольную строку переводим в байты
            var bytes = Encoding.UTF8.GetBytes(password);

            // Создаём объект класса для вычисления хеша
            using (var hash = SHA512.Create())
            {
                // Вычисляем хеш от байт входной строки
                var hashedInputBytes = hash.ComputeHash(bytes);

                // Создаём объект класса для построения строки длиной 128 символов
                var hashedInputStringBuilder = new StringBuilder(128);

                // Цикл по байтам в полученном хеше
                foreach (var b in hashedInputBytes)
                    // Переводим байт в строковый вид и добавляем его к выходной строке
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                // Возвращаем сформированную строку с хешем
                return hashedInputStringBuilder.ToString();
            }
        }
    }
}
