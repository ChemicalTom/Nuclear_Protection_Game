using NuclearProtection.Aircraft;
using NuclearProtection.Config;
using NuclearProtection.Network;
using UnityEngine;

namespace NuclearProtection.Fire
{
    /// <summary>
    /// Client : explosion entendue par tous quand une cible est abattue
    /// (plus sèche et plus proche que le grondement d'un ennemi passé ; plus aiguë pour un missile).
    /// </summary>
    public class ShotDownSound : MonoBehaviour
    {
        private const int SampleRate = 22050;

        private AudioSource _source;
        private AudioClip _clip;

        private static FireSettings S => ConnectionManager.Instance.Config.fire;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // entendu partout, comme une explosion dans le ciel
        }

        private void OnEnable() => FireControl.LocalShotDown += OnShotDown;
        private void OnDisable() => FireControl.LocalShotDown -= OnShotDown;

        private void OnShotDown(ContactKind kind)
        {
            _clip ??= S.shotDownClipOverride != null ? S.shotDownClipOverride : BuildExplosion(S.shotDownDuration);
            _source.pitch = kind == ContactKind.Missile ? S.missileShotDownPitch : 1f;
            _source.PlayOneShot(_clip, S.shotDownVolume);
        }

        // Explosion générée : claquement bref (bruit peu filtré) puis grondement qui s'éteint.
        private static AudioClip BuildExplosion(float duration)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            var data = new float[n];
            var rng = new System.Random(4321);
            float low = 0f, peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float time = (float)i / SampleRate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                low += 0.08f * (white - low); // passe-bas pour le grondement
                float crack = white * Mathf.Exp(-time * 30f);
                float rumble = low * 4f * Mathf.Clamp01(time / 0.01f) * Mathf.Exp(-time * 3f);
                float thump = Mathf.Sin(2f * Mathf.PI * 55f * time) * Mathf.Exp(-time * 9f);
                data[i] = crack * 0.7f + rumble + thump * 0.5f;
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            if (peak > 0f) for (int i = 0; i < n; i++) data[i] /= peak;

            var clip = AudioClip.Create("CibleAbattue", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
