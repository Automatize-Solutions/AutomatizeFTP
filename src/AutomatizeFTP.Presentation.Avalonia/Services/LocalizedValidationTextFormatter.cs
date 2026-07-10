using Avalonia;
using Avalonia.Styling;
using ReactiveUI.Validation.Collections;
using ReactiveUI.Validation.Formatters.Abstractions;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

public sealed class LocalizedValidationTextFormatter : IValidationTextFormatter<string>
{
    private static readonly IReadOnlyDictionary<string, string> ResourceKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["User name shouldn't be null or white space."] = "ValidationUserName",
            ["User name shouldn't be empty."] = "ValidationUserName",
            ["Password shouldn't be null or white space."] = "ValidationPassword",
            ["Password shouldn't be empty."] = "ValidationPassword",
            ["Host address shouldn't be null or white space."] = "ValidationHostAddress",
            ["Host address shouldn't be empty."] = "ValidationHostAddress",
            ["Port should be a valid integer."] = "ValidationPort",
            ["Folder name shouldn't be empty."] = "ValidationFolderName",
            ["Path shouldn't be empty"] = "ValidationPath",
            ["New name shouldn't be empty."] = "ValidationNewName",
            ["Old name shouldn't be empty."] = "ValidationOldName",
        };

    public string Format(IValidationText validationText)
    {
        var message = validationText.ToSingleLine();
        if (!ResourceKeys.TryGetValue(message, out var resourceKey))
            return message;

        var resources = global::Avalonia.Application.Current?.Resources;
        return resources is not null &&
               resources.TryGetResource(resourceKey, ThemeVariant.Default, out var resource)
            ? resource?.ToString() ?? message
            : message;
    }
}
