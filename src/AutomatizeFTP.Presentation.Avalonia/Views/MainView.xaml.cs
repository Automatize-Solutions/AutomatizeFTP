using System.Reflection;
using AutomatizeFTP.Presentation.Interfaces;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class MainView : ReactiveUserControl<IMainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        AppVersionLabel.Text = version is null ? string.Empty : $"· v{version.ToString(3)}";
        this.WhenActivated(disposables => { });
    }
}
