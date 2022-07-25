using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Yak.Generator;

public static class GeneratorExtensions
{
    public static T? FindFirstParent<T>(this SyntaxNode node) where T : SyntaxNode
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

    public static IEnumerable<AttributeSyntax> AllAttributes(this MemberDeclarationSyntax memberDeclarationSyntax)
    {
        foreach (AttributeListSyntax attributeListSyntax in memberDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                yield return attributeSyntax;
            }
        }
    }
}

