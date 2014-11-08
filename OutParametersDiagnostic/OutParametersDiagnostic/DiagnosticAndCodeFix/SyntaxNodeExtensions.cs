using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticAnalyzerAndCodeFix
{
    public static class SyntaxNodeExtensions
    {
        public static T GetFirstParentOfType<T>(this SyntaxNode node)
            where T : SyntaxNode
        {
            if (node.GetType() == typeof(T))
            {
                return (T)node;
            }
            else
            {
                return node.Parent.GetFirstParentOfType<T>();
            }
        }
    }
}
