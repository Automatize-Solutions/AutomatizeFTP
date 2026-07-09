using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;
using Camelotia.Services.Providers;

namespace Camelotia.Services;

public sealed class CloudFactory : ICloudFactory
{
    public CloudFactory(IReadOnlyCollection<CloudType> supported = null)
    {
        SupportedClouds = supported ?? new[]
        {
            CloudType.Local,
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
