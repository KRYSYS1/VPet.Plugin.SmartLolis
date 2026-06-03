# Smart Lolis for VPet

[![Steam Workshop](https://img.shields.io/badge/Steam-Workshop-blue?logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=3699290588)

Smart Lolis is a VPet plugin that adds:

- AI chat with multiple LLM providers
- Voice input (Whisper / Groq transcription)
- Command mode for pet actions (feed, play, sleep, etc.)
- TTS playback for chat replies and speech bubbles
- In-game settings window with provider buttons, API key links, and voice presets
- Multi-language UI (Russian, English, Chinese)

## Features

- **Button-based provider selection** — switch LLM and TTS providers with a single click
- **Per-provider configuration** — each LLM provider stores its own API URL, key, and model
- **Get Key / Get Voice buttons** — one-click links to provider API key and voice ID pages
- **Preset voice lists** — Google TTS and Amazon Polly include ready-to-use voice options for Russian, English, Chinese, Japanese, and Korean

## LLM Providers

- Ollama (local)
- OpenAI
- Groq
- NVIDIA
- Google (AI Studio)
- GitHub Models
- Cohere
- Cerebras
- Mistral
- OpenRouter
- LM Studio (local)
- Custom (any OpenAI-compatible endpoint)

All OpenAI-compatible providers share a single generic request builder (`OpenAiCompatibleProvider.cs`).

## TTS Providers

- **ElevenLabs** — high-quality neural voices
- **Google Cloud Text-to-Speech** — standard and WaveNet voices (ru, en, zh, ja, ko)
- **Local Windows** — built-in system voices
- **Amazon Polly** — AWS neural and standard voices (ru, en, zh, ja, ko)

## Project structure

- `SmartLolisPlugin.cs` — plugin entry point
- `SmartLolisTalkBox.cs` — chat UI and pet interaction logic
- `SmartLolisService.cs` — LLM request orchestration and streaming
- `SmartLolisSettings.cs` — settings persistence with per-provider configs
- `SmartLolisSettingsWindow.xaml` / `.xaml.cs` — settings UI with localization
- `ElevenLabsTtsService.cs` — ElevenLabs TTS integration
- `Providers/OpenAiCompatibleProvider.cs` — generic OpenAI-compatible LLM provider
- `lang/` — localized mod metadata (ru, en, zh)
- `scripts/` — build and deploy scripts
- `artifacts/` — local build output (not committed)

## Build

```powershell
dotnet restore .\VPet.Plugin.SmartLolis.csproj
dotnet build .\VPet.Plugin.SmartLolis.csproj -c Release
```

## Deploy to a local Steam VPet installation

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-SmartLolis.ps1 -Configuration Release -Platform x64 -DeployToSteam
```

## Notes

- Keep API keys out of the repository.
- Do not commit `artifacts/`, editor settings, or local machine-specific files.
- If you want to publish this project on GitHub, this folder is the right source folder to use.

## License

MIT
