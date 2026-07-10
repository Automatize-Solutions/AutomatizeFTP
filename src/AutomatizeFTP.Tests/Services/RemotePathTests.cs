using AutomatizeFTP.Services.Providers;
using FluentAssertions;
using Xunit;

namespace AutomatizeFTP.Tests.Services;

public sealed class RemotePathTests
{
    [Theory]
    [InlineData("/", "teste", "/teste")]
    [InlineData("/Uploads", "teste", "/Uploads/teste")]
    [InlineData("/Uploads/", "teste", "/Uploads/teste")]
    [InlineData("/Uploads", "/teste", "/Uploads/teste")]
    [InlineData("\\Uploads\\cassol", "teste", "/Uploads/cassol/teste")]
    [InlineData("/Uploads", "sub\\teste", "/Uploads/sub/teste")]
    public void CombineShouldAlwaysProduceForwardSlashPaths(string path, string name, string expected) =>
        RemotePath.Combine(path, name).Should().Be(expected);

    [Theory]
    [InlineData("/Uploads/cassol/teste", "/Uploads/cassol")]
    [InlineData("/Uploads/cassol", "/Uploads")]
    [InlineData("/Uploads", "/")]
    [InlineData("/", "/")]
    [InlineData("\\Uploads\\cassol", "/Uploads")]
    [InlineData("\\Uploads", "/")]
    public void GetDirectoryShouldReturnParentWithForwardSlashes(string path, string expected) =>
        RemotePath.GetDirectory(path).Should().Be(expected);
}
