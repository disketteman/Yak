using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Yak;

[Generator]
public class YakGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        /*
        #if DEBUG
                if (!Debugger.IsAttached)
                {
                    Debugger.Launch();
                }
        #endif
        */
        Debug.WriteLine("Initalize code generator");

        /*// Add the marker attribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "EnumExtensionsAttribute.g.cs",
            SourceText.From("//", Encoding.UTF8)));
        */

        // Do a simple filter for enums
        IncrementalValuesProvider<ContainerInfo> enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsSyntaxTarget, // select enums with attributes
                transform: Transform) // sect the enum with the [EnumExtensions] attribute
            .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

        // Combine the selected enums with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<ContainerInfo>)> compilationAndEnums
            = context.CompilationProvider.Combine(enumDeclarations.Collect());

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(compilationAndEnums,
            (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static bool IsCandidate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is CompilationUnitSyntax compilationUnitSyntax)
        {
            foreach (var usingDirectiveSyntax in compilationUnitSyntax.Usings)
            {
                //usingDirectiveSyntax.Using
            }
        }
        return false;
    }

    private static InterfaceDeclarationSyntax? FindModuleInterface(CompilationUnitSyntax node)
    {
        InterfaceDeclarationSyntax? interfaceDeclarationSyntax;
        foreach (var child in node.ChildNodes())
        {
            if ((interfaceDeclarationSyntax = child as InterfaceDeclarationSyntax) != null)
            {
                if (IsModuleInterface(interfaceDeclarationSyntax))
                {
                    return interfaceDeclarationSyntax;
                }
            }
            else if ((interfaceDeclarationSyntax = FindModuleInterface(node)) != null)
            {
                return interfaceDeclarationSyntax;
            }
        }
        return null;
    }

    private static bool IsModuleInterface(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
    {
        foreach (var attributeList in interfaceDeclarationSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                (string name, int arity) = GetIdentifier(attribute.Name);

                if (name == "Container")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsSyntaxTarget(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {

            foreach (var attributeList in interfaceDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    (string name, int arity) = GetIdentifier(attribute.Name);

                    if (name == "Container")
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static T? FindFirstParent<T>(SyntaxNode node) where T : SyntaxNode
    {
        SyntaxNode? evaluated = node.Parent;
        while (evaluated != null)
        {
            if (evaluated is T instance)
            {
                return instance;
            }
            evaluated = evaluated.Parent;
        }
        return null;
    }

    static ContainerInfo? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        InterfaceDeclarationSyntax interfaceDeclarationSyntax = (InterfaceDeclarationSyntax)context.Node;

        CompilationUnitSyntax? compilationUnitSyntax = FindFirstParent<CompilationUnitSyntax>(context.Node);

        // TODO: Is it even possible to not have any parent compilation unit?
        if (compilationUnitSyntax == null)
        {
            return null;
        }

        ContainerInfo containerInfo = new();
        // this doesn't work
        //TypeInfo interfaceType = context.SemanticModel.GetTypeInfo(interfaceDeclarationSyntax);

        var interfaceDeclaredSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

        if (interfaceDeclaredSymbol == null)
        {
            return null;
        }

        //var containingModule = interfaceDeclaredSymbol.;
        //var x = containingModule.Usin

        containerInfo.Name = interfaceDeclarationSyntax.Identifier;
        containerInfo.Namespace = interfaceDeclaredSymbol.ContainingNamespace;
        containerInfo.Usings = compilationUnitSyntax.Usings;

        foreach (MemberDeclarationSyntax member in interfaceDeclarationSyntax.Members)
        {

            RegistrationScope? scope = null;
            foreach (AttributeListSyntax attributeListSyntax in member.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

                    if (symbol == null)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = symbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName.StartsWith("Yak.Attributes.Lifetime."))
                    {
                        scope = DetermineRegistrationScope(fullName);
                    }
                }
            }

            if (!scope.HasValue)
            {
                continue;
            }

            PropertyDeclarationSyntax propertyDeclarationSyntax = (PropertyDeclarationSyntax)member;
            //propertyDeclarationSyntax.ExpressionBody.Expression.
            string name = propertyDeclarationSyntax.Identifier.ValueText;

            IdentifierNameSyntax identifierNameSyntax = (IdentifierNameSyntax)propertyDeclarationSyntax.Type;
            string type = identifierNameSyntax.Identifier.ValueText;

            Registration registration = new Registration
            {
                RegistrationScope = scope.Value,
                Type = type,
                Name = name,
                PropertyDeclarationSyntax = propertyDeclarationSyntax
            };
            containerInfo.Registrations.Add(registration);
        }

        return containerInfo;
    }

    private static RegistrationScope DetermineRegistrationScope(string name)
    {
        switch (name)
        {
            case "Yak.Attributes.Lifetime.SingletonAttribute":
                return RegistrationScope.Singleton;
            case "Yak.Attributes.Lifetime.ScopedAttribute":
                return RegistrationScope.Scoped;
            default:
                return RegistrationScope.Transient;
        }
    }

    void Execute(Compilation compilation, ImmutableArray<ContainerInfo> containerInfos, SourceProductionContext context)
    {
        foreach (var containerInfo in containerInfos)
        {
            (string filename, string content) = MyGeneratorHelper.GenerateContainerClass(containerInfo);
            context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
        }

    }

    // copied from StrongInject
    private static (string identifier, int arity) GetIdentifier(NameSyntax name)
    {
        var simpleName = name switch
        {
            SimpleNameSyntax s => s,
            AliasQualifiedNameSyntax { Name: var s } => s,
            QualifiedNameSyntax { Right: var s } => s,
            var other => throw new NotImplementedException(other.GetType().ToString())
        };

        return (simpleName.Identifier.Text, simpleName.Arity);
    }
}