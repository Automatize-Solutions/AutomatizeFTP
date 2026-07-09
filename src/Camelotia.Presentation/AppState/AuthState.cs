using System.Runtime.Serialization;

namespace Camelotia.Presentation.AppState;

[DataContract]
public class AuthState
{
    [DataMember]
    public HostAuthState HostAuthState { get; set; } = new();
}
