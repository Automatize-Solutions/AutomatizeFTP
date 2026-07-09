using Camelotia.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Camelotia.Presentation.Avalonia.Views;

public sealed partial class MainView : ReactiveUserControl<IMainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}