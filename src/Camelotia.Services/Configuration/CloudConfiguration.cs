using System.Runtime.Serialization;

namespace Camelotia.Services.Configuration;

[DataContract]
public class CloudConfiguration
{
    [DataMember]
    public GitHubCloudOptions GitHub { get; set; } = new();

    [DataMember]
    public YandexDiskCloudOptions YandexDisk { get; set; } = new();
}
