using CommunityToolkit.Mvvm.ComponentModel;

namespace VRT.TextToSpeech.Wpf.Models;
internal sealed partial class SpeechOptions : ObservableObject
{    
    [ObservableProperty]
    private BasicVoiceInfo? _voice;

    [ObservableProperty]
    private int _currentRate = 2;

    [ObservableProperty]
    private string? _currentTextToRead;

    [ObservableProperty]
    private bool _outputToAudioFile;

    [ObservableProperty]
    private string? _outputFileName;    
}
