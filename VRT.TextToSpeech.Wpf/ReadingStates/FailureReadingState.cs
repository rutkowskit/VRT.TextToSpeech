namespace VRT.TextToSpeech.Wpf.ReadingStates;
internal sealed class FailureReadingState : BaseReadingState
{
    private readonly string _errorMessage;

    public FailureReadingState(IReadingStateContext context, string? errorMessage = null)
        : base(context)
    {
        _errorMessage = errorMessage ?? "Operation failed";
    }    
    public override bool CanStopReading => true;
    public override Result StopReading()
    {        
        Context.TransitionToState(new IdleReadingState(Context));
        return Result.Success();
    }
    public override Result EnterState()
    {
        return base.EnterState()
            .Tap(() => Context.ErrorMessage = _errorMessage);
    }
}
