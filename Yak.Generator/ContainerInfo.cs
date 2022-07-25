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
    public string Type { get; }
    public string Name { get; }
    public RegistrationScope RegistrationScope { get; }
    public string StringifiedExpression { get; }

    public Registration(string type, string name, RegistrationScope registrationScope, string stringifiedExpression)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        RegistrationScope = registrationScope;
        StringifiedExpression = stringifiedExpression ?? throw new ArgumentNullException(nameof(stringifiedExpression));
    }
}
internal class ContainerInfo
{
    public SyntaxList<UsingDirectiveSyntax> Usings { get; }
    public string? Namespace { get; }
    public string Name { get; }
    public List<Registration> Registrations { get; }

    public ContainerInfo(SyntaxList<UsingDirectiveSyntax> usings, string? @namespace, string name)
    {
        Usings = usings;
        Namespace = @namespace;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Registrations = new List<Registration>();
    }
}
