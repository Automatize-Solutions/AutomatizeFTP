using AutomatizeFTP.Services.Models;

namespace AutomatizeFTP.Services.Interfaces;

public interface ICloud
{
    Guid Id { get; }

    long? Size { get; }

    string Name { get; }

    DateTime Created { get; }

    string InitialPath { get; }

    Task<IEnumerable<FileModel>> GetFiles(string path);

    Task<IEnumerable<FolderModel>> GetBreadCrumbs(string path);

    Task UploadFile(string toPath, Stream fromStream, string name);

    Task DownloadFile(string fromPath, Stream fromStream);

    Task CreateFolder(string path, string name);

    Task RenameFile(string path, string name);

    Task Delete(string path, bool isFolder);

    Task HostAuth(string address, int port, string login, string password);

    Task Logout();

    bool CanCreateFolder { get; }

    IObservable<bool> IsAuthorized { get; }

    CloudParameters Parameters { get; }

    bool SupportsHostAuth { get; }
}