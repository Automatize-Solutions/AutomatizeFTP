using AutomatizeFTP.Services.Interfaces;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

public sealed class AvaloniaFileManager : IFileManager
{
    private readonly Window _window;

    public AvaloniaFileManager(Window window) => _window = window;

    public async Task<Stream> OpenWrite(string name)
    {
        var folders = await _window.StorageProvider
            .OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false })
            .ConfigureAwait(false);
        var folder = folders.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(folder))
            throw new OperationCanceledException("A destination folder was not selected.");

        var path = Path.Combine(folder, name);
        return File.Create(path);
    }

    public async Task<(string Name, Stream Stream)> OpenRead()
    {
        var files = await _window.StorageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false })
            .ConfigureAwait(false);
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            throw new OperationCanceledException("A source file was not selected.");

        var attributes = File.GetAttributes(path);
        var isFolder = attributes.HasFlag(FileAttributes.Directory);
        if (isFolder) throw new Exception("Folders are not supported.");

        var stream = File.OpenRead(path);
        var name = Path.GetFileName(path);
        return (name, stream);
    }
}
