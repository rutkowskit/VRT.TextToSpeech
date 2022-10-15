namespace VRT.TextToSpeech.Wpf.ReadingStates;
internal sealed class PausedReadingState : BaseReadingState
{
    private readonly InProgressReadingState _inProgressState;

    public PausedReadingState(IReadingStateContext context, InProgressReadingState inProgressState)
        : base(context)
    {
        _inProgressState = inProgressState ?? throw new ArgumentNullException(nameof(inProgressState));
    }
    public override bool CanStartReading => true;
    public override bool CanStopReading => true;
    public override Result StartReading()
    {
        return _inProgressState.StartReading()
            .Tap(() => Context.TransitionToState(_inProgressState));            
    }
    public override Result StopReading()
    {
        return _inProgressState.StopReading()
            .Bind(() => TransitionToIdleState());
    }
}
