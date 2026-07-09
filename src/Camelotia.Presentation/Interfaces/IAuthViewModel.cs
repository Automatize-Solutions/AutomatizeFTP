using System.ComponentModel;

namespace Camelotia.Presentation.Interfaces;

public interface IAuthViewModel : INotifyPropertyChanged
{
    IHostAuthViewModel HostAuth { get; }

    bool SupportsHostAuth { get; }

    bool IsAuthenticated { get; }

    bool IsAnonymous { get; }
}