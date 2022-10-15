namespace VRT.TextToSpeech.Wpf.ReadingStates;
internal abstract class BaseReadingState
{
    private static readonly Result OperationNotAvailable
        = Result.Failure("Operation is not available in this state");

    protected IReadingStateContext Context { get; }
    protected BaseReadingState(IReadingStateContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual Result StartReading() => OperationNotAvailable;
    public virtual bool CanStartReading { get; }
    public virtual Result StopReading() => OperationNotAvailable;
    public virtual bool CanStopReading { get; }
    public virtual Result PauseReading() => OperationNotAvailable;
    public virtual bool CanPauseReading { get; }

    public virtual Result EnterState()
    {
        return EnsureDifferentState().Tap(() =>
        {                
            Context.CanStartReading = CanStartReading;
            Context.CanStopReading = CanStopReading;
            Context.CanPauseReading = CanPauseReading;
            Context.ErrorMessage = null;
        });
    }

    protected Result EnsureDifferentState()
    {
        return Context.State is null || Context.State.GetType() != GetType()
            ? Result.Success()
            : Result.Failure("Reading already in requested state");
    }
    protected Result TransitionToFailingState(string error)
    {
        Context.TransitionToState(new FailureReadingState(Context, error));
        return Result.Success();
    }
    protected Result TransitionToIdleState()
    {
        Context.TransitionToState(new IdleReadingState(Context));
        return Result.Success();
    }

    protected Result HandleFailure<T>(Result<T> result)
    {
        return HandleFailure((Result)result);
    }
    protected Result HandleFailure(Result result)
    {
        return result.IsSuccess
            ? result
            : TransitionToFailingState(result.Error);
    }
    protected Result HasTextToRead()
    {
        return string.IsNullOrWhiteSpace(Context.Options.CurrentTextToRead)
            ? Result.Failure("Text to read not provided")
            : Result.Success();
    }
    protected Result HasVoiceToUse()
    {
        return string.IsNullOrWhiteSpace(Context.Options?.Voice?.Name)
            ? Result.Failure("Voice to use not selected")
            : Result.Success();
    }
}
