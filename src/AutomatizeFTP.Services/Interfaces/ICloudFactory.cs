using AutomatizeFTP.Services.Models;

namespace AutomatizeFTP.Services.Interfaces;

public interface ICloudFactory
{
    ICloud CreateCloud(CloudParameters parameters);

    IReadOnlyCollection<CloudType> SupportedClouds { get; }
}