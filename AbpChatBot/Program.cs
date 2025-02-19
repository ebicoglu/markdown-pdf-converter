namespace AbpChatBot;

public class Program
{
    //TODO: Change the API key and workspace name
    private const string ApiKey = "5J6SBV9-90NMGTT-MTDNHKH-Z2CZ2ET";
    private const string Workspace = "abpio_agent";

    static async Task Main(string[] args)
    {
        Console.WriteLine("ABP AI Support Bot");
        Console.WriteLine("------------------------");

        var anythingLlmService = new AnythingLLMService(ApiKey, Workspace);

        while (true)
        {
            Console.Write("\nQuestion: ");
            var question = Console.ReadLine() + string.Empty;

            try
            {
                var response = await anythingLlmService.GetResponseAsync(question);
                Console.WriteLine($"\nAnswer: {response.textResponse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }
}