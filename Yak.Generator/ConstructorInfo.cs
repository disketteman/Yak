namespace Yak.Generator
{
    internal sealed class ConstructorInfo
    {
        public string? TypeName { get; }
        public ImmutableArray<string> ParameterTypes { get; }
        
        public ConstructorInfo(string? typeName, ImmutableArray<string> parameterTypes)
        {
            TypeName = typeName;
            ParameterTypes = parameterTypes;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            return obj is ConstructorInfo other && TypeName == other.TypeName &&
                   ParameterTypes.SequenceEqual(other.ParameterTypes);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((TypeName != null ? TypeName.GetHashCode() : 0) * 397) ^ ParameterTypes.GetHashCode();
            }
        }
    }
}
