using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Palmtree.IO;
using Palmtree.IO.Console;

namespace ConsoleTasks
{
    internal abstract class ConsoleTaskState
    {
        private readonly ConsoleTask _task;
        private readonly FilePath _taskFile;
        private readonly TaskLockObject _lockObject;
        private readonly Encoding _encoding;

        protected ConsoleTaskState(ConsoleTask task, FilePath taskFile, TaskLockObject taskLockObject, Encoding encoding)
        {
            _task = task;
            _taskFile = taskFile;
            _lockObject = taskLockObject;
            _encoding = encoding;
            TaskId = taskFile.NameWithoutExtension.ToUpperInvariant();
        }

        public string TaskId { get; }
        public FilePath CommandFile => _task.CommandFile;

        public void Execute()
        {
            var baseDirectory = ConsoleTaskQueue.BaseDirectory;
            Environment.CurrentDirectory = baseDirectory.FullName;
            var intermediateScriptFile = (FilePath?)null;
            try
            {
                intermediateScriptFile = CreateIntermediateScriptFile(baseDirectory);
                var startInfo = new ProcessStartInfo
                {
                    FileName = ShellExecutableName,
                    Arguments = GetShellParameter(baseDirectory, intermediateScriptFile),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardInputEncoding = _encoding,
                    StandardOutputEncoding = _encoding,
                    StandardErrorEncoding = _encoding,
                };

                var process = Process.Start(startInfo);
                if (process is not null)
                {
                    try
                    {
                        var standardInputTransferTask = Task.Run(() =>
                        {
                            using var inStream = TinyConsole.OpenStandardInput().AsTextReader(_encoding);
                            using var outStream = process.StandardInput;
                            CopyTextStream(inStream, outStream);
                        });

                        var standardOutputTransferTask = Task.Run(() =>
                        {
                            using var inStream = process.StandardOutput;
                            using var outStream = TinyConsole.OpenStandardOutput().AsTextWriter(_encoding, autoFlush: true);
                            CopyTextStream(inStream, outStream);
                        });

                        var standardErrorTransferTask = Task.Run(() =>
                        {
                            using var inStream = process.StandardError;
                            using var outStream = TinyConsole.OpenStandardError().AsTextWriter(_encoding, autoFlush: true);
                            CopyTextStream(inStream, outStream);
                        });

                        Task.WhenAll(standardOutputTransferTask, standardErrorTransferTask).Wait();
                        process.WaitForExit();
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            finally
            {
                intermediateScriptFile?.SafetyDelete();
            }

            static void CopyTextStream(TextReader reader, TextWriter writer)
            {
                var buffer = new char[1024];
                while (true)
                {
                    var length = reader.Read(buffer);
                    if (length <= 0)
                        break;
                    writer.Write(buffer, 0, length);
                }
            }
        }

        public void Complete()
        {
            _lockObject.Dispose();
            _taskFile.Delete();
        }

        public static ConsoleTaskState CreateInstance(ConsoleTask task, FilePath taskFile, TaskLockObject taskLockObject, Encoding encoding)
            => task.ShellType switch
            {
                ShellType.CommandPrompt => OperatingSystem.IsWindows() ? new CommandPromptConsoleTaskState(task, taskFile, taskLockObject, encoding) : throw new InvalidOperationException(),
                ShellType.PowerShell => new PowerShellConsoleTaskState(task, taskFile, taskLockObject, encoding),
                _ => throw new NotSupportedException(),
            };

        protected abstract FilePath CreateIntermediateScriptFile(DirectoryPath baseDirectory);
        protected abstract string ShellExecutableName { get; }
        protected abstract string GetShellParameter(DirectoryPath baseDirectory, FilePath intermediateScriptFile);
    }
}
