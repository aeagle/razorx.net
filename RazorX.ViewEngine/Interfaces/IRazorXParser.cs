using System.IO;

namespace RazorX.ViewEngine
{
    public interface IRazorXParser
    {
        string Process(Stream stream);
        string Process(string razorContent);
    }
}