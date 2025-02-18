[Serializable]
public class AnythingllmEntities
{
    
    public string id;
    public string @object;
    public int created;
    public string model;
    public List<Choice> choices;

    [Serializable]
    public class Choice
    {
        public int index;
        public string text;
        public object logprobs;
        public object finish_reason;
    }
}

[System.Serializable]
public class ApiJsonPayload
{
    public string model;
    public string prompt;
    public int max_tokens;
    public bool stream;
}

[System.Serializable]
public class AnythingLLMJSONPayload
{
    public string message;
    public string mode;
}

[Serializable]
public class AnythingLLMPromptResponse
{
    public string id;
    public string type;
    public bool close;
    public object error;
    public int chatId;
    public string textResponse;
    public List<Source> sources;

    [Serializable]
    public class Source
    {
        public string id;
        public string url;
        public string title;
        public string docAuthor;
        public string description;
        public string docSource;
        public string chunkSource;
        public string published;
        public int wordCount;
        public int token_count_estimate;
        public string text;
        public double _distance;
        public double score;
    }
}