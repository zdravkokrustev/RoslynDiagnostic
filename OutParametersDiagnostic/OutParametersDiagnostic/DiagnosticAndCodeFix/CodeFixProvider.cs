using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FindSymbols;

namespace DiagnosticAnalyzerAndCodeFix
{
    [ExportCodeFixProvider(DiagnosticAnalyzer.DiagnosticId, LanguageNames.CSharp)]
    public class CodeFixProvider : ICodeFixProvider
    {
        public IEnumerable<string> GetFixableDiagnosticIds()
        {
            return new[] { DiagnosticAnalyzer.DiagnosticId };
        }

        public async Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest

            var diagnosticSpan = diagnostics.First().Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var parameterList = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterListSyntax>().First();

            // Return a code action that will invoke the fix.
            return new[] { CodeAction.Create("Reorder parameters", c => this.ReorderParameters(document, root, parameterList, c)) };
        }

        private async Task<Solution> ReorderParameters(Document document, SyntaxNode documentRoot, ParameterListSyntax parameterList, CancellationToken cancellationToken)
        {
            // Reorder parameters in declaration
            var newParameterList = ReoderDeclarationParameters(parameterList);
            var resultDocumentRoot = documentRoot.ReplaceNode(parameterList, newParameterList);
            var resultDocument = document.WithSyntaxRoot(resultDocumentRoot);

            // Get method references
            var references = await GetReferenceLocationsAsync(resultDocument, parameterList, cancellationToken).ConfigureAwait(false);

            // Reorder parameters in references
            resultDocument = await Formatter.FormatAsync(resultDocument, null, cancellationToken).ConfigureAwait(false);
            var solution = await ProcessReferences(resultDocument, references, cancellationToken).ConfigureAwait(false);

            return solution;
        }

        private async Task<Solution> ProcessReferences(Document document, Dictionary<DocumentId, List<TextSpan>> references, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            foreach (var referencesByDocument in references)
            {
                var referenceDocument = solution.GetDocument(referencesByDocument.Key);
                var documentRoot = await referenceDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var allArgumentsInDocument = new List<ArgumentListSyntax>();
                foreach (var sourceSpan in referencesByDocument.Value)
                {
                    var arguments = documentRoot.FindToken(sourceSpan.Start).Parent.GetFirstParentOfType<InvocationExpressionSyntax>().ArgumentList;
                    allArgumentsInDocument.Add(arguments);
                }

                documentRoot = documentRoot.ReplaceNodes(allArgumentsInDocument, (a, d) => ReoderReferenceParameters(a));
                Document modifiedDocument = referenceDocument.WithSyntaxRoot(documentRoot);
                modifiedDocument = await Formatter.FormatAsync(modifiedDocument, null, cancellationToken).ConfigureAwait(false);

                solution = modifiedDocument.Project.Solution;
            }

            return solution;
        }

        private ParameterListSyntax ReoderDeclarationParameters(ParameterListSyntax parameterList)
        {
            var outParameters = parameterList.Parameters.Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)));
            var otherParameters = parameterList.Parameters.Except(outParameters);

            var reorderedParameters = new List<ParameterSyntax>(otherParameters);
            reorderedParameters.AddRange(outParameters);

            return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(reorderedParameters));
        }

        private ArgumentListSyntax ReoderReferenceParameters(ArgumentListSyntax argumentList)
        {
            var outArguments = argumentList.Arguments.Where(a => a.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword));
            var otherArguments = argumentList.Arguments.Except(outArguments);

            var reorderedArguments = new List<ArgumentSyntax>(otherArguments);
            reorderedArguments.AddRange(outArguments);

            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(reorderedArguments));
        }

        private async Task<Dictionary<DocumentId, List<TextSpan>>> GetReferenceLocationsAsync(Document document, ParameterListSyntax parameterList, CancellationToken cancellationToken)
        {
            var documentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var method = documentRoot.FindToken(parameterList.SpanStart).Parent.Parent as MethodDeclarationSyntax;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);      
            var methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);

            var solution = document.Project.Solution;
            var references = await SymbolFinder.FindCallersAsync(methodSymbol, solution, cancellationToken).ConfigureAwait(false);
            var result = new Dictionary<DocumentId, List<TextSpan>>();

            foreach (var reference in references)
            {
                var documentId = solution.GetDocumentId(reference.Locations.First().SourceTree);

                if (result.ContainsKey(documentId))
                {
                    var locations = result[documentId];
                    locations.AddRange(reference.Locations.Select(l => l.SourceSpan));
                }
                else
                {
                    var locations = new List<TextSpan>();
                    locations.AddRange(reference.Locations.Select(l => l.SourceSpan));

                    result.Add(documentId, locations);
                }
            }

            return result;
        }
    }
}