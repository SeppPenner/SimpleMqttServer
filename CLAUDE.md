# Project rules for Claude

## What this is

SimpleMqttServer is a runnable MQTT broker that takes its whole configuration from a JSON file. The
broker itself is not implemented here, it comes from the NuGet package
[MQTTnet.Server](https://www.nuget.org/packages/MQTTnet.Server/). This repository adds a host
around it: read `appsettings.json`, validate user name and password on connect, reject duplicate
client identifiers, log every connection, subscription and published message through Serilog, and
run as a Windows service or a systemd unit. It is **not** published as a NuGet package. The
delivered artifacts are the Docker images `sepppenner/simplemqttserver` and
`sepppenner/simplemqttserver-arm` plus a zipped Windows publish attached to the GitHub release of
the matching tag.

One solution `src/SimpleMqttServer.sln` with exactly one project:

- `src/SimpleMqttServer/SimpleMqttServer.csproj`, SDK `Microsoft.NET.Sdk.Web`, `OutputType` `Exe`.
  There is no test project and no class library.

Layout inside `src/SimpleMqttServer`:

- `Program.cs`: `Main` reads the configuration, sets up Serilog and runs the host.
  `CreateHostBuilder` chains `ConfigureWebHostDefaults`, `UseSerilog`, `UseWindowsService` and
  `UseSystemd`, so the same binary runs in the console, as a Windows service and under systemd.
  `EnvironmentName` comes from `ASPNETCORE_ENVIRONMENT` and falls back to `Production`.
- `Startup.cs`: the ASP.NET Core startup class. Binds the configuration section named after the
  assembly, registers `MqttServiceConfiguration` and `MqttService` as singletons and registers the
  same `MqttService` instance a second time as `IHostedService`.
- `MqttService.cs`: the real work. A `BackgroundService` that starts the MQTT server, hooks the four
  MQTTnet events (`ValidatingConnectionAsync`, `InterceptingSubscriptionAsync`,
  `InterceptingPublishAsync`, `ClientDisconnectedAsync`) and logs a heartbeat with memory
  information every `DelayInMilliSeconds`.
- `MqttServiceConfiguration.cs`: the configuration type, `Port`, `TlsPort`, `DelayInMilliSeconds`
  and the list of `Users`, each with defaults. `User.cs`: user name and password, both plain text.
- `LoggerConfig.cs`: builds the per-component `LoggerConfiguration` that `MqttService` writes into.
- `GlobalUsings.cs`: all usings of the project, including the alias `ILogger`.
- `appsettings.json` and `appsettings.Development.json`: the shipped example configuration with the
  user `Hans` and the password `Test`.
- `Dockerfile` and `Dockerfile.armv7`: both copy an already published `publish` folder into the
  image, they do not build the project themselves.

Repository root: `README.md` (badges and links), `HowToUse.md` (the only real user documentation,
JSON sample and the whole Docker walkthrough), `Changelog.md`, `License.txt` (MIT), the three build
scripts `buildAndUploadDocker.bat`, `buildAndUploadDockerForArm.bat` and `buildForWindows.bat`,
`Published/<version>/publish.zip` for the releases up to 1.0.8, `.gitattributes` and `.gitignore`.
Code conventions
live in `src/.editorconfig`, there is no `.editorconfig` in the root. There is no `Updating.md` and
no screenshots.

## Build

```powershell
dotnet build src/SimpleMqttServer.sln -c Release
```

There are no tests, so there is nothing to run with `dotnet test`. A behaviour change is verified by
starting the built binary and looking at the log, and by connecting an MQTT client against
`localhost:1883` with the configured user.

- Single target framework `net10.0`, no multi-targeting, no `RuntimeIdentifiers` in the project file.
  The runtime identifier is passed on the command line by the build scripts (`linux-x64`,
  `linux-arm`, `win-x64`).
- All build properties live directly in `src/SimpleMqttServer/SimpleMqttServer.csproj`. There is
  **no** `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.9-1` for the first
  commit after tag `1.0.8`. Never edit a version property or an assembly version by hand.
- Restore needs nuget.org. If a private feed is configured globally on the machine and is
  unreachable, restore fails with `NU1900` ("Warnung als Fehler") because the audit cannot fetch its
  vulnerability data. Then build with an explicit source:
  `dotnet build src/SimpleMqttServer.sln -c Release --source https://api.nuget.org/v3/index.json`.
  For `dotnet list package --outdated` the flag alone is not enough, restore separately first and
  add `--no-restore`.

## Code conventions

Follow the surrounding code, it is consistent throughout every file:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions.
  Overrides of `BackgroundService` members carry `<inheritdoc cref="BackgroundService"/>`.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the namespace
  (`csharp_using_directive_placement=inside_namespace:warning`), which global usings cannot satisfy,
  that is what the pragma is for. The comment text in that block is German because Visual Studio
  generated it, leave it alone.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`). Static members are the exception, they
  are used unqualified.
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.
- Log messages use Serilog message templates with named placeholders, never string interpolation
  into the template.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **The TLS port is configured but TLS is never enabled.** `StartMqttServer` calls
  `WithEncryptedEndpointPort(this.MqttServiceConfiguration.TlsPort)` without
  `WithEncryptedEndpoint()` and without a certificate, and MQTTnet only opens the encrypted endpoint
  when it is explicitly enabled. The server therefore listens on `Port` only, while `appsettings.json`,
  `HowToUse.md` and the `docker run` sample all mention 8883. Enabling it would need a certificate
  and a configuration option for it.
- **`IsValid` never returns `false`.** `MqttServiceConfiguration.IsValid` returns `bool`, but every
  failing branch throws an `Exception` instead of returning `false`. The `if (!IsValid())` in
  `MqttService.StartAsync` and the `throw` inside it are therefore unreachable. The caller behaves
  correctly anyway because the exceptions propagate out of `StartAsync`.
- **Passwords are written to the log.** `LogMessage(ValidatingConnectionEventArgs, bool)` has a
  `showPassword` parameter, and every failed connection attempt passes `true`. A wrong or mistyped
  password ends up in the console log in plain text. Passwords are stored in plain text in
  `appsettings.json` as well, the whole authentication is plain text by design.
- **`Startup.Configure` resolves the service for its side effect.**
  `_ = app.ApplicationServices.GetService<MqttService>();` looks pointless because the return value
  is discarded, but it forces the singleton to be created. `services.AddSingleton<IHostedService>`
  registers the same instance, so the host starts the one that was already built.
- **The Orleans log level override.** `Program.SetupLogging` overrides the minimum level for
  `Orleans`, and it overrides `Microsoft` a second time. Orleans is not referenced anywhere in this
  repository, the line is a leftover from the template these services share. Harmless, an override
  for an unused source does nothing.
- **`appsettings.Development.json` is tracked although `.gitignore` lists it.** The file was added
  before the ignore rule, and git keeps tracking a file it already knows. Changes to it show up in
  `git status` like any other tracked file. Do not "fix" this by deleting it, the Docker sample and
  the Development environment rely on it.
- **The web host serves nothing.** The project uses `Microsoft.NET.Sdk.Web` and calls `AddMvc`,
  `AddRazorPagesOptions` and `UseRouting`, but there is no controller, no Razor page and no
  `MapControllers`, so the HTTP endpoint answers 404 for everything. The web host is there for the
  hosting model, the request logging and `ASPNETCORE_URLS`, not for an API. The `Dockerfile` still
  sets `ASPNETCORE_URLS=http://*:5000`.
- **The Dockerfiles do not build.** Both start from `mcr.microsoft.com/dotnet/aspnet` and only
  `COPY publish .`, so `dotnet publish` has to run first. That is what the two
  `buildAndUploadDocker*.bat` scripts do. Building the image from a clean checkout fails.
- **The image version lives in the batch files.** `buildAndUploadDocker.bat` and
  `buildAndUploadDockerForArm.bat` contain the image tag as a literal in two places each, the build
  and the push. Bumping a release means editing both files. The scripts also expect
  `DOCKERHUB_CLI_TOKEN` in the environment and log in with `--password-stdin`, so the token never
  reaches a command line or the console. Do not remove the `@ECHO OFF` in the first line, without it
  cmd echoes the login line with the token expanded.
- **The ARM build needs `--platform linux/arm/v7` on the command line.** The `--platform` in the
  `FROM` of `Dockerfile.armv7` alone is not enough. BuildKit writes the platform of the *build
  request* into the manifest index, so without the flag the pushed index claims `linux/amd64` while
  the image config says `arm/v7`, and `docker pull` on an armv7 device finds no matching manifest.
  The image itself is fine either way, only the index entry lies.
- **The scripts delete `publish` before publishing.** `dotnet publish --output publish/` does not
  clean, so without the `RD /S /Q` the output of the previous runtime identifier stays in the folder
  and ends up in the image. A `win-x64` publish followed by a `linux-x64` one used to put
  `SimpleMqttServer.exe` and `web.config` into the Linux image.
- **`Published/` holds the releases up to 1.0.8 only.** Those versions keep their own `publish.zip`
  of the framework dependent `win-x64` publish in the repository, including the `.pdb`, roughly
  800 KB each, forever in the git history. From 1.0.9 on the zip is a release asset instead, see
  Releasing. The old folders stay where they are, deleting them would not shrink the history.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. `.github/workflows` exists in the working tree but is empty
  and untracked, there is no pipeline file here.
- **`.gitattributes` sets `* text=auto`** and every rule of the Visual Studio template below it is
  commented out. The publish zips are safe because git detects binary content by heuristic, but
  unlike the sibling repositories there is no explicit rule. A new binary file with text like
  content needs its own `binary` rule.
- **`src/SimpleMqttServer.sln.DotSettings`** is tracked and holds nothing but a ReSharper user
  dictionary. It still contains `Cryptor` and `Haemmer` from a sibling repository. Leave it alone.

## Releasing

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.9.0 (2026-08-17)** : Short description.`
3. Bump the image tag in `buildAndUploadDocker.bat` and `buildAndUploadDockerForArm.bat`, two
   occurrences each.
4. Commit that.
5. Tag the commit with the plain version number, no `v` prefix (`1.0.9`, `1.0.8`, ...). The existing
   tags are lightweight tags, create new ones the same way.
6. Push the commit and the tag.
7. Only now build the artifacts, because GitVersion takes the version from the tag. An untagged
   commit produces something like `1.0.10-1+Branch.master.Sha...` and burns that into the binary.
   - `buildForWindows.bat`, then zip `src/SimpleMqttServer/publish` into a `publish.zip`. The zip
     contains the `publish` folder itself, not its contents at the root, and it keeps the `.pdb`.
   - `buildAndUploadDocker.bat` and `buildAndUploadDockerForArm.bat` for the two images.
8. Create the GitHub release for the tag and attach that `publish.zip` to it. **The zip does not go
   into the repository any more.** With the `gh` CLI that is `gh release create 1.0.9 publish.zip`,
   otherwise `POST /repos/SeppPenner/SimpleMqttServer/releases` and then the upload to
   `uploads.github.com`. The token for that comes from the Windows credential manager, the same
   one `git push` uses: pipe a `protocol=https` and `host=github.com` block into
   `git credential fill` and read the `password=` line. The release body names the changelog
   line and the two image tags.

The version in the `Changelog.md` has four parts (`1.0.9.0`), the tag and the Docker image tag have
three (`1.0.9`). There is no installer to build and no package to push, so the release ends with the
uploaded asset. Nothing is committed after the tag any more, the tagged commit is the release.

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation.
- **No em dashes or en dashes** (`—`, `–`), neither in prose, commit messages, code comments nor
  documentation. Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
