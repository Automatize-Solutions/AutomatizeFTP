using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace AutomatizeFTP.Presentation.Avalonia.Services;

public sealed class LocalizedTextConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<string, string> TransferResourceKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Uploading"] = "TransferUploading",
            ["Downloading"] = "TransferDownloading",
            ["In progress"] = "TransferInProgress",
            ["Completed"] = "TransferCompleted",
            ["Failed"] = "TransferFailed",
            ["Cancelled"] = "TransferCancelled",
        };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString();
        var key = text is not null && TransferResourceKeys.TryGetValue(text, out var transferKey)
            ? transferKey
            : text;
        var resources = Application.Current?.Resources;
        return resources is not null &&
               resources.TryGetResource(key, ThemeVariant.Default, out var resource)
            ? resource?.ToString() ?? key
            : key;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
