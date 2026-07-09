<p><img src="images/horizontal.png" alt="Camelotia" height="50px"></p>

[![Build](https://github.com/reactiveui/Camelotia/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/Camelotia/actions/workflows/ci-build.yml) [![Pull Requests](https://img.shields.io/github/issues-pr/reactiveui/camelotia.svg)](https://github.com/reactiveui/Camelotia/pulls) [![Issues](https://img.shields.io/github/issues/reactiveui/camelotia.svg)](https://github.com/reactiveui/Camelotia/issues) ![License](https://img.shields.io/github/license/reactiveui/camelotia.svg) ![Size](https://img.shields.io/github/repo-size/reactiveui/camelotia.svg) [![codecov](https://codecov.io/gh/reactiveui/Camelotia/branch/main/graph/badge.svg?token=dmQeHH4Us8)](https://codecov.io/gh/reactiveui/Camelotia)

Camelotia is a sample cross-platform application built with reactive extensions, [ReactiveUI](https://github.com/reactiveui/ReactiveUI), and modern .NET UI frameworks. Camelotia is a file manager, it currently supports FTP, SFTP, and local file systems. The app runs on Windows, Linux and MacOS.

### Compiling Avalonia app

<img src="images/UiAvalonia.png" width="550">

In order to compile .NET Standard libraries, run tests and run the <a href="https://github.com/avaloniaui">Avalonia</a> application on Windows, Linux or MacOS operating system make sure to have latest [.NET Core SDK](https://dot.net/) installed. Launch the `Camelotia.sln` file to browse or to edit source files. Camelotia uses [Nuke Build](https://github.com/nuke-build/nuke) to build and test the solution. Execute the following commands to run the build scripts on Linux or MacOS:

```sh
# Linux or MacOS shell. Launches the Avalonia app after build.
git clone https://github.com/worldbeater/Camelotia
cd ./Camelotia && bash ./build.sh --interactive
```

On Windows, execute the following command line:

```sh
# Windows command line. Launches the Avalonia app after build.
# Use the '--configuration Release' option to generate app packages.
git clone https://github.com/worldbeater/Camelotia
cd ./Camelotia && powershell -ExecutionPolicy Unrestricted ./build.ps1 --interactive
```

### Technologies and Tools Used

- <a href="https://reactiveui.net/">ReactiveUI</a> modern MVVM framework
- <a href="https://github.com/reactiveui/reactiveui.validation">ReactiveUI.Validation</a> reactive validation library
- <a href="https://reactiveui.net/docs/handbook/events/">ReactiveUI.Events</a> turning regular events into observables
- <a href="https://github.com/reactiveui/DynamicData">DynamicData</a> reactive collections
- <a href="https://github.com/avaloniaui">AvaloniaUI</a> cross-platform XAML-based GUI framework
- <a href="https://github.com/worldbeater/citrus.avalonia">Citrus.Avalonia</a> bright and modern AvaloniaUI theme
- <a href="https://github.com/reactiveui/Akavache">Akavache</a> persistent key-value store
- <a href="https://github.com/nuke-build/nuke">Nuke</a> build automation system for C#/.NET
- <a href="https://github.com/xunit/xunit">XUnit</a> unit testing tool for .NET
- <a href="https://github.com/tonerdo/coverlet">Coverlet</a> code coverage analyzer
- <a href="https://github.com/nsubstitute/NSubstitute">NSubstitute</a> mocking library
- <a href="https://github.com/fluentassertions/fluentassertions">FluentAssertions</a> assertions library
- <a href="https://github.com/dotnet/reactive">Reactive Extensions</a> for .NET
- <a href="https://github.com/robinrodricks/FluentFTP">FluentFTP</a> FTP implementation
- <a href="https://github.com/sshnet/SSH.NET/">SSH.NET</a> SFTP implementation
- <a href="https://www.jetbrains.com/rider/">JetBrains Rider</a> and <a href="https://visualstudio.microsoft.com/">Microsoft Visual Studio</a> IDEs
- <a href="https://github.com/fornever/avaloniarider">AvaloniaRider</a> plugin for visual designer support
