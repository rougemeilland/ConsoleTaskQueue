using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleTasks;
using Palmtree;
using Palmtree.Application;
using Palmtree.IO;
using Palmtree.IO.Console;

namespace ConsoleTask.CUI
{
    internal static partial class Program
    {
        private sealed partial class ClientApplication
            : ApplicationBase
        {
            private readonly string _title;
            private readonly Encoding _encoding;

            static ClientApplication()
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }

            public ClientApplication(string title, Encoding encoding)
            {
                _title = title;
                _encoding = encoding;
            }

            protected override string ConsoleWindowTitle => _title;
            protected override Encoding? InputOutputEncoding => _encoding;

            protected override ResultCode Main(string[] args)
            {
                try
                {
                    var newArgs = new List<string>();
                    var pathEnvironmentVariables = new List<string>();
                    var environmentVariables = new List<(string Name, string Value)>();
                    if (args.Length == 1 && args[0] == "list")
                    {
                        newArgs.Add(args[0]);
                    }
                    else if (args.Length > 0 && args[0] == "add")
                    {
                        newArgs.Add(args[0]);
                        for (var index = 1; index < args.Length; ++index)
                        {
                            var arg = args[index];
                            if ((arg == "-p" || arg == "--pathenv") && index + 1 < args.Length)
                            {
                                pathEnvironmentVariables.Add(args[index + 1]);
                                ++index;
                            }
                            else if ((arg == "-e" || arg == "--env") && index + 1 < args.Length)
                            {
                                var match = GetEnvironmentValuePattern().Match(args[index + 1]);
                                if (!match.Success)
                                    throw new ApplicationException($"The environment variable specification is incorrect.: \"{args[index + 1]}\"");
                                environmentVariables.Add((match.Groups["name"].Value, match.Groups["value"].Value));
                                ++index;
                            }
                            else if (arg.StartsWith('-'))
                            {
                                throw new ApplicationException($"Unsupported command option is specified.: \"{arg}\"");
                            }
                            else
                            {
                                newArgs.Add(arg);
                            }
                        }
                    }
                    else
                    {
                        throw new ApplicationException($"Unsupported command option is specified.: \"{args[0]}\"");
                    }

                    using var queue = new ConsoleTaskQueue();
                    if (newArgs.Count <= 0)
                    {
                        ReportErrorMessage("No arguments specified.");
                        return ResultCode.Failed;
                    }
                    else if (newArgs[0] == "add")
                    {
                        if (newArgs.Count != 2)
                        {
                            ReportErrorMessage("Invalid argument syntax.");
                            return ResultCode.Failed;
                        }

                        var commandFile = GetFilePath(newArgs[1]);
                        if (!commandFile.Exists)
                            throw new ApplicationException($"Script file does not exist.: \"{commandFile.FullName}\"");

                        if (string.Equals(commandFile.Extension, ".ps1", StringComparison.Ordinal))
                        {
                            if (_encoding.CodePage == Encoding.UTF8.CodePage)
                            {
                                using var inStream = commandFile.OpenRead();
                                Span<byte> buffer = stackalloc byte[3];
                                if (inStream.ReadBytes(buffer) != buffer.Length
                                    || buffer[0] != 0xef
                                    || buffer[1] != 0xbb
                                    || buffer[2] != 0xbf)
                                {
                                    throw new ApplicationException("BOM must be present if the powershell script file is in UTF-8 encoding.");
                                }
                            }
                            else if (_encoding.CodePage == Encoding.Unicode.CodePage)
                            {
                                using var inStream = commandFile.OpenRead();
                                Span<byte> buffer = stackalloc byte[2];
                                if (inStream.ReadBytes(buffer) != buffer.Length
                                    || buffer[0] != 0xff
                                    || buffer[1] != 0xfe)
                                {
                                    throw new ApplicationException("BOM must be present if the powershell script file is in UTF-16 encoding.");
                                }
                            }
                            else if (_encoding.CodePage == Encoding.BigEndianUnicode.CodePage)
                            {
                                using var inStream = commandFile.OpenRead();
                                Span<byte> buffer = stackalloc byte[2];
                                if (inStream.ReadBytes(buffer) != buffer.Length
                                    || buffer[0] != 0xfe
                                    || buffer[1] != 0xff)
                                {
                                    throw new ApplicationException("BOM must be present if the powershell script file is in UTF-16 encoding.");
                                }
                            }
                            else if (_encoding.CodePage == Encoding.UTF32.CodePage)
                            {
                                using var inStream = commandFile.OpenRead();
                                Span<byte> buffer = stackalloc byte[4];
                                if (inStream.ReadBytes(buffer) != buffer.Length
                                    || buffer[0] != 0xff
                                    || buffer[1] != 0xfe
                                    || buffer[2] != 0x00
                                    || buffer[3] != 0x00)
                                {
                                    throw new ApplicationException("BOM must be present if the powershell script file is in UTF-32 encoding.");
                                }
                            }
                            else if (_encoding.CodePage == Encoding.GetEncoding("utf-32BE").CodePage)
                            {
                                using var inStream = commandFile.OpenRead();
                                Span<byte> buffer = stackalloc byte[4];
                                if (inStream.ReadBytes(buffer) != buffer.Length
                                    || buffer[0] != 0x00
                                    || buffer[1] != 0x00
                                    || buffer[2] != 0xfe
                                    || buffer[3] != 0xff)
                                {
                                    throw new ApplicationException("BOM must be present if the powershell script file is in UTF-32 encoding.");
                                }
                            }
                        }

                        queue.Enqueue(new ConsoleTasks.ConsoleTask(commandFile, new DirectoryPath(Environment.CurrentDirectory), pathEnvironmentVariables, environmentVariables));

                        ReportInformationMessage($"Task added.: \"{commandFile.FullName}\"");
                        return ResultCode.Success;
                    }
                    else if (newArgs[0] == "list")
                    {
                        if (newArgs.Count != 1)
                        {
                            ReportErrorMessage("Invalid argument syntax.");
                            return ResultCode.Failed;
                        }

                        foreach (var task in queue.EnumerateConsoleTasks())
                            TinyConsole.WriteLine(task.CommandFile.FullName);

                        return ResultCode.Success;
                    }
                    else
                    {
                        ReportErrorMessage("Invalid argument syntax.");
                        return ResultCode.Failed;
                    }
                }
                catch (Exception ex)
                {
                    ReportException(ex);
                    return ResultCode.Failed;
                }
            }

            protected override void Finish(ResultCode result, bool isLaunchedByConsoleApplicationLauncher)
            {
                if (isLaunchedByConsoleApplicationLauncher)
                {
                    TinyConsole.WriteLine("Hit ENTER key to exit.");
                    TinyConsole.Beep();
                    _ = TinyConsole.ReadLine();
                }
            }

            private static FilePath GetFilePath(string arg)
            {
                try
                {
                    return new FilePath(arg);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException($"Not a valid file path name.: \"{arg}\"", ex);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [GeneratedRegex("^(?<name>[A-Za-z_][A-Za-z0-9_]*)=(?<value>.*)$")]
            private static partial Regex GetEnvironmentValuePattern();
        }

        private static int Main(string[] args)
        {
            ProcessUtility.SetupCurrentProcessPriority();

            if (TinyConsole.InputEncoding.CodePage != Encoding.UTF8.CodePage || TinyConsole.OutputEncoding.CodePage != Encoding.UTF8.CodePage)
            {
                if (OperatingSystem.IsWindows())
                    TinyConsole.WriteLog(LogCategory.Warning, "The encoding of standard input or output is not UTF8. Consider running the command \"chcp 65001\".");
                else
                    TinyConsole.WriteLog(LogCategory.Warning, "The encoding of standard input or standard output is not UTF8.");
            }

            TinyConsole.DefaultTextWriter = ConsoleTextWriterType.StandardError;
            return new ClientApplication("Task Queue Client", Encoding.UTF8).Run(args);
        }
    }
}
