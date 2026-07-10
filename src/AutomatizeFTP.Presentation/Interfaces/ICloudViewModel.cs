using System.ComponentModel;
using System.Reactive;
using AutomatizeFTP.Services.Models;
using ReactiveUI;

namespace AutomatizeFTP.Presentation.Interfaces;

public interface ICloudViewModel : INotifyPropertyChanged
{
    Guid Id { get; }

    IAuthViewModel Auth { get; }

    IRenameFileViewModel Rename { get; }

    ICreateFolderViewModel Folder { get; }

    IFileViewModel SelectedFile { get; set; }

    IReadOnlyList<IFileViewModel> SelectedFiles { get; }

    IEnumerable<IFileViewModel> Files { get; }

    IEnumerable<IFolderViewModel> BreadCrumbs { get; }

    ReactiveCommand<Unit, Unit> DownloadSelectedFile { get; }

    ReactiveCommand<Unit, Unit> UploadToCurrentPath { get; }

    ReactiveCommand<Unit, Unit> DeleteSelectedFile { get; }

    ReactiveCommand<Unit, Unit> UnselectFile { get; }

    ReactiveCommand<Unit, IEnumerable<FileModel>> Refresh { get; }

    ReactiveCommand<Unit, Unit> Logout { get; }

    ReactiveCommand<Unit, string> Back { get; }

    ReactiveCommand<Unit, string> Open { get; }

    ReactiveCommand<string, string> SetPath { get; }

    Task UploadFileFromAsync(string sourcePath, string name, bool isFolder, string destinationPath = null);

    Task MoveFileToAsync(string sourcePath, string destinationPath, string name);

    Task DownloadFileToAsync(string sourcePath, string destinationPath, string name, bool isFolder);

    void SetSelectedFiles(IEnumerable<IFileViewModel> files);

    bool IsCurrentPathEmpty { get; }

    bool IsLoading { get; }

    bool IsReady { get; }

    bool HasErrorMessage { get; }

    string ErrorMessage { get; }

    void ReportError(Exception exception);

    bool CanLogout { get; }

    bool CanInteract { get; }

    bool ShowBreadCrumbs { get; }

    bool HideBreadCrumbs { get; }

    string CurrentPath { get; }

    string Description { get; }

    DateTime Created { get; }

    string Name { get; }

    string Size { get; }
}
