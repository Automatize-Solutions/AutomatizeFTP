using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using AutomatizeFTP.Services.Models;
using ReactiveUI;

namespace AutomatizeFTP.Presentation.Interfaces;

public interface IMainViewModel : INotifyPropertyChanged
{
    ReadOnlyObservableCollection<ICloudViewModel> Clouds { get; }

    ICloudViewModel SelectedProvider { get; set; }

    ICloudViewModel LocalProvider { get; }

    IEnumerable<CloudType> SupportedTypes { get; }

    CloudType SelectedSupportedType { get; set; }

    bool WelcomeScreenCollapsed { get; }

    bool WelcomeScreenVisible { get; }

    ReactiveCommand<Unit, Unit> Unselect { get; }

    ReactiveCommand<Unit, Unit> Refresh { get; }

    ReactiveCommand<Unit, Unit> Remove { get; }

    ReactiveCommand<Unit, Unit> Add { get; }

    ReactiveCommand<Unit, Unit> UploadToRemote { get; }

    ReactiveCommand<Unit, Unit> DownloadToLocal { get; }

    bool IsLoading { get; }

    bool IsReady { get; }
}
