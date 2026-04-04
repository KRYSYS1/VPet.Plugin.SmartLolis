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
