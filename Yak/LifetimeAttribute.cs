using System;
using System.Collections.Generic;
using System.Text;

namespace Yak;

public static class Lifetime
{
    public class SingletonAttribute : Attribute
    {

    }

    public class ScopedAttribute : Attribute
    {

    }

    public class TransientAttribute : Attribute
    {

    }
}
