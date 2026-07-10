using System.ComponentModel;

namespace AutomatizeFTP.Presentation.Interfaces;

public interface IFolderViewModel : INotifyPropertyChanged
{
    ICloudViewModel Provider { get; }

    string Name { get; }

    string FullPath { get; }

    IEnumerable<IFolderViewModel> Children { get; }
}
