# Smart Lolis for VPet

Smart Lolis is a VPet plugin that adds:

- AI chat
- voice input
- command mode for pet actions
- TTS playback for chat replies and speech bubbles
- an in-game settings window

## Built-in providers

LLM:

- Groq
- OpenRouter

TTS:

- ElevenLabs
- Local Windows
- Google Cloud Text-to-Speech
- Amazon Polly

## Main project files

- `SmartLolisPlugin.cs`
- `SmartLolisTalkBox.cs`
- `SmartLolisService.cs`
- `ElevenLabsTtsService.cs`
- `SmartLolisSettings.cs`
- `SmartLolisSettingsWindow.xaml`
- `SmartLolisLogWindow.xaml`
- `Providers/GroqProvider.cs`

## Project structure

- `lang/` - localized mod metadata
- `Providers/` - chat provider integrations
- `scripts/` - build and deploy scripts
- `artifacts/` - local build output, not intended for source control

## Build

```powershell
dotnet restore .\VPet.Plugin.SmartLolis.csproj
dotnet build .\VPet.Plugin.SmartLolis.csproj -c Release
```

## Deploy to a local VPet installation

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-SmartLolis.ps1 -Configuration Release -Platform x64 -DeployToSteam
```

## Notes

- Keep API keys out of the repository.
- Do not commit `artifacts/`, editor settings, or local machine-specific files.
- If you want to publish this project on GitHub, this folder is the right source folder to use.
