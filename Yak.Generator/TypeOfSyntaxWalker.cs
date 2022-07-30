using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Yak.Generator;

// https://stackoverflow.com/a/47339547/271495
public class TypeOfSyntaxWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;

    public ISymbol? SymbolInfoSymbol { get; private set; }
    internal List<Registration> Registrations = new List<Registration>();
    private Registration _currentRegistration;

    public TypeOfSyntaxWalker( SemanticModel semanticModel )
    {
        _semanticModel = semanticModel;
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        base.VisitInvocationExpression(node);
    }

    public override void VisitTypeOfExpression(TypeOfExpressionSyntax typeOfExpressionSyntax)
    {
        var parent = typeOfExpressionSyntax.Parent;
        if (parent.IsKind(SyntaxKind.ReturnStatement))
        {
            var propertyDeclarationSyntax = parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (propertyDeclarationSyntax != null)
            {
                //propertyDeclarationSyntax.Identifier.Value
            }
        }
        base.VisitTypeOfExpression(typeOfExpressionSyntax);
    }
}