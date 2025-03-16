using Deepgram.Models.Speak.v1.REST;
using Deepgram;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NAudio.Wave;

namespace SampleApp
{
    class Program
    {
#pragma warning disable SKEXP0070

        // Method to play an MP3 file using NAudio
        static void PlayAudio(string filePath)
        {
            using (var audioFile = new AudioFileReader(filePath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                // Wait until playback completes
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }
        }

        static async Task Main(string[] args)
        {
            ChatHistory history = new ChatHistory();

            var modelID = "mistral-large-latest model";
            var apiKey = " ";
            var builder = Kernel.CreateBuilder();

            builder.AddMistralChatCompletion(modelID, apiKey);
            Kernel kernel = builder.Build();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            while (true)
            {
                Console.WriteLine("Prompt:");
                history.AddUserMessage(Console.ReadLine());
                var content = chatCompletionService.GetStreamingChatMessageContentsAsync(history, kernel: kernel);

                string fullMessage = "";

                await foreach (var chunk in content)
                {
                    Console.Write(chunk.Content);
                    fullMessage += chunk.Content;
                }
                history.AddAssistantMessage(fullMessage);
                Console.WriteLine();

                Library.Initialize();
                try
                {
                    var deepgramClient = ClientFactory.CreateSpeakRESTClient(" ");

                    // Convert text to speech and save it as an MP3 file (non-streaming)
                    string outputPath = "output.mp3";
                    await deepgramClient.ToFile(
                        new TextSource(fullMessage),
                        outputPath,
                        new SpeakSchema()
                        {
                            Model = "aura-luna-en",
                        }
                    );

                    Console.WriteLine($"Audio saved to {outputPath}. Playing now...");
                    // Play the generated MP3 file
                    PlayAudio(outputPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    Library.Terminate();
                }
            }
        }
    }
}
