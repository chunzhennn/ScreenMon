namespace ScreenMonAPI.Messages
{
    public class RegisterMessage : IAppMessage
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
