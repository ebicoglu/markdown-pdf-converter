using System.Net;
using Newtonsoft.Json;

namespace AbpChatBot;

public static class AnythingllmAPI
{
    //SWAGGER UI -> http://localhost:3001/api/docs/

    private static string apikey = "Z4JVGPR-Z654GYT-QQTP407-695GSN1";
    private static string agentName = "ollamaagent";

    public static AnythingLLMPromptResponse GetNonStreamingResponse(string message, string mode = "chat")
    {
        //Constructing JSON payload
        AnythingLLMJSONPayload requestPayload = new AnythingLLMJSONPayload
        {
            message = message,
            mode = mode
        };

        //Creat HTTP POST request 
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:3001/api/v1/workspace/" + agentName + "/chat");

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Bearer " + apikey;
        request.UseDefaultCredentials = true;

        // Write the JSON payload to the request body
        using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
        {
            writer.Write(JsonConvert.SerializeObject(requestPayload));
        }

        //Non streaming
        //send request and get response
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //read JSON response body
        StreamReader reader = new StreamReader(response.GetResponseStream());
        string json = reader.ReadToEnd();
        //Deserialize JSON to LlamaPromptResponse object type
        return JsonConvert.DeserializeObject<AnythingLLMPromptResponse>(json);
    }
}