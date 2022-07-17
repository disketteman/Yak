using NUnit.Framework;
using Yak.Attributes;

namespace Yak.Generator.IntegrationTests;

public interface IA
{

}

public class A : IA
{

}

public class B
{
    public IA A { get; }
    public B(IA a)
    {
        A = a;
    }
}

public class C
{
    public IA A { get; }
    public B B { get; }

    public C(IA a, B b)
    {
        A = a;
        B = b;
    }
}

[Container]
public interface IMyModule
{
    [Lifetime.Singleton]
    IA A => new A();

    [Lifetime.Transient]
    B B => new B(A);

    [Lifetime.Scoped]
    C C => new C(A, B);
}

public partial class MyModule: IMyModule
{

}

public class TestYakGenerator
{
    private IMyModule _module;

    [SetUp]
    public void Setup()
    {
        _module = new MyModule();
    }

    [Test]
    public void TestSingletonIsCreatedProperly()
    {
        IA a1 = _module.A;
        Assert.IsNotNull(a1);

        IA a2 = _module.A;
        Assert.IsNotNull(a2);

        Assert.True(object.ReferenceEquals(a1, a2));
    }

    [Test]
    public void TestTransientIsCreatedProperly()
    {
        B b1 = _module.B;
        Assert.IsNotNull(b1);

        B b2 = _module.B;
        Assert.IsNotNull(b2);

        Assert.False(object.ReferenceEquals(b1, b2));
        Assert.True(object.ReferenceEquals(b1.A, b2.A));
    }

    [Test]
    public void TestScopedIsCreatedProperly()
    {
        C c1 = _module.C;
        Assert.IsNotNull(c1);

        C c2 = _module.C;
        Assert.IsNotNull(c2);

        Assert.True(object.ReferenceEquals(c1, c2));
        Assert.True(object.ReferenceEquals(c1.A, c2.A));
        Assert.True(object.ReferenceEquals(c1.B, c2.B));
    }
}
