using System.Collections;

namespace VRT.TextToSpeech.Wpf.Extensions
{
    public static class CollectionsExtensions
    {
        public static IEnumerable<object> AsObjectEnumerable(this IList list)
        {
            if (list == null) yield break;
            foreach (var item in list)
            {
                yield return item;
            }
        }
    }
}
