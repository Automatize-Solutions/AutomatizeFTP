using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class ProviderView : ReactiveUserControl<ICloudViewModel>
{
    public ProviderView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}