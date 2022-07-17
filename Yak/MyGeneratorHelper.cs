using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Yak;

internal static class MyGeneratorHelper
{
    public static (string, string) GenerateContainerClass(ContainerInfo containerInfo)
    {
        StringBuilder stringBuilder = new StringBuilder();
        StringWriter stringWriter = new StringWriter(stringBuilder);

        string interfaceName = containerInfo.Name.ValueText;
        string name = containerInfo.Name.ValueText.Substring(1);
        string containingNamespace = containerInfo.Namespace?.ToDisplayString() ?? "";

        foreach (var usingDirective in containerInfo.Usings)
        {
            usingDirective.WriteTo(stringWriter);
        }

        string s =
$@"namespace {containingNamespace};

#nullable enable
public partial class {name} : {interfaceName}
{{
    private {name}? _root = null;
";
        stringWriter.Write(s);

        foreach (var registration in containerInfo.Registrations)
        {
            string stringyfiedRegistration = CreateMember(containerInfo, registration, stringWriter);
            stringWriter.Write(stringyfiedRegistration);
            stringWriter.WriteLine();
        }

        stringWriter.WriteLine("}");
        stringWriter.WriteLine("#nullable restore");

        return ($"{name}.Generated.cs", stringBuilder.ToString());
    }

    private static string CreateMember(ContainerInfo containerInfo, Registration registration, StringWriter writer)
    {
        string privateInstanceName = $"_{char.ToLowerInvariant(registration.Name[0])}{registration.Name.Substring(1)}";
        string expression = registration.PropertyDeclarationSyntax.ExpressionBody.ToFullString();

        if (registration.RegistrationScope == RegistrationScope.Singleton)
        {
            return
$@"
    // singleton
    private {registration.Type} Provide{registration.Name}() {expression};
    private {registration.Type} {privateInstanceName};
    public {registration.Type} {registration.Name} {{
        get
        {{
            var root = _root ?? this;

            if (root.{privateInstanceName} != null)
            {{
                return root.{privateInstanceName};
            }}

            root.{privateInstanceName} = Provide{registration.Name}();
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
    private {registration.Type} Provide{registration.Name}() {expression};
    private {registration.Type} {privateInstanceName};
    public {registration.Type} {registration.Name} {{
        get
        {{
            if ({privateInstanceName} != null)
            {{
                return {privateInstanceName};
            }}

            {privateInstanceName} = Provide{registration.Name}();
            return {privateInstanceName};
        }}
    }}
";
        }

        return
$@"
    // transient
    public {registration.Type} {registration.Name} {expression};
";
    }
}
