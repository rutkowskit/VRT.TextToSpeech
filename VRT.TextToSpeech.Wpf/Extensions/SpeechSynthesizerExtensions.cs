// based on answer from here: https://stackoverflow.com/questions/51811901/speechsynthesizer-doesnt-get-all-installed-voices-3
using System.Collections;
using System.Reflection;

namespace VRT.TextToSpeech.Wpf.Extensions;

public static class SpeechSynthesizerExtensions
{
    private static VoiceInfo[]? VoiceInfosCache;
    private static readonly Assembly SpeechSynthesizerAssembly = typeof(SpeechSynthesizer).Assembly;
    private static readonly Type? ObjectTokenCategoryType = SpeechSynthesizerAssembly
        .GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory");

    public static SpeechSynthesizer InjectOneCoreVoices(this SpeechSynthesizer synthesizer)
    {
        GetVoiceSynthesizer(synthesizer).Tap(voiceSynthesizer =>
        {
            GetOneCoreVoiceInfos()
                .Map(voiceInfos => voiceInfos.Select(vi => ToInstalledVoice(voiceSynthesizer, vi)))
                .Tap(newVoices => AddInstalledVoices(voiceSynthesizer, newVoices));
        });        
        return synthesizer;
    }

    private static void AddInstalledVoices(object voiceSynthesizer, IEnumerable<InstalledVoice> newVoices)
    {
        if (newVoices == null || GetField(voiceSynthesizer, "_installedVoices") is not IList<InstalledVoice> installedVoices)
        {
            return;
        }
        foreach (var voice in newVoices)
        {
            installedVoices.Add(voice);
        }
    }

    private static InstalledVoice ToInstalledVoice(object voiceSynthesizer, VoiceInfo voiceInfo)
    {
        var installedVoice = SpeechSynthesizerAssembly.CreateInstance(
            typeof(InstalledVoice).FullName!, true, BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { voiceSynthesizer, voiceInfo }, null, null) as InstalledVoice;
        return installedVoice!;
    }
    private static Result<VoiceInfo[]> GetOneCoreVoiceInfos()
    {
        if(VoiceInfosCache != null)
        {
            return VoiceInfosCache;
        }
        var voiceInfoTypeName = typeof(VoiceInfo).FullName!;
        var result = GetObjectTokens().Map(tokens =>
        {
            var voiceInfoList = tokens
                .Where(HasAttributesProperty)
                .Select(CreateVoiceInfo)
                .Where(vi => vi != null)
                .Select(vi => vi!)
                .ToArray();                
            return voiceInfoList;
        }).Tap(vi=> VoiceInfosCache = vi);

        return result;
    }
    private static bool HasAttributesProperty(object voiceObjectToken)
    {
        return voiceObjectToken is not null && GetProperty(voiceObjectToken, "Attributes") is not null;
    }
    private static VoiceInfo? CreateVoiceInfo(object voiceObjectToken)
    {
        var result = SpeechSynthesizerAssembly.CreateInstance(typeof(VoiceInfo).FullName!, true,
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { voiceObjectToken }, null, null) as VoiceInfo;
        return result;
    }
    private static Result<IEnumerable<object>> GetObjectTokens()
    {        
        var result = CreateObjectTokenCategory().Map(otc =>
        {
            using (otc)
            {
                if (ObjectTokenCategoryType
                    ?.GetMethod("FindMatchingTokens", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(otc, new object?[] { null, null }) is not IList tokens)
                    {
                        return Array.Empty<object>().AsEnumerable();
                    }
                return tokens.AsObjectEnumerable();
            }            
        });
        return result;
    }
    private static Result<IDisposable> CreateObjectTokenCategory()
    {
        const string SpeechOneCoreRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";
        if (ObjectTokenCategoryType
            ?.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)
            ?.Invoke(null, new object?[] { SpeechOneCoreRegistryKey }) is not IDisposable result)
        {
            return Result.Failure<IDisposable>("Unable to crate Object Token Category Instance");
        }
        return Result.Success(result);
    }
    private static Result<object> GetVoiceSynthesizer(SpeechSynthesizer synthesizer)
    {        
        var result = GetProperty(synthesizer, "VoiceSynthesizer");
        return result ?? Result.Failure<object>("Voice Synthesizer property not exists");
    }
    
    private static object? GetProperty(object target, string propName)
    {
        return target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }

    private static object? GetField(object target, string propName)
    {
        return target.GetType().GetField(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }   
}
