using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

public sealed class LocalizationManager
{
    private static readonly Uri EnglishResources =
        new("avares://AutomatizeFTP.Presentation.Avalonia/Resources/Strings.en.axaml");

    private static readonly Uri PortugueseResources =
        new("avares://AutomatizeFTP.Presentation.Avalonia/Resources/Strings.pt-BR.axaml");

    public static LocalizationManager Instance { get; } = new();

    public event EventHandler LanguageChanged;

    public bool IsPortuguese { get; private set; }

    public string CurrentLanguage => IsPortuguese ? "pt-BR" : "en";

    public void ToggleLanguage()
    {
        UseLanguage(IsPortuguese ? "en" : "pt-BR");
    }

    public void UseLanguage(string cultureName)
    {
        var portuguese = string.Equals(cultureName, "pt-BR", StringComparison.OrdinalIgnoreCase);
        var resources = Application.Current?.Resources;

        if (resources is null)
            return;

        var source = portuguese ? PortugueseResources : EnglishResources;
        var dictionary = new ResourceInclude(source)
        {
            Source = source
        };
        if (resources.MergedDictionaries.Count == 0)
            resources.MergedDictionaries.Add(dictionary);
        else
            resources.MergedDictionaries[0] = dictionary;

        IsPortuguese = portuguese;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
