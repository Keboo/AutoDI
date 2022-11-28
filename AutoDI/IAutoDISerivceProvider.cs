namespace AutoDI;

public interface IAutoDISerivceProvider
{
    object? GetService(Type serviceType, object[] parameters);
}