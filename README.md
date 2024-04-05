# AssistantAi

AssistantAi is a WPF application designed for interactive communication with OpenAI's various endpoints. The application is currently under continuous development, evolving to test the new features and enhancements of the API.

## Features

AssistantAi offers a versatile range of functionalities, including:

1. Interaction with OpenAI's ChatGPT models (3.5 and 4).
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

## Support and Contributions

If you find this project helpful or inspiring, consider supporting its development. Your contributions enable the continuous improvement and creation of new and exciting features.

For the latest stable version, download the build from the latest workflow under the action page: https://github.com/bsoverns/AssistantAi/actions.

## API Status Indicators

- **Red**: API is currently down or performance is degraded.
- **Yellow**: Internet connectivity issues detected.
- **Green**: API should be fully operational and accessible.

![image](https://github.com/bsoverns/AssistantAi/assets/12473875/39635100-4a0f-4b4e-b59f-76e9630aff90)




