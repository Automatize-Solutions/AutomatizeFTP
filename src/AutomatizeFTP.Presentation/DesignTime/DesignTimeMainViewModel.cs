using System.Collections.ObjectModel;
using System.Reactive;
using AutomatizeFTP.Presentation.Interfaces;
using AutomatizeFTP.Services.Models;
using ReactiveUI;

namespace AutomatizeFTP.Presentation.DesignTime;

public class DesignTimeMainViewModel : ReactiveObject, IMainViewModel
{
    public ReadOnlyObservableCollection<ICloudViewModel> Clouds { get; } =
        new(
            new ObservableCollection<ICloudViewModel>(
                [
                    new DesignTimeCloudViewModel(),
                    new DesignTimeCloudViewModel()
                ]));

    public ICloudViewModel SelectedProvider { get; set; } = new DesignTimeCloudViewModel();

    public ICloudViewModel LocalProvider { get; } = new DesignTimeCloudViewModel();

    public IEnumerable<CloudType> SupportedTypes { get; } = [CloudType.Ftp, CloudType.Sftp];

    public CloudType SelectedSupportedType { get; set; } = CloudType.Sftp;

    public bool WelcomeScreenCollapsed { get; } = true;

    public bool WelcomeScreenVisible { get; }

    public ReactiveCommand<Unit, Unit> Unselect { get; }

    public ReactiveCommand<Unit, Unit> Refresh { get; }

    public ReactiveCommand<Unit, Unit> Remove { get; }

    public ReactiveCommand<Unit, Unit> Add { get; }

    public ReactiveCommand<Unit, Unit> UploadToRemote { get; }

    public ReactiveCommand<Unit, Unit> DownloadToLocal { get; }

    public bool IsLoading { get; }

    public bool IsReady { get; } = true;
}
