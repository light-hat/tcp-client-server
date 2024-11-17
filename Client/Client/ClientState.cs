namespace Client
{
    /// <summary>
    /// Перечисление, определяющее каждое состояние данной программы-клиента.
    /// </summary>
    public enum ClientState
    {
        Disconnected,
        Waiting,
        Ready,
        TextEditor,
    }
}
