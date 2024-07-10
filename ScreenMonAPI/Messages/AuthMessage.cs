namespace ScreenMonAPI.Messages
{
    public class AuthMessage : IAppMessage
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string ClientMac { get; set; }
    }
}
