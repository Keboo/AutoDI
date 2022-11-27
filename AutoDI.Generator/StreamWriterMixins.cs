
namespace AutoDI.Generator
{
    public static class StreamWriterMixins
    {
        public static void WriteLine(this StreamWriter writer, int level, string line)
        {
            writer.WriteLine("{0}{1}", level <= 0 ? "" : new string(' ', level * 4), line);
        }

        public static void Write(this StreamWriter writer, int level, string line)
        {
            writer.Write("{0}{1}", level <= 0 ? "" : new string(' ', level * 4), line);
        }
    }
}