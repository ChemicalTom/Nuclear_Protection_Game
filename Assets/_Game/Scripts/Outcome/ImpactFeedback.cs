using NuclearProtection.Config;
using NuclearProtection.Network;
using UnityEngine;

namespace NuclearProtection.Outcome
{
    /// <summary>
    /// Client : un ennemi a frappé la base → secousse de la caméra et explosion sourde lointaine,
    /// de plus en plus fortes à chaque impact (le compteur, lui, n'est jamais affiché).
    /// </summary>
    public class ImpactFeedback : MonoBehaviour
    {
        private const int SampleRate = 22050;

        private AudioSource _source;
        private AudioClip _boom;
        private float _shakeUntil, _amplitude, _seed;
        private bool _shaking;

        private static DefeatSettings S => ConnectionManager.Instance.Config.defeat;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // entendu partout dans le bunker, comme une explosion au loin
        }

        private void OnEnable() => MatchOutcome.LocalEnemyImpact += OnImpact;
        private void OnDisable() => MatchOutcome.LocalEnemyImpact -= OnImpact;

        private void OnImpact(int impact)
        {
            int extra = Mathf.Max(0, impact - 1);
            _amplitude = S.shakeFirstAmplitude + S.shakeAmplitudePerImpact * extra;
            _shakeUntil = Time.time + S.shakeDuration;
            _seed = Random.value * 100f;
            _shaking = true;

            _boom ??= S.boomClipOverride != null ? S.boomClipOverride : BuildBoom(S.boomDuration);
            _source.PlayOneShot(_boom, Mathf.Clamp01(S.boomFirstVolume + S.boomVolumePerImpact * extra));
        }

        // Après le PlayerMotor (qui oriente la tête), on ajoute un décalage à la caméra, posée sur la tête.
        private void LateUpdate()
        {
            if (!_shaking) return;
            var cam = Camera.main;
            if (cam == null) return;

            float left = _shakeUntil - Time.time;
            if (left <= 0f)
            {
                _shaking = false;
                cam.transform.localRotation = Quaternion.identity;
                cam.transform.localPosition = Vector3.zero;
                return;
            }

            float k = _amplitude * (left / S.shakeDuration); // s'atténue jusqu'à zéro
            float t = Time.time * S.shakeFrequency;
            float pitch = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f * k;
            float yaw = (Mathf.PerlinNoise(_seed + 10f, t) - 0.5f) * 2f * k;
            float roll = (Mathf.PerlinNoise(_seed + 20f, t) - 0.5f) * k;
            cam.transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
            cam.transform.localPosition = new Vector3(0f, (Mathf.PerlinNoise(_seed + 30f, t) - 0.5f) * k * 0.02f, 0f);
        }

        // Explosion lointaine générée : bruit grave filtré + coup sourd, avec une longue décroissance.
        private static AudioClip BuildBoom(float duration)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            var data = new float[n];
            var rng = new System.Random(1234);
            float low = 0f, lower = 0f, peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float time = (float)i / SampleRate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                low += 0.04f * (white - low);    // passe-bas : on ne garde que le grondement
                lower += 0.04f * (low - lower);
                float attack = Mathf.Clamp01(time / 0.03f);
                float decay = Mathf.Exp(-time * 2.2f);
                float thump = Mathf.Sin(2f * Mathf.PI * 38f * time) * Mathf.Exp(-time * 6f);
                data[i] = (lower * 6f + thump * 0.6f) * attack * decay;
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            if (peak > 0f) for (int i = 0; i < n; i++) data[i] /= peak;

            var clip = AudioClip.Create("ExplosionLointaine", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
