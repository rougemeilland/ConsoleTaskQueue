namespace ConsoleTasks
{
    public enum TaskStatus
        : byte
    {
        Uninitialized = 0,
        Idling = 1,
        Running = 2,
        Completed = 3,
    }
}
