using System.Reactive.Disposables;
using System.Text.RegularExpressions;

namespace VRT.TextToSpeech.Wpf;

public sealed class MainWindowViewModel : BaseViewModel
{    
    private SpeechSynthesizer? _synthesizer;
    private readonly List<string> _readWords;
    public MainWindowViewModel()
    {        
        _readWords = new();
        CurrentRate = 2;
        StartReadingCommand = ReactiveCommand.Create(StartReading, CanStartReading());
        PauseReadingCommand = ReactiveCommand.Create(PauseReading, CanPauseReading());
        StopReadingCommand = ReactiveCommand.Create(StopReading, CanStopReading());
        LoadVoicesCommand = ReactiveCommand.CreateFromTask(LoadVoices);
        LoadVoicesCommand.Execute(System.Reactive.Unit.Default);

        this.WhenAnyValue(p => p.SelectedVoice, r => r.CurrentRate)
            .ObserveOnDispatcher()
            .Where(_ => State != SpeechStates.Idle)
            .Throttle(TimeSpan.FromMilliseconds(1200))            
            .Subscribe(s => RestartSpeechEngineIfInProgress())
            .DisposeWith(Disposables);            
    }

    [Reactive]
    public int CurrentRate { get; set; }
    [Reactive]
    public string? CurrentTextToRead { get; set; }    
    [Reactive]
    public SpeechStates State { get; set; }    
    [Reactive]
    public IReadOnlyCollection<BasicVoiceInfo>? Voices { get; set; }
    [Reactive]
    public BasicVoiceInfo? SelectedVoice { get; set; }
    [Reactive]
    public int CarretIndex { get; set; }

    public ICommand StartReadingCommand { get; }
    public ICommand PauseReadingCommand { get; }
    public ICommand StopReadingCommand { get; }
    public ICommand LoadVoicesCommand { get; }

    public Task LoadVoices()
    {
        Voices = CreateSynthesizer()
            .GetInstalledVoices()
            .Where(e => e.Enabled)
            .Select(s => new BasicVoiceInfo(s.VoiceInfo.Name, s.VoiceInfo.Culture.Name))
            .OrderBy(s => s.CultureName)
            .ThenBy(s => s.Name)
            .ToArray();  
        return Task.CompletedTask;
    }

    private void StartReading()
    {        
        if (_synthesizer != null && State == SpeechStates.Paused)
        {
            _synthesizer.Resume();
            State = SpeechStates.InProgress;
        }
        else
        {            
            DoPrompt(SelectedVoice!.Name, CurrentTextToRead!);
        }        
    }

    private int CalculateProcessedTextOffset()
    {
        var words = _readWords.SkipLast(1).ToArray();
        var text = CurrentTextToRead ??"";
        var result = 0;
        var machedWords = new List<string>();
        foreach (var w in words)
        {
            var match = Regex.Match(text, w);
            if(match.Success == false || match.Index > 10)
            {                
                break;
            }
            machedWords.Add(w);
            var toSkipLocal = match.Index + match.Length;
            result += toSkipLocal;
            text = text[toSkipLocal..];
        }
        _readWords.Clear();
        _readWords.AddRange(machedWords);
        return result;
    }

    private IObservable<bool> CanStartReading()
    {
        return this.WhenAnyValue(
            p => p.State,            
            p => p.SelectedVoice,
            p => p.CurrentTextToRead,
            (state, voice, textToRead) =>
            {
                return state == SpeechStates.Paused || (state == SpeechStates.Idle
                    && voice !=null
                    && string.IsNullOrWhiteSpace(textToRead) == false);
            })
            .ObserveOnDispatcher();
    }

    private void PauseReading()
    {
        if (_synthesizer != null && State == SpeechStates.InProgress)
        {
            _synthesizer.Pause();
            State = SpeechStates.Paused;            
        }
    }
    private IObservable<bool> CanPauseReading()
    {
        return this
            .WhenAnyValue(p => p.State, (state) => state == SpeechStates.InProgress)
            .ObserveOnDispatcher();
    }
    private void StopReading()
    {
        if (_synthesizer != null)
        {
            _synthesizer.SpeakAsyncCancelAll();
            _readWords.Clear();
        }
    }
    private IObservable<bool> CanStopReading()
    {
        return this
            .WhenAnyValue(p => p.State, 
                (state) => state == SpeechStates.InProgress || state == SpeechStates.Paused)
            .ObserveOnDispatcher();
    }
    private void RestartSpeechEngineIfInProgress()
    {
        if (State == SpeechStates.InProgress)
        {            
            DoPrompt(SelectedVoice!.Name, CurrentTextToRead!);
        }
    }

    private void DoPrompt(string voice, string textToRead)
    {
        var synth = InitSynthesizer();
        var charsToSkip = CalculateProcessedTextOffset();
        var textLeftToRead = charsToSkip < textToRead.Length
            ? textToRead[charsToSkip..]
            : textToRead;                
        synth.SelectVoice(voice);
        synth.SpeakAsync(textLeftToRead);
        State = SpeechStates.InProgress;
    }
    private SpeechSynthesizer InitSynthesizer()
    {
        CleanupSynthesizer();
        _synthesizer = CreateSynthesizer();        
        _synthesizer.SpeakCompleted += OnSpeekCompleted;
        _synthesizer.SpeakProgress += OnSpeakProgress;
        return _synthesizer;
    }
    private void CleanupSynthesizer()
    {
        var engine = _synthesizer;
        if (engine != null)
        {
            engine.SpeakCompleted -= OnSpeekCompleted;
            engine.SpeakProgress -= OnSpeakProgress;
            engine.Dispose();            
            _synthesizer = null;            
            State = SpeechStates.Idle;
        }
    }
    private void OnSpeakProgress(object? sender, SpeakProgressEventArgs e)
    {
        _readWords.Add(e.Text);
    }

    private void OnSpeekCompleted(object? sender, SpeakCompletedEventArgs e)
    {
        CleanupSynthesizer();
        _readWords.Clear();
    }    
    private SpeechSynthesizer CreateSynthesizer()
    {        
        var result = new SpeechSynthesizer()
        {
            Rate = CurrentRate,
            Volume = 100
        }.InjectOneCoreVoices();        
        return result;
    }
}
