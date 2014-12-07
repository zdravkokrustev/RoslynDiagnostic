using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticTest
{
    class Class1
    {
        public static void Method2()
        {
            int x;
            int y;

            Program.Method(5, out x, out y, 10);
        }
    }
}
