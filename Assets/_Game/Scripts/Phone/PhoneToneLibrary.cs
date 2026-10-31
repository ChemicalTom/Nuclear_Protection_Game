using System.Collections.Generic;
using NuclearProtection.Config;
using UnityEngine;

namespace NuclearProtection.Phone
{
    /// <summary>Fabrique (une fois) le clip en boucle d'un motif PhoneTone, ou rend le clip assigné.</summary>
    public static class PhoneToneLibrary
    {
        private const int SampleRate = 22050;
        private const float FadeTime = 0.005f; // évite les clics en début/fin de bip

        private static readonly Dictionary<PhoneTone, AudioClip> Cache = new();

        public static AudioClip Get(PhoneTone tone)
        {
            if (tone == null) return null;
            if (tone.clipOverride != null) return tone.clipOverride;
            if (!Cache.TryGetValue(tone, out var clip)) Cache[tone] = clip = Build(tone);
            return clip;
        }

        private static AudioClip Build(PhoneTone t)
        {
            float cycle = t.bursts * t.onTime + (t.bursts - 1) * t.gapTime + t.offTime;
            var data = new float[Mathf.Max(1, Mathf.RoundToInt(cycle * SampleRate))];
            bool continuous = t.bursts == 1 && t.offTime <= 0f; // son continu (tonalité, bourdonnement) : pas de fondu, sinon un « tic » à chaque boucle

            for (int b = 0; b < t.bursts; b++)
            {
                int start = Mathf.RoundToInt(b * (t.onTime + t.gapTime) * SampleRate);
                int length = Mathf.RoundToInt(t.onTime * SampleRate);
                for (int i = 0; i < length && start + i < data.Length; i++)
                {
                    float time = (float)i / SampleRate;
                    float v = Mathf.Sin(2f * Mathf.PI * t.freqA * time);
                    if (t.freqB > 0f) v = (v + Mathf.Sin(2f * Mathf.PI * t.freqB * time)) * 0.5f;
                    float fade = continuous ? 1f : Mathf.Clamp01(Mathf.Min(time, t.onTime - time) / FadeTime);
                    data[start + i] = v * fade * t.volume;
                }
            }

            var clip = AudioClip.Create("PhoneTone", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
