using System;

namespace ConsoleTasks
{
    internal static class Constants
    {
        internal static readonly string InterProcessResourceNamePrefix = $"{typeof(ConsoleTaskQueue).FullName}-{{127006A4-719F-4ED8-AA99-F5F3828E55F9}}";
        internal static readonly DateTime NotAvailableDateTime = new(0, DateTimeKind.Utc);

    }
}
