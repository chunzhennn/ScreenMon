namespace ScreenMonAPI.Messages
{
    public class ResponseMessage : IAppMessage
    {
        public required bool Success { get; set; }
        public required string Message { get; set; }
    }
}
