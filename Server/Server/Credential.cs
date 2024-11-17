namespace Server
{
    /// <summary>
    /// Структура для более удобного представления
    /// пользовательских данных.
    /// </summary>
    public class Credential
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Хеш пароля
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Права доступа пользователя
        /// </summary>
        public ClientRole Role { get; set; }
    }
}
