using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yak.Generator;

internal enum RegistrationScope
{
    Transient, Scoped, Singleton
}

internal sealed class ContainerInfo
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

internal class ContainerInfoComparer: IEqualityComparer<ContainerInfo?>
{
    private readonly RegistrationComparer _registrationComparer;

    public ContainerInfoComparer(RegistrationComparer registrationComparer)
    {
        _registrationComparer = registrationComparer;
    }
    public bool Equals(ContainerInfo? x, ContainerInfo? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;

        foreach ((Registration regX, Registration regY) in x.Registrations.Zip(y.Registrations,
                     (regX, regY) => (regX, regY)))
        {
            if (!_registrationComparer.Equals(regX, regY))
            {
                return false;
            }
        }
        
        return x.Usings.Equals(y.Usings) && x.Namespace == y.Namespace && x.Name == y.Name && x.Registrations.Equals(y.Registrations);
    }

    public int GetHashCode(ContainerInfo? obj)
    {
        if (obj == null)
        {
            return 0;
        }
        unchecked
        {
            var hashCode = obj.Usings.GetHashCode();
            hashCode = (hashCode * 397) ^ (obj.Namespace != null ? obj.Namespace.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ obj.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Registrations.GetHashCode();
            return hashCode;
        }
    }
}
