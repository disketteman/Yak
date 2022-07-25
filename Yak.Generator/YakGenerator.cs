using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Yak.Generator;

[Generator]
public class YakGenerator : IIncrementalGenerator
{
    private const string YakAttributesPrefix = "Yak.";
    private const string YakSingletonAttribute = "Yak.SingletonAttribute";
    private const string YakScopedAttribute = "Yak.ScopedAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
// This is the only meaningful way to debug Source Generators.
// It's not by default, so it's not triggered for every debugging session.
#if ATTACH_DEBUGGER
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif

        IncrementalValuesProvider<ContainerInfo> containers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsInterfaceInheritingIContainer,
                transform: Transform)
            .Where(static m => m is not null)!;

        context.RegisterSourceOutput(containers, Execute);
    }

    private static bool IsInterfaceInheritingIContainer(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            if (interfaceDeclarationSyntax.BaseList == null)
            {
                return false;
            }

            foreach (var type in interfaceDeclarationSyntax.BaseList.Types)
            {
                if (type.ToString() == "IContainer")
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    static ContainerInfo? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        // it's pretty safe to assume it's InterfaceDeclarationSyntax, because only that type is prefiltered by IsInterfaceInheritingIContainer
        InterfaceDeclarationSyntax interfaceDeclarationSyntax = (InterfaceDeclarationSyntax)context.Node;

        CompilationUnitSyntax? compilationUnitSyntax = context.Node.FindFirstParent<CompilationUnitSyntax>();

        // TODO: Is it even possible to not have any parent compilation unit?
        if (compilationUnitSyntax == null)
        {
            return null;
        }

        // TODO: SemanticModel is needed only for ContainingNamespace, perhaps we can easily figure that out via SyntaxNode?
        var interfaceDeclaredSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

        if (interfaceDeclaredSymbol == null)
        {
            return null;
        }

        ContainerInfo containerInfo = new ContainerInfo(
            compilationUnitSyntax.Usings,
            interfaceDeclaredSymbol.ContainingNamespace.ToDisplayString(),
            interfaceDeclarationSyntax.Identifier.ToString()
        );

        foreach (MemberDeclarationSyntax member in interfaceDeclarationSyntax.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RegistrationScope? scope = null;
            foreach (AttributeSyntax attributeSyntax in member.AllAttributes())
            {
                // TODO: Perhaps the semantic model could be ignored and scope could be figured out just from the name
                var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

                if (symbol == null)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = symbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName.StartsWith(YakAttributesPrefix))
                {
                    scope = DetermineRegistrationScope(fullName);
                }
            }

            if (!scope.HasValue)
            {
                continue;
            }

            PropertyDeclarationSyntax propertyDeclarationSyntax = (PropertyDeclarationSyntax)member;
            string name = propertyDeclarationSyntax.Identifier.ValueText;

            IdentifierNameSyntax identifierNameSyntax = (IdentifierNameSyntax)propertyDeclarationSyntax.Type;

            // TODO: report an error on missing ExpressionBody 
            if (propertyDeclarationSyntax.ExpressionBody == null)
            {
                continue;
            }

            string stringifiedExpression = propertyDeclarationSyntax.ExpressionBody.ToFullString();
            string type = identifierNameSyntax.Identifier.ValueText;

            Registration registration = new Registration(type, name, scope.Value, stringifiedExpression);
            containerInfo.Registrations.Add(registration);
        }

        return containerInfo;
    }

    private static RegistrationScope DetermineRegistrationScope(string name)
    {
        switch (name)
        {
            case YakSingletonAttribute:
                return RegistrationScope.Singleton;
            case YakScopedAttribute:
                return RegistrationScope.Scoped;
            default:
                return RegistrationScope.Transient;
        }
    }

    void Execute(SourceProductionContext context, ContainerInfo containerInfo)
    {
        (string filename, string content) = MyGeneratorHelper.GenerateContainerClass(containerInfo);
        context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
    }
}