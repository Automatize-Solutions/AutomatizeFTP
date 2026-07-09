using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AutomatizeFTP.Presentation.Interfaces;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AutomatizeFTP.Presentation.Avalonia.Views;

public sealed partial class AuthView : ReactiveUserControl<IAuthViewModel>
{
    public AuthView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(context => context != null)
                .Select(ResolveControl)
                .BindTo(this, x => x.Content)
                .DisposeWith(disposables);
        });
    }

    private static Control ResolveControl(IAuthViewModel context)
    {
        if (context.SupportsHostAuth)
            return new HostAuthView { DataContext = context.HostAuth };
        return new TextBlock
        {
            Text = "No supported authentication method found.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}