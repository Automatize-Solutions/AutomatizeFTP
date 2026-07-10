using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class FolderView : ReactiveUserControl<IFolderViewModel>
{
    public FolderView()
    {
        InitializeComponent();
    }
}
