using DynamicData.Binding;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows.Threading;

namespace VRT.TextToSpeech.Wpf.ReadingStates;
internal sealed class IdleReadingState : BaseReadingState
{
    private readonly CompositeDisposable _disposables;

    public IdleReadingState(IReadingStateContext context) : base(context)
    {
        _disposables = new();

        context.Options.WhenAnyPropertyChanged()
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(_ => UpdateContextCanReadFlag())
            .DisposeWith(_disposables);           
    }

    private void UpdateContextCanReadFlag()
    {
        var result = Context.Options.HaveCorrectReadingOptions();
        Context.CanStartReading = result.IsSuccess;        
    }

    public override bool CanStartReading => true;
    public override Result StartReading()
    {
        var result = Result.Success()
            .Ensure(HasTextToRead)
            .Ensure(HasVoiceToUse)
            .Map(() => new InProgressReadingState(Context, Context.Options.CurrentTextToRead!, Context.Options.Voice!.Name))
            .Tap(newState => Context.TransitionToState(newState))
            .Bind(newState => newState.StartReading())
            .Tap(() => _disposables.Dispose())
            .OnFailure(error => Context.ErrorMessage = error);
        return result;
    }
}
