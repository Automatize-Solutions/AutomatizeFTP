using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using AutomatizeFTP.Services.Providers;

namespace AutomatizeFTP.Services;

public sealed class CloudFactory : ICloudFactory
{
    public CloudFactory(IReadOnlyCollection<CloudType> supported = null)
    {
        SupportedClouds = supported ?? new[]
        {
            CloudType.Ftp,
            CloudType.Sftp
        };
    }

    public IReadOnlyCollection<CloudType> SupportedClouds { get; }

    public ICloud CreateCloud(CloudParameters parameters) => parameters.Type switch
    {
        CloudType.Ftp => new FtpCloud(parameters),
        CloudType.Local => new LocalCloud(parameters),
        CloudType.Sftp => new SftpCloud(parameters),
        _ => throw new ArgumentOutOfRangeException(nameof(parameters))
    };
}
