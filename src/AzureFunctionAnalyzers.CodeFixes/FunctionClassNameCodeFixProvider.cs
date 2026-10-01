using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace AzureFunctionAnalyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FunctionClassNameCodeFixProvider))]
[Shared]
public sealed class FunctionClassNameCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [FunctionClassNameAnalyzer.FunctionClassNameMismatch.Id];

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var classDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (classDeclaration is null)
        {
            return;
        }

        if (!diagnostic.Properties.TryGetValue("ExpectedClassName", out var newName) || newName is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename class to '{newName}'",
                createChangedSolution: cancellationToken => RenameClassAsync(context.Document, classDeclaration, newName, cancellationToken),
                equivalenceKey: nameof(FunctionClassNameCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Solution> RenameClassAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        string newName,
        System.Threading.CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var classSymbol = semanticModel?.GetDeclaredSymbol(classDeclaration, cancellationToken);
        if (classSymbol is null)
        {
            return document.Project.Solution;
        }

        return await Renamer.RenameSymbolAsync(
            document.Project.Solution,
            classSymbol,
            new SymbolRenameOptions(),
            newName,
            cancellationToken).ConfigureAwait(false);
    }
}
