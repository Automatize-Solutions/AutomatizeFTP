using System.ComponentModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;

namespace AutomatizeFTP.Presentation.Interfaces;

public interface IHostAuthViewModel :
    INotifyPropertyChanged,
    INotifyDataErrorInfo,
    IValidatableViewModel,
    IReactiveObject
{
    string Username { get; set; }

    string Password { get; set; }

    string Address { get; set; }

    string Port { get; set; }

    ReactiveCommand<Unit, Unit> Login { get; }

    bool HasErrorMessage { get; }

    string ErrorMessage { get; }

    bool IsBusy { get; }
}
