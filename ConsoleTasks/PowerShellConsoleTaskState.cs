using System.Text;
using Palmtree;
using Palmtree.IO;

namespace ConsoleTasks
{
    internal class PowerShellConsoleTaskState
        : ConsoleTaskState
    {
        private readonly ConsoleTask _task;
        private readonly FilePath _taskFile;
        private readonly Encoding _encoding;

        public PowerShellConsoleTaskState(ConsoleTask task, FilePath taskFile, TaskLockObject taskLockObject, Encoding encoding)
            : base(task, taskFile, taskLockObject, encoding)
        {
            _task = task;
            _taskFile = taskFile;
            _encoding = encoding;
        }

        protected override FilePath CreateIntermediateScriptFile(DirectoryPath baseDirectory)
        {
            var intermediateScriptFile = baseDirectory.GetFile($"{_taskFile.NameWithoutExtension}.bat");
            using var writer = intermediateScriptFile.CreateText(_encoding);
            writer.WriteLine($"[Console]::OutputEncoding = [System.Text.Encoding]::GetEncoding({_encoding.CodePage})");
            writer.WriteLine($"Start-Process -FilePath powershell -ArgumentList {$"-File {_task.CommandFile.Name.CommandLineArgumentEncode()}".CommandLineArgumentEncode()} -NoNewWindow -Wait");
            return intermediateScriptFile;
        }

        protected override string ShellExecutableName => "powershell";

        protected override string GetShellParameter(DirectoryPath baseDirectory, FilePath intermediateScriptFile)
        {
            Validation.Assert(intermediateScriptFile.Directory.FullName == baseDirectory.FullName, "intermediateScriptFile.Directory.FullName == baseDirectory.FullName");
            return $"-ExecutionPolicy Unrestricted -File {_task.CommandFile.FullName.CommandLineArgumentEncode()}";
        }
    }
}
