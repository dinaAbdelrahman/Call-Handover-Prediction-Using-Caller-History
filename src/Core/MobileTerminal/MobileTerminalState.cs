namespace VerticalHandoverPrediction.Mobile
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    //[JsonConverter(typeof(StringEnumConverter))]
    public enum MobileTerminalState
    {
        Idle,
        Voice,
        Video,
        Data,
        VoiceVideo,
        VideoData,
        VoiceData,
        VoiceDataVideo,
    }
}