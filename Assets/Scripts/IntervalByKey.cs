using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Scripts.Utility
{
    public static class IntervalByKey
    {
        private static readonly Dictionary<object, float> _last = new Dictionary<object, float>();

        public static void PrintInterval(object key)
        {

            float now = Time.unscaledTime; // 或 realtimeSinceStartup / Time.time
            if (_last.TryGetValue(key, out float prev))
            {
                Debug.Log($"[Interval:{key}] Δt = {now - prev:F3} s");
                _last[key] = now;
            }
            else
            {
                _last[key] = now; // 第一次见到这个 key，记录不打印
            }
        }
    }
}