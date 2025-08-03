# AssistantAi

AssistantAi is a WPF application designed for interactive communication with OpenAI's various endpoints. The application is currently under continuous development, evolving to test the new features and enhancements of the API.

## Features

AssistantAi offers a versatile range of functionalities, including:

1. Interaction with OpenAI's ChatGPT models (3.5, 4, 4o, and 4o mini).
2. Usage of Whisper for transcription, translation, and speech processing.
3. Integration with OpenAI's Vision capabilities.
4. Ability to generate images using OpenAI's DALL-E.
5. Ability to use "Image Review Mode" to select a folder of PNG images and ask questions about them.  You will need to update the Max Tokens to ensure you get a full answer.

## Installation Guide

To set up AssistantAi, please follow these steps:

1. Clone the repository to your local machine.
2. Build the project using your preferred IDE or command line tools.
3. On first launch, add your OpenAI API key. This key is crucial for the application's interaction with OpenAI services.
   - The key will be stored in a local file named "ApiKey.json".
   - Once set up, the application will automatically use this key for all future sessions.
   - To change or remove the key, simply delete the "ApiKey.json" file.
4. Alternatively you can download the latest stable build from the top workflow (main) under the action page: https://github.com/bsoverns/AssistantAi/actions.

## Support and Contributions

If you find this project helpful or inspiring, consider supporting its development. Your contributions enable the continuous improvement and creation of new and exciting features.

## API Status Indicators

- **Red**: API is currently down or performance is degraded.
- **Yellow**: Internet connectivity issues detected.
- **Green**: API should be fully operational and accessible.

<img width="1395" height="1022" alt="image" src="https://github.com/user-attachments/assets/65a1ca19-b0be-428a-aada-d86e467f0264" />
