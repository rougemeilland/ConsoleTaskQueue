using System.Runtime.Versioning;
using System.Text;
using Palmtree;
using Palmtree.IO;

namespace ConsoleTasks
{
    [SupportedOSPlatform("windows")]
    internal class CommandPromptConsoleTaskState
        : ConsoleTaskState
    {
        private readonly ConsoleTask _task;
        private readonly Encoding _encoding;

        public CommandPromptConsoleTaskState(ConsoleTask task, FilePath taskFile, TaskLockObject taskLockObject, Encoding encoding)
            : base(task, taskFile, taskLockObject, encoding)
        {
            _task = task;
            _encoding = encoding;
        }

        protected override FilePath CreateIntermediateScriptFile(DirectoryPath baseDirectory)
        {
            var intermediateScriptFile = baseDirectory.GetFile($"{_task.CommandFile.NameWithoutExtension}.bat");
            using var writer = intermediateScriptFile.CreateText(_encoding);
            writer.WriteLine("@echo off");
            writer.WriteLine($"chcp {_encoding.CodePage}>NUL");
            writer.WriteLine($"cd /d {_task.WorkingDirectory.FullName.CommandLineArgumentEncode().CommandPromptCommandLineArgumentEncode()}");
            writer.WriteLine(_task.CommandFile.FullName.CommandLineArgumentEncode().CommandPromptCommandLineArgumentEncode());
            return intermediateScriptFile;
        }

        protected override string ShellExecutableName => "cmd";

        protected override string GetShellParameter(DirectoryPath baseDirectory, FilePath intermediateScriptFile)
        {
            Validation.Assert(intermediateScriptFile.Directory.FullName == baseDirectory.FullName, "intermediateScriptFile.Directory.FullName == baseDirectory.FullName");
            return $"/c {intermediateScriptFile.Name.CommandLineArgumentEncode().CommandPromptCommandLineArgumentEncode()}";
        }
    }
}
