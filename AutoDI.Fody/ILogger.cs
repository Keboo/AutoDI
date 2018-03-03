namespace AutoDI.Fody
{
    public interface ILogger
    {
        void Error(string message);
        void Warning(string message);
    }
}