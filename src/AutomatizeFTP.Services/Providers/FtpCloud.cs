using System.Reactive.Subjects;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using FluentFTP;

namespace AutomatizeFTP.Services.Providers;

public sealed class FtpCloud : ICloud
{
    private static readonly string[] PathSeparators = { "/", "\\" };
    private readonly ISubject<bool> _isAuthorized = new ReplaySubject<bool>();
    private Func<AsyncFtpClient> _factory;

    public FtpCloud(CloudParameters model)
    {
        Parameters = model;
        _isAuthorized.OnNext(false);
    }

    public CloudParameters Parameters { get; }

    public long? Size => null;

    public Guid Id => Parameters.Id;

    public string InitialPath => "/";

    public string Name => Parameters.Type.ToString();

    public DateTime Created => Parameters.Created;

    public IObservable<bool> IsAuthorized => _isAuthorized;

    public bool SupportsHostAuth => true;

    public bool CanCreateFolder => true;

    public async Task HostAuth(string address, int port, string login, string password)
    {
        _factory = () => new AsyncFtpClient(address, login, password, port);
        await GetFiles("/").ConfigureAwait(false);
        _isAuthorized.OnNext(true);
    }

    public async Task<IEnumerable<FileModel>> GetFiles(string path)
    {
        using var connection = _factory();
        await connection.Connect().ConfigureAwait(false);
        var files = await connection.GetListing(path).ConfigureAwait(false);
        await connection.Disconnect().ConfigureAwait(false);
        return files.Select(file => new FileModel
        {
            IsFolder = file.Type == FtpObjectType.Directory,
            Modified = file.Modified,
            Name = file.Name,
            Path = file.FullName,
            Size = file.Size
        });
    }

    public async Task<IEnumerable<FolderModel>> GetBreadCrumbs(string path)
    {
        var pathParts = new List<string> { "/" }; // Add root path first
        pathParts.AddRange(path.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries));
        var foldermodels = new List<FolderModel>();
        using var connection = _factory();
        await connection.Connect().ConfigureAwait(false);
        for (var i = 0; i < pathParts.Count; i++)
        {
            var fullPath = i == 0
                ? "/"
                : "/" + string.Join(PathSeparators[0], pathParts.Skip(1).Take(i));
            var name = pathParts[i];
            var listing = await connection.GetListing(fullPath).ConfigureAwait(false);
            var folder = new FolderModel(
                fullPath,
                name,
                listing
                    .Where(f => f.Type == FtpObjectType.Directory)
                    .Select(f => new FolderModel(f.FullName, f.Name)));
            foldermodels.Add(folder);
        }

        await connection.Disconnect().ConfigureAwait(false);
        return foldermodels;
    }

    public async Task CreateFolder(string path, string name)
    {
        using var connection = _factory();
        await connection.Connect().ConfigureAwait(false);
        var directory = RemotePath.Combine(path, name);
        await connection.CreateDirectory(directory).ConfigureAwait(false);
        await connection.Disconnect().ConfigureAwait(false);
    }

    public async Task RenameFile(string path, string name)
    {
        using var connection = _factory();
        await connection.Connect().ConfigureAwait(false);
        var directoryName = RemotePath.GetDirectory(path);
        var newName = RemotePath.Combine(directoryName, name);
        await connection.Rename(path, newName).ConfigureAwait(false);
        await connection.Disconnect().ConfigureAwait(false);
    }

    public async Task MoveFile(string sourcePath, string destinationPath)
    {
        using var connection = _factory();
        await connection.Connect().ConfigureAwait(false);
        await connection.Rename(sourcePath, destinationPath).ConfigureAwait(false);
        await connection.Disconnect().ConfigureAwait(false);
    }

    public async Task Delete(string path, bool isFolder)
    {
        using var connection = _factory();
        await connection.Connect().ConfigureAwait(false);
        if (isFolder) await connection.DeleteDirectory(path).ConfigureAwait(false);
        else await connection.DeleteFile(path).ConfigureAwait(false);
        await connection.Disconnect().ConfigureAwait(false);
    }

    public Task Logout()
    {
        _factory = null;
        _isAuthorized.OnNext(false);
        return Task.CompletedTask;
    }

    public async Task UploadFile(
        string to,
        Stream from,
        string name,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _factory();
        await connection.Connect(cancellationToken).ConfigureAwait(false);
        var path = RemotePath.Combine(to, name);
        IProgress<FtpProgress> ftpProgress = progress is null
            ? null
            : new Progress<FtpProgress>(value => progress.Report(value.Progress));
        await connection.UploadStream(@from, path, FtpRemoteExists.Overwrite, true, ftpProgress, cancellationToken).ConfigureAwait(false);
        await connection.Disconnect(cancellationToken).ConfigureAwait(false);
    }

    public async Task DownloadFile(
        string from,
        Stream to,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _factory();
        await connection.Connect(cancellationToken).ConfigureAwait(false);
        IProgress<FtpProgress> ftpProgress = progress is null
            ? null
            : new Progress<FtpProgress>(value => progress.Report(value.Progress));
        await connection.DownloadStream(to, @from, 0, ftpProgress, cancellationToken).ConfigureAwait(false);
        await connection.Disconnect(cancellationToken).ConfigureAwait(false);
    }
}
