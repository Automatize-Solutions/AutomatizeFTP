using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using AutomatizeFTP.Presentation.AppState;
using AutomatizeFTP.Presentation.Interfaces;
using AutomatizeFTP.Presentation.ViewModels;
using AutomatizeFTP.Services.Interfaces;
using FluentAssertions;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace AutomatizeFTP.Tests.Presentation;

public sealed class CreateFolderViewModelTests
{
    private static readonly string Separator = Path.DirectorySeparatorChar.ToString();
    private readonly ICloudViewModel _model = Substitute.For<ICloudViewModel>();
    private readonly ICloud _provider = Substitute.For<ICloud>();
    private readonly CreateFolderState _state = new();

    [Fact]
    public void ShouldProperlyInitializeCreateFolderViewModel()
    {
        var model = BuildCreateFolderViewModel();
        model.Name.Should().BeNullOrEmpty();
        model.Path.Should().BeNullOrEmpty();

        model.ErrorMessage.Should().BeNullOrEmpty();
        model.HasErrorMessage.Should().BeFalse();
        model.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void ShouldChangeVisibility()
    {
        _model.CanInteract.Returns(true);
        _model.CurrentPath.Returns(Separator);
        _provider.CanCreateFolder.Returns(true);

        var model = BuildCreateFolderViewModel();
        model.Open.CanExecute().Should().BeTrue();
        model.Open.CanExecute().Should().BeTrue();
        model.Close.CanExecute().Should().BeFalse();
        model.IsVisible.Should().BeFalse();
        model.Open.Execute().Subscribe();

        model.Open.CanExecute().Should().BeFalse();
        model.Close.CanExecute().Should().BeTrue();
        model.IsVisible.Should().BeTrue();
        model.Close.Execute().Subscribe();

        model.Open.CanExecute().Should().BeTrue();
        model.Close.CanExecute().Should().BeFalse();
        model.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void ShouldCreateFolderSuccessfullyAndCloseViewModel()
    {
        _model.CanInteract.Returns(true);
        _model.CurrentPath.Returns(Separator);
        _provider.CanCreateFolder.Returns(true);

        var model = BuildCreateFolderViewModel();
        model.IsVisible.Should().BeFalse();
        model.Close.CanExecute().Should().BeFalse();
        model.Open.CanExecute().Should().BeTrue();
        model.Open.Execute().Subscribe();

        model.IsVisible.Should().BeTrue();
        model.Create.CanExecute().Should().BeFalse();
        model.ErrorMessage.Should().BeNullOrEmpty();
        model.HasErrorMessage.Should().BeFalse();
        model.IsLoading.Should().BeFalse();

        model.Close.CanExecute().Should().BeTrue();
        model.Open.CanExecute().Should().BeFalse();

        model.Name = "Foo";
        model.Create.CanExecute().Should().BeTrue();
        model.Create.Execute().Subscribe();

        model.IsLoading.Should().BeFalse();
        model.Create.CanExecute().Should().BeFalse();
        model.Name.Should().BeNullOrEmpty();
        model.Path.Should().Be(Separator);
        model.IsVisible.Should().BeFalse();

        model.Close.CanExecute().Should().BeFalse();
        model.Open.CanExecute().Should().BeTrue();
    }

    [Fact]
    public void ShouldEnterCreatedFolderWhenRequested()
    {
        _model.CanInteract.Returns(true);
        _model.CurrentPath.Returns(Separator);
        _provider.CanCreateFolder.Returns(true);

        var paths = new List<string>();
        var setPath = ReactiveCommand.Create<string, string>(path => path);
        setPath.Subscribe(paths.Add);
        _model.SetPath.Returns(setPath);

        var model = BuildCreateFolderViewModel();
        model.OpenAndEnter.Execute().Subscribe();
        model.Name = "Foo";
        model.Create.Execute().Subscribe();

        paths.Should().Contain(Path.Combine(Separator, "Foo"));
    }

    [Fact]
    public void ShouldUpdateValidationsForProperties()
    {
        _model.CurrentPath.Returns(Separator);

        var model = BuildCreateFolderViewModel();
        model.Create.CanExecute().Should().BeFalse();
        model.GetErrors(string.Empty).Cast<object>().Should().HaveCount(1);
        model.GetErrors(nameof(model.Name)).Cast<object>().Should().HaveCount(1);
        model.HasErrors.Should().BeTrue();

        model.Name = "Example";
        model.Create.CanExecute().Should().BeTrue();
        model.GetErrors(string.Empty).Cast<object>().Should().BeEmpty();
        model.GetErrors(nameof(model.Name)).Cast<object>().Should().BeEmpty();
        model.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ShouldUpdateStateProperties()
    {
        const string name = "Secret Folder";
        var model = BuildCreateFolderViewModel();

        _state.Name.Should().BeNullOrWhiteSpace();
        _state.IsVisible.Should().BeFalse();

        model.Name = name;
        model.IsVisible = true;

        _state.Name.Should().Be(name);
        _state.IsVisible.Should().BeTrue();
    }

    private CreateFolderViewModel BuildCreateFolderViewModel()
    {
        var scheduler = ImmediateScheduler.Instance;
        return new CreateFolderViewModel(_state, _model, _provider, scheduler);
    }
}
