namespace ManagedCode.Orleans.RateLimiting.Core.Attributes;

public interface ILimiterAttribute<T>
{
    public string? Key { get; }
    public KeyType KeyType { get; }
    public T? Options { get; }
}