using System.ComponentModel;

namespace AutomatizeFTP.Presentation.Interfaces;

public interface IAuthViewModel : INotifyPropertyChanged
{
    IHostAuthViewModel HostAuth { get; }

    bool SupportsHostAuth { get; }

    bool IsAuthenticated { get; }

    bool IsAnonymous { get; }
}