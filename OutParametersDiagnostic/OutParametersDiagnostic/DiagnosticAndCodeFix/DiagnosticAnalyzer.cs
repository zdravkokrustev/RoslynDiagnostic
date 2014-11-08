using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DiagnosticAnalyzerAndCodeFix
{
    // TODO: Consider implementing other interfaces that implement IDiagnosticAnalyzer instead of or in addition to ISymbolAnalyzer

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiagnosticAnalyzer : ISyntaxNodeAnalyzer<SyntaxKind>
    {
        public const string DiagnosticId = "OutParameterDiagnostic";
        internal const string Description = "Reorder out parameters";
        internal const string MessageFormat = "Out parameter '{0}' should be placed after all in and ref parameters.";
        internal const string Category = "Convention";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Description, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {
                return ImmutableArray.Create(SyntaxKind.ParameterList);
            }
        }

        public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var parameterList = (ParameterListSyntax)node;

            var outParameters = new Queue<ParameterSyntax>();
            foreach (var parameter in parameterList.Parameters)
            {
                if (parameter.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)))
                {
                    outParameters.Enqueue(parameter);
                }
                else
                {
                    while (outParameters.Count > 0)
                    {
                        ParameterSyntax outParameter = outParameters.Dequeue();
                        addDiagnostic(Diagnostic.Create(Rule, outParameter.GetLocation(), outParameter.Identifier));
                    }
                }
            }
        }
    }
}
