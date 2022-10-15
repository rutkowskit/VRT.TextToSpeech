using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace VRT.TextToSpeech.Wpf.ReadingStates;
internal sealed class InProgressReadingState : BaseReadingState
{
    private SpeechSynthesizer? _synthesizer;
    private CompositeDisposable? _disposables;
    private readonly List<string> _readWords;
    private readonly string? _textToRead;
    private readonly string _voiceToUse;

    public InProgressReadingState(IReadingStateContext context,
        string textToRead,
        string voiceToUse) : base(context)
    {
        _readWords = new();
        _textToRead = textToRead;
        _voiceToUse = voiceToUse;
    }
    public override bool CanStopReading => true;
    public override bool CanPauseReading => true;
    public override Result StartReading()
    {
        var result = DoPrompt()
            .Tap(() => Context.CanPauseReading = true)
            .Tap(() => Context.CanStopReading = true)
            .Finally(HandleFailure);
        return result;
    }
    public override Result StopReading()
    {
        var result = Result
            .Try(() => CleanupSynthesizer())
            .Finally(_ => TransitionToIdleState());
        return result;
    }
    public override Result PauseReading()
    {
        var result = Result.Success(_synthesizer)
            .Ensure(synth => synth is not null, "Synthesizer not initialized")
            .Tap(synth => synth!.Pause())
            .Map(_ => new PausedReadingState(Context, this))
            .Tap(pauseState => Context.TransitionToState(pauseState))
            .Finally(HandleFailure);
        return result;
    }

    public override Result EnterState()
    {
        var result = Result.Success()
            .Ensure(HasTextToRead)
            .Ensure(HasVoiceToUse)
            .Bind(() => base.EnterState());
        return result;
    }

    private Result DoPrompt()
    {
        var result = Result.Success()
            .Tap(() => CleanupSynthesizer())
            .Bind(() => GetOrCreteSynthesizer())
            .Bind(synth => GetTextLeftToRead().Map(text => (Synth: synth, Text: text)))
            .Tap(c => c.Synth.SelectVoice(Context.Options.Voice ?? _voiceToUse))
            .Tap(c => c.Synth.SpeakAsync(c.Text));
        return result;
    }
    private Result<string> GetTextLeftToRead()
    {
        var charsToSkip = CalculateProcessedTextOffset();
        var textLeftToRead = charsToSkip < _textToRead!.Length
            ? _textToRead[charsToSkip..]
            : _textToRead;
        return textLeftToRead.Length > 0
            ? textLeftToRead
            : Result.Failure<string>("Nothing left to read");
    }
    private int CalculateProcessedTextOffset()
    {
        var words = _readWords.SkipLast(1).ToArray();
        var text = _textToRead ?? "";
        var result = 0;
        var machedWords = new List<string>();
        foreach (var w in words)
        {
            var match = Regex.Match(text, w);
            if (match.Success == false || match.Index > 10)
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

    private Result<SpeechSynthesizer> GetOrCreteSynthesizer()
    {
        _synthesizer ??= InitSynthesizer();
        return _synthesizer;
    }

    private SpeechSynthesizer InitSynthesizer()
    {
        var result = CreateSynthesizer();
        result.SpeakCompleted += OnSpeekCompleted;
        result.SpeakProgress += OnSpeakProgress;
        return result;
    }

    private void CleanupSynthesizer()
    {
        var engine = _synthesizer;
        if (engine != null)
        {
            engine.SpeakCompleted -= OnSpeekCompleted;
            engine.SpeakProgress -= OnSpeakProgress;
            if (engine.State == SynthesizerState.Speaking)
            {
                engine.SpeakAsyncCancelAll();
            }
            engine.Dispose();
            _synthesizer = null;
        }
        _disposables?.Dispose();
        _disposables = null;
    }
    private void OnSpeakProgress(object? sender, SpeakProgressEventArgs e)
    {
        _readWords.Add(e.Text);
    }

    private void OnSpeekCompleted(object? sender, SpeakCompletedEventArgs e)
    {
        CleanupSynthesizer();
        _readWords.Clear();
        var newState = new IdleReadingState(Context);
        Context.TransitionToState(newState);
    }
    private SpeechSynthesizer CreateSynthesizer()
    {
        var result = new SpeechSynthesizer()
        {
            Rate = Context.Options.CurrentRate,
            Volume = 100
        }.InjectOneCoreVoices();
        if (Context.Options.OutputToAudioFile
            && string.IsNullOrWhiteSpace(Context.Options.OutputFileName) is false)
        {
            result.SetOutputToWaveFile(Context.Options.OutputFileName);
        }
        return result;
    }
}
