using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OutParametersDiagnostic
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OutParametersDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "OutParameterDiagnostic";
        internal const string Title = "Reorder out parameters";
        internal const string MessageFormat = "Out parameter '{0}' should be placed after all in and ref parameters.";
        internal const string Category = "Convention";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.ParameterList);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var parameterList = (ParameterListSyntax)context.Node;

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

                        context.ReportDiagnostic(Diagnostic.Create(Rule, outParameter.GetLocation(), outParameter.Identifier));
                    }
                }
            }
        }
    }
}
