using System;
using System.Collections.Generic;

namespace Yak.Generator;

internal sealed class Registration
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

internal class RegistrationComparer : IEqualityComparer<Registration>
{
    public bool Equals(Registration x, Registration y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;

        return x.Type == y.Type && x.Name == y.Name && x.RegistrationScope == y.RegistrationScope && x.StringifiedExpression == y.StringifiedExpression;
    }

    public int GetHashCode(Registration obj)
    {
        unchecked
        {
            var hashCode = obj.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)obj.RegistrationScope;
            hashCode = (hashCode * 397) ^ obj.StringifiedExpression.GetHashCode();
            return hashCode;
        }
    }
}