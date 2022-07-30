using System;
using System.Collections.Generic;
using System.Text;

namespace Yak.Generator
{
    internal class ConstructorInfo
    {
        public string? TypeName { get; set; }
        public List<string> ParameterTypes { get; set; }
    }
}
