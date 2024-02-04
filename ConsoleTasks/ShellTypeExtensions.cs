using System;

namespace ConsoleTasks
{
    internal static class ShellTypeExtensions
    {
        public static string ToInternalName(this ShellType shellType)
            => shellType switch
            {
                ShellType.CommandPrompt => "bat",
                ShellType.PowerShell => "ps1",
                _ => throw new NotSupportedException(),
            };
    }
}
