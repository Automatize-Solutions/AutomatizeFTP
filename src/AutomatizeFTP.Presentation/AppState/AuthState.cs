using System.Runtime.Serialization;

namespace AutomatizeFTP.Presentation.AppState;

[DataContract]
public class AuthState
{
    [DataMember]
    public HostAuthState HostAuthState { get; set; } = new();
}
