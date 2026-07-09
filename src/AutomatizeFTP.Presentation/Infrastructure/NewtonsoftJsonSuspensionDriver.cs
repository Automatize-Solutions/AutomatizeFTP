using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json.Serialization.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReactiveUI;

namespace AutomatizeFTP.Presentation.Infrastructure;

public sealed class NewtonsoftJsonSuspensionDriver(string stateFilePath) : ISuspensionDriver
{
    private readonly JsonSerializerSettings _settings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    public IObservable<Unit> InvalidateState()
    {
        if (File.Exists(stateFilePath))
            File.Delete(stateFilePath);
        return Observable.Return(Unit.Default);
    }

    public IObservable<object> LoadState() => Load<object>().Select(state => (object)state);

    public IObservable<Unit> SaveState<T>(T state) => Save(state);

    // ReactiveUI offers these overloads so an AOT-friendly driver can hand
    // System.Text.Json its source-generated metadata. Newtonsoft resolves
    // contracts at runtime and has no use for it.
    public IObservable<T> LoadState<T>(JsonTypeInfo<T> typeInfo) => Load<T>();

    public IObservable<Unit> SaveState<T>(T state, JsonTypeInfo<T> typeInfo) => Save(state);

    private IObservable<T> Load<T>()
    {
        if (!File.Exists(stateFilePath))
        {
            return Observable.Throw<T>(new FileNotFoundException(stateFilePath));
        }

        var lines = File.ReadAllText(stateFilePath);
        var state = JsonConvert.DeserializeObject<T>(lines, _settings);
        return Observable.Return(state);
    }

    private IObservable<Unit> Save<T>(T state)
    {
        var lines = JsonConvert.SerializeObject(state, Formatting.Indented, _settings);
        File.WriteAllText(stateFilePath, lines);
        return Observable.Return(Unit.Default);
    }
}
