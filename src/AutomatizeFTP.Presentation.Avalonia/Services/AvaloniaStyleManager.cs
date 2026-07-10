using System;
using Avalonia.Markup.Xaml.Styling;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

public sealed class AvaloniaStyleManager
{
    public enum Theme
    {
        Light,
        Dark
    }

    private static readonly Uri LightResources =
        new("avares://AutomatizeFTP.Presentation.Avalonia/Resources/Theme.Light.axaml");

    private static readonly Uri DarkResources =
        new("avares://AutomatizeFTP.Presentation.Avalonia/Resources/Theme.DarkBlue.axaml");

    private static void SetThemeResources(Theme theme)
    {
        var resources = global::Avalonia.Application.Current?.Resources;
        if (resources is null)
            return;

        var source = theme == Theme.Light ? LightResources : DarkResources;
        var dictionary = new ResourceInclude(source)
        {
            Source = source
        };

        if (resources.MergedDictionaries.Count < 2)
            resources.MergedDictionaries.Add(dictionary);
        else
            resources.MergedDictionaries[1] = dictionary;
    }

    public AvaloniaStyleManager()
    {
        SetThemeResources(Theme.Light);
    }

    public Theme CurrentTheme { get; private set; } = Theme.Light;

    public event EventHandler ThemeChanged;

    public void UseNextTheme() =>
        UseTheme(CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light);

    public void UseTheme(string themeName)
    {
        var normalizedThemeName = string.Equals(themeName, "DarkBlue", StringComparison.OrdinalIgnoreCase)
            ? nameof(Theme.Dark)
            : themeName;
        var theme = Enum.TryParse<Theme>(normalizedThemeName, true, out var parsed)
            ? parsed
            : Theme.Light;
        UseTheme(theme);
    }

    private void UseTheme(Theme theme)
    {
        CurrentTheme = theme;
        SetThemeResources(theme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
