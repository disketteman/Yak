using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Yak.Generator;

internal enum RegistrationScope
{
    Transient, Scoped, Singleton
}

internal class Registration
{
    public string Type { get; set; }
    public string Name { get; set; }
    public RegistrationScope RegistrationScope { get; set; }
    public PropertyDeclarationSyntax PropertyDeclarationSyntax { get; set; }
};

internal class ContainerInfo
{
    public SyntaxList<UsingDirectiveSyntax> Usings { get; set; }
    public INamespaceSymbol? Namespace { get; set; }
    public SyntaxToken Name { get; set; }
    public List<Registration> Registrations { get; set; } = new();
}
