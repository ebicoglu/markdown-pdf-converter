namespace AbpChatBot;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ABP AI Support Bot");
        Console.WriteLine("------------------------");

        while (true)
        {
            Console.Write("\nQuestion: ");
            var question = Console.ReadLine();

            try
            {
                var response = LlamaAPI.GetAnythingLLMNonStreaming(question);
                Console.WriteLine($"\nAnswer: {response.textResponse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }
}