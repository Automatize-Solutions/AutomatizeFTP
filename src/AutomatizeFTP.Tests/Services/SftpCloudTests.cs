using System;
using AutomatizeFTP.Services.Models;
using AutomatizeFTP.Services.Providers;
using FluentAssertions;
using Xunit;

namespace AutomatizeFTP.Tests.Services;

public sealed class SftpCloudTests
{
    private readonly CloudParameters _model = new()
    {
        Id = Guid.NewGuid(),
        Created = DateTime.Now,
        Type = CloudType.Sftp
    };

    [Fact]
    public void VerifyDefaultPropertyValues()
    {
        var provider = new SftpCloud(_model);

        // Must be '/' on every OS — Path.DirectorySeparatorChar
        // would yield '\' on Windows and corrupt remote paths.
        provider.InitialPath.Should().Be("/");

        provider.CanCreateFolder.Should().BeTrue();
        provider.Created.Should().Be(_model.Created);
        provider.Name.Should().Be("Sftp");
        provider.Id.Should().Be(_model.Id);

        provider.SupportsHostAuth.Should().BeTrue();
    }
}
