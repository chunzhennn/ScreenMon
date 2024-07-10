namespace ScreenMonAPI.Messages
{
    public class FrequencyChangeMessage : IAppMessage
    {
        public required int NewFrequency { get; set; }
    }
}
