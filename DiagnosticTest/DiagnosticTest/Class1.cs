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
            Program.Method(5, out var x, out var y, 10);
        }
    }
}
