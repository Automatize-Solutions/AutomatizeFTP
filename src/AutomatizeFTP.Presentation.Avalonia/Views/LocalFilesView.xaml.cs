using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class LocalFilesView : ReactiveUserControl<ICloudViewModel>
{
    public LocalFilesView()
    {
        InitializeComponent();
    }
}
