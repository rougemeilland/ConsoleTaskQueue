using System.Text.RegularExpressions;

namespace ConsoleTasks
{
    internal static partial class StringExtensions
    {
        public static string PowerShellCommandLineArgumentEncode(this string arg)
        {
            return
                GetNeedQuotingPattern().IsMatch(arg)
                ? $"'{Encode(arg)}'"
                : Encode(arg);

            static string Encode(string s)
            {
                return GetPowerShellSpecialCharactersPattern().Replace(s, m => $"`{m.Value}");
            }
        }

        [GeneratedRegex(@"[ @""&$*%?()-]", RegexOptions.Compiled)]
        private static partial Regex GetNeedQuotingPattern();

        [GeneratedRegex(@"[`\t\n\r\0\a\f\v']|\r\n", RegexOptions.Compiled)]
        private static partial Regex GetPowerShellSpecialCharactersPattern();
    }
}
