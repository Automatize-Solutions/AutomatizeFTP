using System.Runtime.Serialization;

namespace AutomatizeFTP.Presentation.AppState;

[DataContract]
public class RenameFileState
{
    [DataMember]
    public string NewName { get; set; }
}