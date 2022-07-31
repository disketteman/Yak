using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

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
        if (node is ClassDeclarationSyntax classDeclarationSyntax)
        {
            if (classDeclarationSyntax.BaseList == null)
            {
                return false;
            }

            foreach (var type in classDeclarationSyntax.BaseList.Types)
            {
                if (type.ToString() == "ContainerBase")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static IdentifierNameSyntax? IdentifyConstructType(PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        GenericNameSyntax? invocationExpressionSyntax = propertyDeclarationSyntax
            // => Create<X>()
            .FirstDescendant<ArrowExpressionClauseSyntax>()
            // Create<X>()
            .FirstDescendant<InvocationExpressionSyntax>()
            // Create<X>
            .FirstDescendant<GenericNameSyntax>();

        string? genericFunctionCallName = invocationExpressionSyntax?.Identifier.ValueText;

        // TODO: how to make sure it's really ContainerBase.Create?
        if (genericFunctionCallName != "Create")
        {
            return null;
        }

        IdentifierNameSyntax? identifierNameSyntax = invocationExpressionSyntax
            // <X>
            .FirstDescendant<TypeArgumentListSyntax>()
            // X
            .FirstDescendant<IdentifierNameSyntax>();

        return identifierNameSyntax;
    }

    private static ConstructorInfo? PrepareConstructorInfo(SemanticModel semanticModel, PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        // TODO: validate it's virtual
        IdentifierNameSyntax? constructIdentifierNameSyntax = IdentifyConstructType(propertyDeclarationSyntax);

        if (constructIdentifierNameSyntax == null)
        {
            return null;
        }

        TypeInfo typeInfo = semanticModel.GetTypeInfo(constructIdentifierNameSyntax);

        if (typeInfo.Type is not INamedTypeSymbol constructTypeSymbol)
        {
            return null;
        }
        string constructedTypeName = constructTypeSymbol.ToString();

        IMethodSymbol constructorSymbol = constructTypeSymbol.Constructors[0];

        ImmutableArray<string> parameters = constructorSymbol.Parameters.Select(param => param.Type.ToString()).ToImmutableArray();
        
        return new ConstructorInfo(constructedTypeName, parameters);
    }
    
    private static ContainerInfo? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        // it's pretty safe to assume it's InterfaceDeclarationSyntax, because only that type is prefiltered by IsInterfaceInheritingIContainer
        ClassDeclarationSyntax classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        CompilationUnitSyntax? compilationUnitSyntax = context.Node.FindFirstParent<CompilationUnitSyntax>();

        // TODO: Is it even possible to not have any parent compilation unit?
        if (compilationUnitSyntax == null)
        {
            return null;
        }

        // TODO: SemanticModel is needed only for ContainingNamespace, perhaps we can easily figure that out via SyntaxNode?
        var classDeclaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (classDeclaredSymbol == null)
        {
            return null;
        }

        ContainerInfo containerInfo = new ContainerInfo(
            compilationUnitSyntax.Usings,
            classDeclaredSymbol.ContainingNamespace.ToDisplayString(),
            classDeclarationSyntax.Identifier.ToString()
        );

        foreach (MemberDeclarationSyntax member in classDeclarationSyntax.Members)
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
            
            var someSymbol = context.SemanticModel.GetDeclaredSymbol(member);
            
            if (someSymbol is not IPropertySymbol propertySymbol)
            {
                continue;
            }

            string type = propertySymbol.Type.ToString();
            string name = propertySymbol.Name;

            PropertyDeclarationSyntax propertyDeclarationSyntax = (PropertyDeclarationSyntax)member;
            ConstructorInfo? constructorInfo = PrepareConstructorInfo(context.SemanticModel, propertyDeclarationSyntax);

            //string name = propertyDeclarationSyntax.Identifier.ValueText;

            //IdentifierNameSyntax identifierNameSyntax = (IdentifierNameSyntax)propertyDeclarationSyntax.Type;
            //string type = identifierNameSyntax.Identifier.ValueText;

            PropertyInfo propertyInfo = new PropertyInfo(type, name, scope.Value, constructorInfo);
            containerInfo.Registrations.Add(propertyInfo);
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
        (string filename, string content) = YakGeneratorHelper.GenerateContainerClass(containerInfo);
        context.AddSource(filename, SourceText.From(content, Encoding.UTF8));
    }
}