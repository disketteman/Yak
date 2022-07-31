using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    public List<PropertyInfo> Registrations { get; }

    public ContainerInfo(SyntaxList<UsingDirectiveSyntax> usings, string? @namespace, string name)
    {
        Usings = usings;
        Namespace = @namespace;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Registrations = new List<PropertyInfo>();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;

        return obj is ContainerInfo other && Usings.SequenceEqual(other.Usings) && Namespace == other.Namespace &&
               Name == other.Name && Registrations.SequenceEqual(other.Registrations);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Usings.GetHashCode();
            hashCode = (hashCode * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ Registrations.GetHashCode();
            return hashCode;
        }
    }
}
