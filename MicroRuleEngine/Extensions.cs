using System.Collections.Generic;

namespace MicroRuleEngine
{
    public static class Extensions
    {
        public static void AddRange<T>(this IList<T> collection, IEnumerable<T> newValues)
        {
            foreach (var item in newValues)
                collection.Add(item);
        }
    }
}