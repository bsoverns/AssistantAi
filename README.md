# AssistantAi

AssistantAi is a Windows desktop (WPF, .NET 8) front end for OpenAI's APIs. It is a personal sandbox for exercising new API features — chat, vision, image generation, speech-to-text and text-to-speech — from a single window. Development is ongoing, so models and defaults change as the API does.

## Features

- **Chat** against the `chat/completions` endpoint. Models offered in the drop-down: `gpt-5.4`, `gpt-5.4-mini`, `gpt-5.4-nano`, `gpt-5`, `gpt-5-mini`, `gpt-4.1`, `gpt-4.1-mini`, `gpt-4.1-nano`, `gpt-4o`, `gpt-4o-mini`, `o3`, `o3-pro`, `o3-mini`, `o4-mini`. The default is `gpt-5-mini`.
- **Saved conversations.** Every exchange is written to a local SQLite database, and the full thread is replayed as history on each send. Use the *Conversation* drop-down to switch threads, *New Chat* to start one, and *Delete Chat* to remove one. A new thread is auto-named after its first question.
- **Vision from a screenshot.** *Get Image* hides the window, captures the full screen, and attaches the PNG to your next question. *Reset Image* discards it. If the selected model can't take images, the request falls back to `gpt-5-mini`.
- **Image Review Mode.** Point *Pickup Folder* at a directory and every `.png`/`.jpg`/`.jpeg` in it is sent in one `responses` request, ordered by the first number in each filename. Raise **Max Tokens** first or the answer will be cut short.
- **Image generation.** With *Create Image* checked, your prompt goes to `images/generations` using `gpt-image-2` at 1024x1024, and the result is saved and shown inline in the output pane.
- **Speech to text.** *Translate/Transcribe Mode* records up to 30 seconds from the microphone and sends it to `audio/transcriptions` or `audio/translations` (selected in the *Whisper Model* box) using `gpt-4o-mini-transcribe`. *Continuous STT Mode* loops in 5-second chunks until you switch it off. Silent clips are discarded locally instead of being uploaded.
- **Text to speech.** Uncheck *Mute Voice* to have replies spoken back, or check *Text To Speech Only* to just read your own text aloud. Models: `tts-1`, `tts-1-hd`, `gpt-4o-mini-tts`. Thirteen voices are available; `onyx` is the default.
- **Output rendering.** Fenced ```` ``` ```` blocks are shown as syntax-coloured code, and generated images are drawn inline.
- **API key management.** *Update API Key* opens a dialog that tests a new key against the API before it will let you save it.
- **API status light** polled every 30 seconds (see below).

## Requirements

- Windows (x64), .NET 8 SDK — the project targets `net8.0-windows8.0` and uses WPF.
- An OpenAI API key with billing enabled.

NuGet packages: `Newtonsoft.Json`, `NAudio`, `Microsoft.Data.Sqlite`, `HtmlAgilityPack`.

## Installation

1. Clone the repository.
2. Build the solution in Visual Studio, or from the command line with `dotnet build AssistantAi.sln -c Release`.
3. On first launch you'll be prompted for your OpenAI API key. It is saved to `Files\ApiKey.json` beside the executable and reused on every later run. Change it with the *Update API Key* button, or delete that file to be prompted again.

Alternatively, download the latest build from the top (main) workflow run on the [Actions page](https://github.com/bsoverns/AssistantAi/actions) instead of building it yourself.

## Where files are written

Everything lives under a `Files` folder next to the executable:

| Path | Contents |
| --- | --- |
| `Files\ApiKey.json` | Your OpenAI key |
| `Files\conversations.db` | SQLite database of saved chats |
| `Files\Images\Captures` | Screenshots taken with *Get Image* |
| `Files\Images\Creations` | Generated images |
| `Files\Sound recordings\Recordings` | Microphone captures (deleted after transcription) |
| `Files\Sound recordings\Speech` | TTS audio (deleted after playback) |
| `Files\ErrorLogs` | Error logs, foldered by year and date |

## Project layout

| Folder | Role |
| --- | --- |
| `Services/` | All OpenAI network calls — chat/vision, audio, images, status, plus the shared HTTP client |
| `Models/` | Model catalog, defaults, file paths, pricing table, chat message type |
| `Helpers/` | Screen capture, image encoding, audio silence detection, token estimation, output rendering, error logging |
| `Classes/` | Audio recorder, conversation database, log writer, API key config |
| `Controls/` | Spinner control |
| `MainWindow.xaml(.cs)` | View layer only: wires controls to the services above |

## API Status Indicators

- **Red**: API is currently down, degraded, or the status page could not be read.
- **Yellow**: Internet connectivity issues detected.
- **Green**: API should be fully operational and accessible.

## Known limitations

- **Estimated Cost always reads $0.00.** The pricing table only covers gpt-3.5/gpt-4 era models, so every model currently in the drop-down falls through to zero. The token estimate itself is a rough character-count approximation.
- **Temperature and User ID are displayed but not sent** with any request yet.
- **Max Tokens** applies to vision and Image Review requests; plain chat requests don't send a limit. It is also compared against the token estimate to block a send that looks too large.
- **Realtime models** are listed in the catalog but no realtime mode is implemented.

## Support and Contributions

If you find this project helpful or inspiring, consider supporting its development. Your contributions enable the continuous improvement and creation of new and exciting features.

<img width="1395" height="1022" alt="image" src="https://github.com/user-attachments/assets/65a1ca19-b0be-428a-aada-d86e467f0264" />
