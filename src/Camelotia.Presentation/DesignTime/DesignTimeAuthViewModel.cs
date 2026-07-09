using Camelotia.Presentation.Interfaces;
using ReactiveUI;

namespace Camelotia.Presentation.DesignTime;

public class DesignTimeAuthViewModel : ReactiveObject, IAuthViewModel
{
    public IHostAuthViewModel HostAuth { get; } = new DesignTimeHostAuthViewModel();

    public bool SupportsHostAuth { get; }

    public bool IsAuthenticated { get; } = true;

    public bool IsAnonymous { get; }
}