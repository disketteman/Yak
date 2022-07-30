namespace Yak;

public interface IContainer
{
    T Construct<T>() => throw new InvalidOperationException();
}
