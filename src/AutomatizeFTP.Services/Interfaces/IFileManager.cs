namespace AutomatizeFTP.Services.Interfaces;

public interface IFileManager
{
    Task<Stream> OpenWrite(string name);

    Task<(string Name, Stream Stream)> OpenRead();
}