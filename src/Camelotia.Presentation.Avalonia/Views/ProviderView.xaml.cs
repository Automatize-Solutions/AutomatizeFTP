using Camelotia.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Camelotia.Presentation.Avalonia.Views;

public sealed partial class ProviderView : ReactiveUserControl<ICloudViewModel>
{
    public ProviderView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}