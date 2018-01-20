using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RegexAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegexAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId1 = "StaticRegexMatchAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title1 = new LocalizableResourceString(nameof(Resources.StaticRegexMatchTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat1 = new LocalizableResourceString(nameof(Resources.StaticRegexMatchMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description1 = new LocalizableResourceString(nameof(Resources.StaticRegexMatchDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category1 = "Localization";

        public const string DiagnosticId2 = "RegexCreationAnalyzer";

        private static readonly LocalizableString Title2 = new LocalizableResourceString(nameof(Resources.RegexCreationTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat2 = new LocalizableResourceString(nameof(Resources.RegexCreationMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description2 = new LocalizableResourceString(nameof(Resources.RegexCreationDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category2 = "Localization";

        private static DiagnosticDescriptor StaticRegexMatchRule = new DiagnosticDescriptor(DiagnosticId1, Title1, MessageFormat1, Category1, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description1);
        private static DiagnosticDescriptor RegexCreationRule = new DiagnosticDescriptor(DiagnosticId2, Title1, MessageFormat2, Category2, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description1);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(StaticRegexMatchRule, RegexCreationRule); } }

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (node.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocationExpr = (InvocationExpressionSyntax)context.Node;
                var memberAccessExpr = invocationExpr.Expression as MemberAccessExpressionSyntax;
                if (memberAccessExpr == null)
                    return;
                if (!memberAccessExpr.Name.ToString().Equals("Match") && !memberAccessExpr.Name.ToString().Equals("Matches") && !memberAccessExpr.Name.ToString().Equals("IsMatch"))
                    return;
                var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
                if (!memberSymbol.IsStatic)
                    return;
                if (!memberSymbol?.ToString().StartsWith("System.Text.RegularExpressions.Regex") ?? true)
                    return;
                var argumentList = invocationExpr.ArgumentList as ArgumentListSyntax;
                if (!argumentList.ToString().Contains("RegexOptions.CultureInvariant"))
                {
                    var diagnostic = Diagnostic.Create(StaticRegexMatchRule, invocationExpr.GetLocation(), StaticRegexMatchRule.Description);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (node.IsKind(SyntaxKind.ObjectCreationExpression))
            {
                var objectCreationExpr = (ObjectCreationExpressionSyntax)context.Node;
                if (objectCreationExpr?.Type.ToString() != "Regex")
                    return;
                var memberSymbol = context.SemanticModel.GetSymbolInfo(objectCreationExpr).Symbol as IMethodSymbol;
                if (!memberSymbol?.ToString().StartsWith("System.Text.RegularExpressions.Regex") ?? true)
                    return;
                var argumentList = objectCreationExpr.ArgumentList as ArgumentListSyntax;
                if (!argumentList.ToString().Contains("RegexOptions.CultureInvariant"))
                {
                    var diagnostic = Diagnostic.Create(RegexCreationRule, objectCreationExpr.GetLocation(), RegexCreationRule.Description);
                    context.ReportDiagnostic(diagnostic);
                }
            }       
        }
    }
}
