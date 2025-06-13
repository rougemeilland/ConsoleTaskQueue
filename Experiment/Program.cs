using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Experiment
{
    internal static class Program
    {
        static Program()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:未使用のパラメーターを削除します", Justification = "<保留中>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:メソッドに式本体を使用する", Justification = "<保留中>")]
        private static void Main(string[] args)
        {
            Execute();
        }

        private static void Execute()
        {
            var baseDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? throw new Exception();
            Environment.CurrentDirectory = baseDirectory;
            var intermediateScriptFile = (string?)null;
            try
            {
                intermediateScriptFile = CreateIntermediateScriptFile(baseDirectory);
                var startInfo = new ProcessStartInfo
                {
                    FileName = ShellExecutableName,
                    Arguments = GetShellParameter(baseDirectory, intermediateScriptFile),
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Minimized,
                };
                var process = Process.Start(
                    startInfo);
                try
                {
                    process?.WaitForExit();
                }
                finally
                {
                    process?.Dispose();
                }
            }
            finally
            {
                if (intermediateScriptFile is not null)
                {
                    if (File.Exists(intermediateScriptFile))
                        File.Delete(intermediateScriptFile);
                }
            }
        }

        private static string CreateIntermediateScriptFile(string baseDirectory)
        {
            var path1 = Path.Combine(baseDirectory, "script1.bat");
            using (var writer = new StreamWriter(path1, false, Encoding.UTF8))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine($"chcp {Encoding.UTF8.CodePage}");
                writer.WriteLine($"call script2.bat");
            }

            var path2 = Path.Combine(baseDirectory, "script2.bat");
            using (var writer = new StreamWriter(path2, false, Encoding.UTF8))
            {
                writer.WriteLine("@echo off");
                var command = @"I:\VIDEO\Blu-ray Ripper\THE ビッグオー\test_convert_S1#1_1.bat";
                writer.WriteLine($"call \"{command}\"");
            }

            return path1;
        }

        private static string ShellExecutableName => Environment.GetEnvironmentVariable("ComSpec") ?? throw new Exception();

        private static string GetShellParameter(string baseDirectory, string intermediateScriptFile)
        {
            if (!intermediateScriptFile.StartsWith(baseDirectory, StringComparison.Ordinal))
                throw new Exception();
            var f = intermediateScriptFile[(baseDirectory.Length + 1)..];
            return $"/k \"{f}\"";
        }
    }
}
