using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;

namespace Experiment
{
    internal static class Program
    {
        static Program()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static void Main()
        {
            {
                var d1 = DateTime.MinValue;
                var d2 = d1.ToUniversalTime();
                var d3 = d1.ToLocalTime();
                Console.WriteLine($"{d1.ToString(CultureInfo.InvariantCulture)} {d1.Kind}");
                Console.WriteLine(d2);
                Console.WriteLine(d3);
                Console.WriteLine(d1.Ticks);
                Console.WriteLine(d2.Ticks);
                Console.WriteLine(d3.Ticks);
            }

            Console.WriteLine("---");

            {
                var d1 = new DateTime(0, DateTimeKind.Utc);
                var d2 = d1.ToUniversalTime();
                var d3 = d1.ToLocalTime();
                Console.WriteLine($"{d1.ToString(CultureInfo.InvariantCulture)} {d1.Kind}");
                Console.WriteLine(d2);
                Console.WriteLine(d3);
                Console.WriteLine(d1.Ticks);
                Console.WriteLine(d2.Ticks);
                Console.WriteLine(d3.Ticks);
            }

            Console.Beep();
            Console.WriteLine("Complete");
            _=Console.ReadLine();
        }
    }
}
