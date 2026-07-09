using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class MainView : ReactiveUserControl<IMainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        this.WhenActivated(disposables => { });
    }
}