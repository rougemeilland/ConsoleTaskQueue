using System.Text;
using Palmtree;
using Palmtree.IO;

namespace ConsoleTasks
{
    internal sealed class PowerShellConsoleTaskState
        : ConsoleTaskState
    {
        private readonly ConsoleTask _task;
        private readonly Encoding _encoding;

        public PowerShellConsoleTaskState(ConsoleTask task, FilePath taskFile, TaskLockObject taskLockObject, Encoding encoding)
            : base(task, taskFile, taskLockObject, encoding)
        {
            _task = task;
            _encoding = encoding;
        }

        protected override FilePath CreateIntermediateScriptFile(DirectoryPath baseDirectory)
        {
            var intermediateScriptFile = baseDirectory.GetFile($"{_task.CommandFile.NameWithoutExtension}.bat");
            using var writer = intermediateScriptFile.CreateText(_encoding);
            writer.WriteLine($"[Console]::OutputEncoding = [System.Text.Encoding]::GetEncoding({_encoding.CodePage})");
            writer.WriteLine($"Set-Location -Path {_task.WorkingDirectory.FullName.PowerShellCommandLineArgumentEncode()}");
            writer.WriteLine($"Start-Process -FilePath powershell -ArgumentList {$"-File {_task.CommandFile.Name.PowerShellCommandLineArgumentEncode()}".PowerShellCommandLineArgumentEncode()} -NoNewWindow -Wait");
            return intermediateScriptFile;
        }

        protected override string ShellExecutableName => "powershell";

        protected override string GetShellParameter(DirectoryPath baseDirectory, FilePath intermediateScriptFile)
        {
            Validation.Assert(intermediateScriptFile.Directory.FullName == baseDirectory.FullName);
            return $"-ExecutionPolicy Unrestricted -File {intermediateScriptFile.Name.PowerShellCommandLineArgumentEncode()}";
        }
    }
}
