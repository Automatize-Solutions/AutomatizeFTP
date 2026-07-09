using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Camelotia.Presentation.AppState;
using Camelotia.Presentation.Infrastructure;
using Camelotia.Services.Models;
using DynamicData;
using FluentAssertions;
using ReactiveUI;
using Xunit;

namespace Camelotia.Tests.Presentation;

public class SuspensionDriversTests
{
    [Fact]
    public Task NewtonsoftJsonSuspensionDriverShouldSaveAndLoadState() =>
        SuspensionDriverShouldSaveAndLoadState(
            new NewtonsoftJsonSuspensionDriver(Path.GetTempFileName()));

    [Fact]
    public Task AkavacheSuspensionDriverShouldSaveAndLoadState() =>
        SuspensionDriverShouldSaveAndLoadState(
            new AkavacheSuspensionDriver<MainState>("Camelotia-Tests"));

    private static async Task SuspensionDriverShouldSaveAndLoadState(ISuspensionDriver driver)
    {
        var state = new MainState
        {
            SelectedSupportedType = CloudType.Sftp
        };

        state.Clouds.AddOrUpdate(new CloudState());
        state.Clouds.AddOrUpdate(new CloudState
        {
            AuthState = new AuthState
            {
                HostAuthState = new HostAuthState
                {
                    Username = "Joseph Joestar",
                    Password = "Dio"
                }
            }
        });

        await driver.SaveState(state);
        var loaded = await driver.LoadState();
        loaded.Should().BeOfType<MainState>();

        var retyped = (MainState)loaded;
        retyped.SelectedSupportedType.Should().Be(CloudType.Sftp);
        retyped.Clouds.Count.Should().Be(2);
        retyped.CloudStates.Should().NotBeEmpty();
        retyped.CloudStates.Should().Contain(provider =>
            provider.AuthState.HostAuthState.Username == "Joseph Joestar" &&
            provider.AuthState.HostAuthState.Password == "Dio");

        await driver.InvalidateState();
        await Assert.ThrowsAnyAsync<Exception>(async () => await driver.LoadState()).ConfigureAwait(false);
        await driver.SaveState(new MainState());
        await driver.LoadState();
    }
}