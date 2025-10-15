namespace AiService.Models
{
    public class ChatCompletionResponse
    {
        public List<Choice> choices { get; set; } = new();

        public class Choice
        {
            public Message message { get; set; } = new();
        }
        public class Message 
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}

