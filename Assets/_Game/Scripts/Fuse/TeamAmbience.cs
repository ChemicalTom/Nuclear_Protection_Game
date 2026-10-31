using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Phone;
using NuclearProtection.Rooms;
using UnityEngine;

namespace NuclearProtection.Fuse
{
    /// <summary>
    /// Ambiance de l'espace d'une équipe (local, sur chaque machine) : lumière normale et bourdonnement,
    /// remplacés pendant une panne par la lumière rouge d'urgence et l'alarme (entendue seulement sur place).
    /// Éteint aussi les postes de l'équipe, sauf le boîtier à fusible.
    /// </summary>
    public class TeamAmbience : MonoBehaviour
    {
        private TeamId _team;
        private GameConfig _config;
        private Light _normal, _emergency;
        private AudioSource _hum, _alarm;
        private bool? _wasDark;

        private FuseSettings S => _config.fuse;

        /// <summary>Crée l'ambiance au centre de l'espace de l'équipe (taille au sol = size).</summary>
        public static TeamAmbience Create(Transform parent, TeamId team, Vector3 origin, Vector2 size, GameConfig config)
        {
            var go = new GameObject($"Ambiance_{team}");
            go.transform.SetParent(parent, false);
            go.transform.position = origin + Vector3.up * (config.rooms.wallHeight - 0.3f);

            var a = go.AddComponent<TeamAmbience>();
            a._team = team;
            a._config = config;
            float range = Mathf.Max(size.x, size.y) * 1.2f;
            a._normal = a.CreateLight("LumiereNormale", config.rooms.roomLightColor, config.rooms.roomLightIntensity, range);
            a._emergency = a.CreateLight("LumiereUrgence", config.fuse.emergencyLightColor, config.fuse.emergencyLightIntensity, range);
            a._hum = a.CreateSource("Bourdonnement");
            a._alarm = a.CreateSource("Alarme");
            return a;
        }

        private Light CreateLight(string lightName, Color color, float intensity, float range)
        {
            var light = new GameObject(lightName).AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var src = new GameObject(sourceName).AddComponent<AudioSource>();
            src.transform.SetParent(transform, false);
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = S.ambienceMaxDistance;
            return src;
        }

        private void Update()
        {
            bool dark = FuseSystem.Instance != null && FuseSystem.Instance.IsBlackedOut(_team);

            // Clignotement doux de la lumière d'urgence.
            if (dark && S.emergencyLightPulse > 0f)
                _emergency.intensity = S.emergencyLightIntensity * (0.55f + 0.45f * Mathf.Cos(Time.time * S.emergencyLightPulse * 2f * Mathf.PI));

            if (_wasDark == dark) return;
            _wasDark = dark;

            _normal.enabled = !dark;
            _emergency.enabled = dark;
            _emergency.intensity = S.emergencyLightIntensity;
            Play(_hum, dark ? null : S.humSound);
            Play(_alarm, dark ? S.alarmSound : null);

            foreach (var s in StationRegistry.All)
            {
                if (s.Team != _team) continue;
                if (s.Type == StationType.Fusible) s.SetStateColor(dark ? S.blownFuseColor : null);
                else s.SetPowered(!dark, S.unpoweredStationColor);
            }
        }

        private static void Play(AudioSource src, PhoneTone tone)
        {
            var clip = PhoneToneLibrary.Get(tone);
            if (clip == null) { src.Stop(); return; }
            src.clip = clip;
            src.Play();
        }
    }
}
