namespace Yak.Generator;

internal sealed class PropertyInfo
{
    public string Type { get; }
    public string Name { get; }
    public RegistrationScope RegistrationScope { get; }
    public ConstructorInfo? ConstructorInfo { get; }

    public PropertyInfo(string type, string name, RegistrationScope registrationScope, ConstructorInfo? constructorInfo)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        RegistrationScope = registrationScope;
        ConstructorInfo = constructorInfo;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;

        return obj is PropertyInfo other &&
// not really sure why the warning is raised, EqualityComparer<ConstructorInfo>.Default.Equals accepts nullables
#pragma warning disable CS8604
               EqualityComparer<ConstructorInfo>.Default.Equals(ConstructorInfo, other.ConstructorInfo) &&
#pragma warning restore CS8604
               Type == other.Type && Name == other.Name && RegistrationScope == other.RegistrationScope;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Type.GetHashCode();
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)RegistrationScope;
            hashCode = (hashCode * 397) ^ (ConstructorInfo != null ? ConstructorInfo.GetHashCode() : 0);
            return hashCode;
        }
    }
}
