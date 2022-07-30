namespace Yak;

public abstract class ContainerBase
{
    protected T Construct<T>() => throw new InvalidOperationException();
}
