namespace VRT.TextToSpeech.Wpf.ReadingStates;

internal interface IReadingStateContext
{
    BaseReadingState State { get; }
    void TransitionToState(BaseReadingState state);
    SpeechOptions Options { get; }
    bool CanStartReading { set; }
    bool CanStopReading { set; }
    bool CanPauseReading { set; }
    string? ErrorMessage { set; }
}
