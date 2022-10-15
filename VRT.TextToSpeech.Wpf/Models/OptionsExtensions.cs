namespace VRT.TextToSpeech.Wpf.Models;
internal static class SpeechOptionsExtensions
{
    public static Result<SpeechOptions> HaveCorrectReadingOptions(this SpeechOptions options)
    {
        if (options.Voice is null)
            return Result.Failure<SpeechOptions>("Voice is required");
        if (string.IsNullOrWhiteSpace(options.CurrentTextToRead))
            return Result.Failure<SpeechOptions>("Text to read is required");

        if (options.OutputToAudioFile && string.IsNullOrWhiteSpace(options.OutputFileName))
        {
            return Result.Failure<SpeechOptions>("Output file name is required");
        }
        return options;
    }
}
