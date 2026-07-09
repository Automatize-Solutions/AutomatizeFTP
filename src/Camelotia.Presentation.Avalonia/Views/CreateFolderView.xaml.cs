using Camelotia.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Camelotia.Presentation.Avalonia.Views;

public sealed partial class CreateFolderView : ReactiveUserControl<ICreateFolderViewModel>
{
    public CreateFolderView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}