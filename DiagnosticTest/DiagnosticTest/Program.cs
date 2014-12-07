using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticTest
{
    public class Program
    {
        public static void Method(int a, out int x, out int y, int b)
        {
            x = a;
            y = b;
        }

        public static void Main(string[] args)
        {
            int x;
            int y;

            Method(5, out x, out y, 10);

            Console.WriteLine("x = {0}, y = {1}", x, y);
        }
    }
}
