namespace ScreenMonAPI.Messages
{
    public class ImageMessage : IAppMessage
    {
        public required byte[] ImageData { get; set; }
        public required DateTime Timestamp { get; set; }
    }
}
