using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Yak.Generator;

internal static class YakGeneratorHelper
{
    public static (string, string) GenerateContainerClass(ContainerInfo containerInfo)
    {
        StringBuilder stringBuilder = new StringBuilder();
        StringWriter stringWriter = new StringWriter(stringBuilder);

        string baseName = containerInfo.Name;
        // TODO: what about base
        string name = containerInfo.Name.Remove(containerInfo.Name.Length - 4);
        // TODO: what about global namespace?
        string containingNamespace = containerInfo.Namespace ?? "";

        foreach (var usingDirective in containerInfo.Usings)
        {
            usingDirective.WriteTo(stringWriter);
        }

        string s =
$@"namespace {containingNamespace};

#nullable enable
public partial class {name} : {baseName}
{{
    private {name}? _root = null;
";
        stringWriter.Write(s);

        Dictionary<string, string> typeToPropertyNameLookup = new();

        foreach (var registration in containerInfo.Registrations)
        {
            typeToPropertyNameLookup[registration.Type] = registration.Name;
        }

        foreach (var registration in containerInfo.Registrations)
        {
            string stringyfiedRegistration = CreateMember(registration, typeToPropertyNameLookup, stringWriter);
            stringWriter.Write(stringyfiedRegistration);
            stringWriter.WriteLine();
        }

        stringWriter.WriteLine("}");
        stringWriter.WriteLine("#nullable restore");

        return ($"{name}.Generated.cs", stringBuilder.ToString());
    }

    private static string CreateMember(Registration registration, Dictionary<string, string> typeToPropertyNameLookup, StringWriter writer)
    {
        string privateInstanceName = $"_{char.ToLowerInvariant(registration.Name[0])}{registration.Name.Substring(1)}";

        string construction = null;
        ConstructorInfo constructorInfo = registration.ConstructorInfo;
        if (constructorInfo != null)
        {
            
            construction = $"new {constructorInfo.TypeName}(";
            foreach (var param in constructorInfo.ParameterTypes)
            {
                construction = construction + typeToPropertyNameLookup[param];
            }
        }

        if (registration.RegistrationScope == RegistrationScope.Singleton)
        {
            return
$@"
    // singleton
    private {registration.Type}? {privateInstanceName};
    public sealed override {registration.Type} {registration.Name} {{
        get
        {{
            var root = _root ?? this;

            if (root.{privateInstanceName} != null)
            {{
                return root.{privateInstanceName};
            }}

            root.{privateInstanceName} = base.{registration.Name};
            return root.{privateInstanceName};
        }}
    }}
";
        }

        if (registration.RegistrationScope == RegistrationScope.Scoped)
        {
            return
$@"
    // scoped
    private {registration.Type}? {privateInstanceName};
    public sealed override {registration.Type} {registration.Name} {{
        get
        {{
            if ({privateInstanceName} != null)
            {{
                return {privateInstanceName};
            }}

            {privateInstanceName} = base.{registration.Name};
            return {privateInstanceName};
        }}
    }}
";
        }

        return
$@"
    // transient
    public sealed override {registration.Type} {registration.Name} => base.{registration.Name};
";
    }
}
