using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleTasks;
using Palmtree.Application;
using Palmtree.IO;
using Palmtree.IO.Console;

namespace ConsoleTask.CUI
{
    internal partial class Program
    {
        private partial class ClientApplication
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
                    var isRawCommandParameter = false;
                    if (args.Length == 1 && args[0] == "list")
                    {
                    }
                    else if (args.Length > 0 && args[0] == "add")
                    {
                        for (var index = 0; index < args.Length; ++index)
                        {
                            var arg = args[index];
                            if (isRawCommandParameter)
                            {
                                newArgs.Add(arg);
                            }
                            else if ((arg == "-p" || arg == "--pathenv") && index + 1 < args.Length)
                            {
                                pathEnvironmentVariables.Add(args[index + 1]);
                                ++index;
                            }
                            else if ((arg == "-e" || arg == "--env") && index + 1 < args.Length)
                            {
                                var match = GetEnvironmentValuePattern().Match(args[index + 1]);
                                if (!match.Success)
                                    throw new Exception($"The environment variable specification is incorrect.: \"{args[index + 1]}\"");
                                environmentVariables.Add((match.Groups["name"].Value, match.Groups["value"].Value));
                                ++index;
                            }
                            else if (arg == "add")
                            {
                                isRawCommandParameter = true;
                            }
                            else
                            {
                                throw new Exception($"Unsupported command option is specified.: \"{arg}\"");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Unsupported command option is specified.: \"{args[0]}\"");
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
                            throw new Exception($"Script file does not exist.: \"{commandFile.FullName}\"");

                        if (string.Equals(commandFile.Extension, ".ps1"))
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
                                    throw new Exception("BOM must be present if the powershell script file is in UTF-8 encoding.");
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
                                    throw new Exception("BOM must be present if the powershell script file is in UTF-16 encoding.");
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
                                    throw new Exception("BOM must be present if the powershell script file is in UTF-16 encoding.");
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
                                    throw new Exception("BOM must be present if the powershell script file is in UTF-32 encoding.");
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
                                    throw new Exception("BOM must be present if the powershell script file is in UTF-32 encoding.");
                                }
                            }
                        }

                        queue.Enqueue(new ConsoleTasks.ConsoleTask(commandFile, pathEnvironmentVariables, environmentVariables));

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
                    throw new Exception($"Not a valid file path name.: \"{arg}\"", ex);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [GeneratedRegex("^(?<name>[A-Za-z_][A-Za-z0-9_]*)=(?<value>.*)$")]
            private static partial Regex GetEnvironmentValuePattern();
        }

        static int Main(string[] args)
            => new ClientApplication("Task Queue Client", Encoding.UTF8).Run(args);
    }
}
