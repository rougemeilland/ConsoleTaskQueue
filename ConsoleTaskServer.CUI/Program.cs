﻿using System;
using System.Text;
using ConsoleTasks;
using Palmtree;
using Palmtree.Application;
using Palmtree.IO;
using Palmtree.IO.Console;
using Palmtree.Text;

namespace ConsoleTaskServer.CUI
{
    internal static class Program
    {
        private sealed class ServerApplication
            : ApplicationBase
        {
            private readonly string _title;
            private readonly Encoding _encoding;

            private bool _blinkingState;

            public ServerApplication(string title, Encoding encoding)
            {
                _blinkingState = false;
                _title = title;
                _encoding = encoding.WithoutPreamble();
            }

            protected override string ConsoleWindowTitle => _title;
            protected override Encoding? InputOutputEncoding => _encoding;

            protected override ResultCode Main(string[] args)
            {
                try
                {
                    using var queue = new ConsoleTaskQueue();
                    while (true)
                        queue.DequeueAndExecute(_encoding, Idling, Starting, Ending);
                }
                catch (OperationCanceledException)
                {
                    return ResultCode.Cancelled;
                }
            }

            protected override void Finish(ResultCode result, bool isLaunchedByConsoleApplicationLauncher)
            {
                if (result == ResultCode.Success)
                    TinyConsole.WriteLine("Completed.");
                else if (result == ResultCode.Cancelled)
                    TinyConsole.WriteLine("Cancelled.");

                if (isLaunchedByConsoleApplicationLauncher)
                {
                    TinyConsole.WriteLine("Hit ENTER key to exit.");
                    TinyConsole.Beep();
                    _ = TinyConsole.ReadLine();
                }
            }

            private void Idling()
            {
                TinyConsole.Erase(ConsoleEraseMode.FromCursorToEndOfScreen);

                if (IsPressedBreak)
                    throw new OperationCanceledException();

                if (_blinkingState)
                {
                    TinyConsole.ForegroundColor = ConsoleColor.Yellow;
                    TinyConsole.BackgroundColor = ConsoleColor.Black;
                }
                else
                {
                    TinyConsole.ForegroundColor = ConsoleColor.Black;
                    TinyConsole.BackgroundColor = ConsoleColor.Yellow;
                }

                TinyConsole.Write("Waiting for task to be queued... (Exit to Press Ctrl+C)\r");
                TinyConsole.ResetColor();

                _blinkingState = !_blinkingState;
            }

            private void Starting(FilePath commandFile)
            {
                TinyConsole.ResetColor();
                TinyConsole.Erase(ConsoleEraseMode.FromCursorToEndOfScreen);
                TinyConsole.WriteLine($"Start task.: \"{commandFile.FullName}\"");
            }

            private void Ending(FilePath commandFile)
            {
                TinyConsole.ResetColor();
                TinyConsole.Erase(ConsoleEraseMode.FromCursorToEndOfScreen);
                TinyConsole.WriteLine($"Task finished.: \"{commandFile.FullName}\"");
            }
        }

        private static void Main(string[] args)
        {
            if (TinyConsole.InputEncoding.CodePage != Encoding.UTF8.CodePage || TinyConsole.OutputEncoding.CodePage != Encoding.UTF8.CodePage)
            {
                if (OperatingSystem.IsWindows())
                    TinyConsole.WriteLog(LogCategory.Warning, "The encoding of standard input or output is not UTF8. Consider running the command \"chcp 65001\".");
                else
                    TinyConsole.WriteLog(LogCategory.Warning, "The encoding of standard input or standard output is not UTF8.");
            }

            TinyConsole.DefaultTextWriter = ConsoleTextWriterType.StandardError;
            var currentDirectory = Environment.CurrentDirectory;
            try
            {
                _ = new ServerApplication("Console Task Server", Encoding.UTF8).Run(args);
            }
            finally
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }
    }
}
