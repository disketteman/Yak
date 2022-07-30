namespace Yak;

public abstract class ContainerBase
{
    protected T Create<T>() => throw new InvalidOperationException();
}
