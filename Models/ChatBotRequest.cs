namespace QLDA.Models
{
    public class ChatBotRequest
    {
        public int ProjectId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
