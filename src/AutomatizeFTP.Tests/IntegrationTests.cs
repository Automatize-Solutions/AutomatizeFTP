using System.Reactive.Concurrency;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Interfaces;
using AutomatizeFTP.Presentation.ViewModels;
using AutomatizeFTP.Services;
using AutomatizeFTP.Services.Interfaces;
using AutomatizeFTP.Services.Models;
using DynamicData;
using FluentAssertions;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace AutomatizeFTP.Tests;

public sealed class IntegrationTests
{
    private readonly IFileManager _files = Substitute.For<IFileManager>();
    private readonly MainState _state = new();

    [Fact]
    public void ShouldWireUpAppViewModels()
    {
        _state.Clouds.AddOrUpdate(new CloudState
        {
            Type = CloudType.Local,
            CreateFolderState = new CreateFolderState
            {
                Name = "Example",
                IsVisible = true
            }
        });

        var main = BuildMainViewModel();
        main.SupportedTypes.Should().Contain(CloudType.Local);
        main.SelectedSupportedType.Should().Be(CloudType.Local);
        main.Clouds.Should().NotBeEmpty();
        main.Clouds.Count.Should().Be(1);

        var provider = main.Clouds[0];
        provider.Name.Should().Be("Local");
        provider.CanInteract.Should().BeFalse();
        provider.Rename.IsVisible.Should().BeFalse();
        provider.Folder.IsVisible.Should().BeTrue();
        provider.Folder.Name.Should().Be("Example");
    }

    private IMainViewModel BuildMainViewModel()
    {
        var scheduler = ImmediateScheduler.Instance;
        return new MainViewModel(
            _state,
            new CloudFactory(),
            (state, provider) => new CloudViewModel(
                state,
                owner => new CreateFolderViewModel(state.CreateFolderState, owner, provider, scheduler),
                owner => new RenameFileViewModel(state.RenameFileState, owner, provider, scheduler),
                (file, owner) => new FileViewModel(owner, file),
                (folder, owner) => new FolderViewModel(owner, folder),
                new AuthViewModel(
                    new HostAuthViewModel(state.AuthState.HostAuthState, provider, scheduler),
                    provider,
                    scheduler),
                _files,
                provider,
                scheduler),
            scheduler);
    }
}
