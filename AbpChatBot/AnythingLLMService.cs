using System.Text;
using Newtonsoft.Json;

namespace AbpChatBot;

public class AnythingLLMService
{
    //SWAGGER UI -> http://localhost:3001/api/docs/
    private const string BaseUrl = "http://localhost:3001/api/v1/";
    private readonly string _bearerToken;
    private readonly string _chatRequestUrl;

    public AnythingLLMService(string apiKey, string workspace)
    {
        _bearerToken = "Bearer " + apiKey;
        _chatRequestUrl = BaseUrl + "workspace/" + workspace + "/chat";
    }

    public async Task<AnythingLLMPromptResponse> GetResponseAsync(string message, string mode = "chat")
    {
        try
        {
            using (var client = new HttpClient())
            {
                var payload = new AnythingLLMInput
                {
                    message = message,
                    mode = mode
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Add("Authorization", _bearerToken);
                client.DefaultRequestHeaders.Add("UseDefaultCredentials", "true");

                var response = await client.PostAsync(_chatRequestUrl, content);
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AnythingLLMPromptResponse>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());

            return new AnythingLLMPromptResponse(
            {
                error = ex,
                textResponse = ex.Message,
                close = true
            };
        }
    }
}