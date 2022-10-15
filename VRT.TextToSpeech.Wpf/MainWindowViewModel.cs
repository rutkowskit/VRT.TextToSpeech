using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reactive.Disposables;
using System.Windows.Threading;
using VRT.TextToSpeech.Wpf.ReadingStates;

namespace VRT.TextToSpeech.Wpf;

internal sealed partial class MainWindowViewModel : BaseViewModel, IReadingStateContext
{
    public MainWindowViewModel()
    {
        LoadVoicesCommand.Execute(System.Reactive.Unit.Default);

        this.WhenAnyValue(p => p.Options.Voice, p => p.Options.CurrentRate)
            .Where(_ => State is not null && State.CanPauseReading)
            .Throttle(TimeSpan.FromMilliseconds(1200))
            .ObserveOn(Dispatcher.CurrentDispatcher)
            .Subscribe(s => State.PauseReading().Bind(() => State.StartReading()))
            .DisposeWith(Disposables);

        TransitionToState(new IdleReadingState(this));
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartReadingCommand), nameof(PauseReadingCommand), nameof(StopReadingCommand))]
    private SpeechOptions _options = new();

    [ObservableProperty]
    private string? _errorMessage = null!;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartReadingCommand), nameof(PauseReadingCommand), nameof(StopReadingCommand))]
    private BaseReadingState _state = null!;

    [ObservableProperty]
    public IReadOnlyCollection<BasicVoiceInfo>? _voices;

    [RelayCommand(AllowConcurrentExecutions = false)]
    public Task LoadVoices()
    {
        Voices = new SpeechSynthesizer()
            .InjectOneCoreVoices()
            .GetInstalledVoices()
            .Where(e => e.Enabled)
            .Select(s => new BasicVoiceInfo(s.VoiceInfo.Name, s.VoiceInfo.Culture.Name))
            .OrderBy(s => s.CultureName)
            .ThenBy(s => s.Name)
            .ToArray();
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanStartReading))]
    private void StartReading() => State.StartReading();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartReadingCommand))]
    public bool _canStartReading;

    [RelayCommand(CanExecute = nameof(CanPauseReading))]
    private void PauseReading() => State.PauseReading();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PauseReadingCommand))]
    public bool _canPauseReading;

    [RelayCommand(CanExecute = nameof(CanStopReading))]
    private void StopReading() => State.StopReading();
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopReadingCommand))]
    private bool _canStopReading;

    public void TransitionToState(BaseReadingState state)
    {
        state.EnterState().Tap(() => State = state);
    }
}