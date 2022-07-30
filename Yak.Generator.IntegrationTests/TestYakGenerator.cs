using NUnit.Framework;
using Yak;
using Yak.Generator.IntegrationTests.Dependency;

namespace Yak.Generator.IntegrationTests;

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

public abstract class MyContainerBase : ContainerBase
{
    [Singleton]
    public virtual IA A => Construct<A>();

    [Transient]
    public virtual B B => Construct<B>();

    [Scoped]
    public virtual C C => Construct<C>();
}

public partial class MyContainer: MyContainerBase
{

}

public class TestYakGenerator
{
    private MyContainerBase _container;

    [SetUp]
    public void Setup()
    {
        _container = new MyContainer();
    }

    [Test]
    public void TestSingletonIsCreatedProperly()
    {
        IA a1 = _container.A;
        Assert.IsNotNull(a1);

        IA a2 = _container.A;
        Assert.IsNotNull(a2);

        Assert.True(object.ReferenceEquals(a1, a2));
    }

    [Test]
    public void TestTransientIsCreatedProperly()
    {
        B b1 = _container.B;
        Assert.IsNotNull(b1);

        B b2 = _container.B;
        Assert.IsNotNull(b2);

        Assert.False(object.ReferenceEquals(b1, b2));
        Assert.True(object.ReferenceEquals(b1.A, b2.A));
    }

    [Test]
    public void TestScopedIsCreatedProperly()
    {
        C c1 = _container.C;
        Assert.IsNotNull(c1);

        C c2 = _container.C;
        Assert.IsNotNull(c2);

        Assert.True(object.ReferenceEquals(c1, c2));
        Assert.True(object.ReferenceEquals(c1.A, c2.A));
        Assert.True(object.ReferenceEquals(c1.B, c2.B));
    }
}
