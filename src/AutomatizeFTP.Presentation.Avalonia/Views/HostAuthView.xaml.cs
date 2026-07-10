using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using AutomatizeFTP.Presentation.Avalonia.Services;
using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Validation.Extensions;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class HostAuthView : ReactiveUserControl<IHostAuthViewModel>
{
    public HostAuthView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            var validationBindings = new SerialDisposable().DisposeWith(disposables);
            var formatter = new LocalizedValidationTextFormatter();

            void BindLocalizedValidation()
            {
                var bindings = new CompositeDisposable
                {
                    this.BindValidation(ViewModel, x => x.Address, x => x.AddressValidation.Text, formatter),
                    this.BindValidation(ViewModel, x => x.Port, x => x.PortValidation.Text, formatter),
                    this.BindValidation(ViewModel, x => x.Username, x => x.UsernameValidation.Text, formatter),
                    this.BindValidation(ViewModel, x => x.Password, x => x.PasswordValidation.Text, formatter),
                    this.BindValidation(ViewModel, x => x.FormValidation.Text, formatter),
                };

                validationBindings.Disposable = bindings;
            }

            void OnLanguageChanged(object sender, EventArgs args) => BindLocalizedValidation();

            LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
            disposables.Add(Disposable.Create(() => LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged));
            BindLocalizedValidation();
        });
    }
}
