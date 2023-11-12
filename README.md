# AssistantAi
WPF Program designed to interact with a user and OpenAI endpoints.  

The program allows you to perform the following:
1. Interact with standard ChatGpt3.5 and ChatGpt4
2. Interact with Whisper Transcribe, Translate, and Speech.

Install Steps:
1. Clone project.
2. Build project.
3. Add OpenApi key on startup.
  a. The key is stored locally in a file called "ApiKey.json".
  b. From that point forward when you start the program it will never ask for the key again.
  c. If you need to remove or update the key, you need to remove the "ApiKey.json" file.
