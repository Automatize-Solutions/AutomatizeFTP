using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class CreateFolderView : ReactiveUserControl<ICreateFolderViewModel>
{
    public CreateFolderView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}