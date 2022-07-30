namespace Yak.Generator.IntegrationTests.Dependency;

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