using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class RenameFileView : ReactiveUserControl<IRenameFileViewModel>
{
    public RenameFileView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}