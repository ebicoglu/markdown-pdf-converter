using Newtonsoft.Json;
namespace AbpDocsMd2PdfConverter;


public class DisplayParameterPair
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty("values")]
    public Dictionary<string, string> Values { get; set; }
}

public class DisplayParameter
{
    [JsonProperty("parameters")]
    public List<DisplayParameterPair> Values { get; set; }

    public static DisplayParameter Build()
    {
        //D:\github\volosoft\abp\docs\en\docs-params.json
        string json = @"
        {
          ""parameters"": [
            {
              ""name"": ""UI"",
              ""displayName"": ""UI"",
              ""values"": {
                ""MVC"": ""MVC / Razor Pages"",
                ""Blazor"": ""Blazor WebAssembly"",
                ""BlazorServer"": ""Blazor Server"",
                ""BlazorWebApp"": ""Blazor WebApp"",
                ""MAUIBlazor"": ""MAUI Blazor (Hybrid)"",
                ""NG"": ""Angular""
              }
            },
            {
              ""name"": ""DB"",
              ""displayName"": ""Database"",
              ""values"": {
                ""EF"": ""Entity Framework Core"",
                ""Mongo"": ""MongoDB""
              }
            },
            {
              ""name"": ""Tiered"",
              ""displayName"": ""Tiered"",
              ""values"": {
                ""No"": ""Not Tiered"",
                ""Yes"": ""Tiered""
              }
            }
          ]
        }";

        // Deserialize JSON
        return JsonConvert.DeserializeObject<DisplayParameter>(json);
    }
}
