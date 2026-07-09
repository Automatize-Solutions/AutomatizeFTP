# AutomatizeFTP

A minimal, cross-platform **FTP / SFTP client** for desktop, built with
[Avalonia](https://avaloniaui.net/) and [ReactiveUI](https://www.reactiveui.net/)
on modern .NET. Runs on **macOS, Linux and Windows** from a single codebase.

The goal is deliberately narrow: a fast, no-nonsense file transfer client — the
FileZilla-style workflow you actually use day to day (browse local + remote,
upload/download, navigate, manage files) — without the weight of a general-purpose
cloud file manager.

## Origin & credits

AutomatizeFTP started as a derivative of
[**Camelotia**](https://github.com/reactiveui/Camelotia) by
[@worldbeater](https://github.com/worldbeater) and the ReactiveUI community.

Camelotia is an **excellent** project. It's a genuinely well-architected,
cross-platform sample that shows how to build reactive .NET desktop apps the right
way — clean provider abstractions, ReactiveUI end to end, and a build/test setup
that works across Windows, Linux and macOS. Much of the solid foundation this
project stands on came straight from there, and it's worth studying on its own
merits whether or not you care about FTP.

- Upstream: **https://github.com/reactiveui/Camelotia**
- This project: **https://github.com/Automatize-Solutions/AutomatizeFTP**

Camelotia is MIT-licensed. The original `LICENSE` is preserved in this repository,
and the full upstream history is kept intact in the Git log — no history rewrite,
so you can trace exactly where each piece came from.

## What's different from Camelotia

Camelotia is a multi-provider cloud file manager (Google Drive, Yandex Disk, VK,
GitHub, FTP, SFTP, local) targeting many UI heads (Avalonia, WPF, UWP, Xamarin).
AutomatizeFTP strips that down to the essentials:

- **Providers:** only **FTP**, **SFTP** and the **local file system** are kept.
  All cloud providers and their SDK dependencies were removed.
- **UI head:** only the **Avalonia** target is kept. The WPF, UWP and Xamarin
  heads were dropped.
- **Auth:** the OAuth flow was removed (no remaining provider needs it); FTP/SFTP
  use direct host/credential authentication.
- **Focus:** the UI is being reshaped from a "cloud storage manager" into a
  purpose-built FTP/SFTP client.

## Tech stack

- **.NET** (modern SDK-style projects, single Avalonia solution)
- **Avalonia UI** — cross-platform XAML UI framework
- **ReactiveUI** — MVVM / reactive view models
- **[FluentFTP](https://github.com/robinrodricks/FluentFTP)** — FTP / FTPS
- **[SSH.NET](https://github.com/sshnet/SSH.NET)** — SFTP
- **Newtonsoft.Json** — app-state persistence

## Roadmap

The work is proceeding in stages, deliberately kept separate:

1. **Prune** — remove unused providers and UI heads, keep the solution building and
   tests green. *(done)*
2. **Stabilize** — fix outstanding test failures and any reactive-behavior drift
   inherited from dependency upgrades, to reach a known-good baseline. *(done)*
3. **Rebrand** — rename the `Camelotia.*` projects, solution and namespaces to
   `AutomatizeFTP.*` once the surface is minimal. *(done)*
4. **Reshape the UI** — turn the cloud-manager interface into a focused FTP/SFTP
   client (dual-pane browsing, transfer progress, connection/site management).
5. **Polish & package** — macOS-first packaging, then Linux and Windows.

## Building

Requires a recent .NET SDK.

```bash
dotnet build src/AutomatizeFTP.slnx
dotnet test  src/AutomatizeFTP.slnx
dotnet run --project src/AutomatizeFTP.Presentation.Avalonia
```

## License

MIT — see [`LICENSE`](./LICENSE). This project inherits Camelotia's MIT license and
preserves the original copyright notice, as required.
