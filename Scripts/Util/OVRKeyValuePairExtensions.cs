using System.Collections.Generic;

namespace Util {
    public static class OVRKeyValuePairExtensions {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair,
            out TKey key,
            out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}