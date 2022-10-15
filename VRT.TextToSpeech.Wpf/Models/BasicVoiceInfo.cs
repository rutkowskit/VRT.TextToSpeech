namespace VRT.TextToSpeech.Wpf.Models;
public sealed record BasicVoiceInfo(string Name, string CultureName)
{
    public override string ToString()
    {
        return $"{Name} - {CultureName}";
    }
    public static implicit operator string(BasicVoiceInfo info) 
        => info?.Name ?? "";
}