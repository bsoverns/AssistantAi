# AssistantAi
WPF Program designed to interact with a user and OpenAI endpoints.  Please keep in mind this is still a work in progress.

The program allows you to perform the following:
1. Interact with standard ChatGpt3.5 and ChatGpt4
2. Interact with Whisper Transcribe, Translate, and Speech.
3. Interact with Vision.

Install Steps:
1. Clone project.
2. Build project.
3. Add OpenApi key on startup.
  a. The key is stored locally in a file called "ApiKey.json".
  b. From that point forward when you start the program it will never ask for the key again.
  c. If you need to remove or update the key, you need to remove the "ApiKey.json" file.

If this helps you in any way please donate if possible.  I would like to create more programs like this and improve this one.

Or you can download current version from the build artifact [AssistantArtifact] <= Incomplete for now.
Api Status Lights:

Red - Api is down.

Yellow - Internet is down\Unable to access internet.

Green - Api is up.

![image](https://github.com/bsoverns/AssistantAi/assets/12473875/f1f20804-c696-4dbc-8362-bdc0354273cc)
