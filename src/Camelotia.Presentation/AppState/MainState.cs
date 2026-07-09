using System.Runtime.Serialization;
using Camelotia.Services.Models;
using DynamicData;

namespace Camelotia.Presentation.AppState;

[DataContract]
public class MainState
{
    [IgnoreDataMember]
    public SourceCache<CloudState, Guid> Clouds { get; } = new(x => x.Id);

    [DataMember]
    public IEnumerable<CloudState> CloudStates
    {
        // Materialize as an array: a collection expression yields the compiler's
        // internal <>z__ReadOnlyArray, which TypeNameHandling.All persists as $type
        // and Newtonsoft cannot construct when reading the state back.
        get => Clouds.Items.ToArray();
        set => Clouds.AddOrUpdate(value);
    }

    [DataMember]
    public CloudType? SelectedSupportedType { get; set; }

    [DataMember]
    public Guid SelectedProviderId { get; set; }
}
