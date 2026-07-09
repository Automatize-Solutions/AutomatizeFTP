using System.Runtime.Serialization;

namespace Camelotia.Services.Configuration;

[DataContract]
public class CloudConfiguration
{
    [DataMember]
    public YandexDiskCloudOptions YandexDisk { get; set; } = new();
}
