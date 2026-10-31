using NuclearProtection.Config;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NuclearProtection.Rooms.Visuals
{
    /// <summary>
    /// Ambiance de l'image (local) : halo des écrans et néons (bloom), vignette, grain de film,
    /// couleurs un peu désaturées, brouillard léger, lumière ambiante. Tout vient de VisualSettings.
    /// </summary>
    public class PostFxSetup : MonoBehaviour
    {
        private VisualSettings _v;
        private Camera _camera;

        public static void Create(Transform parent, VisualSettings v)
        {
            var go = new GameObject("PostTraitement");
            go.transform.SetParent(parent, false);
            go.AddComponent<PostFxSetup>()._v = v;
        }

        private void Start()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = _v.ambientColor;
            var probe = new SphericalHarmonicsL2(); // URP lit la sonde d'ambiance, pas ambientLight seul
            probe.AddAmbientLight(_v.ambientColor);
            RenderSettings.ambientProbe = probe;
            RenderSettings.fog = _v.fogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = _v.fogDensity;
            RenderSettings.fogColor = _v.fogColor;
            if (_v.postProcessing) CreateVolume();
        }

        private void CreateVolume()
        {
            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(_v.bloomIntensity);
            bloom.threshold.Override(_v.bloomThreshold);
            profile.Add<Vignette>(true).intensity.Override(_v.vignette);
            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(_v.filmGrain);
            grain.type.Override(FilmGrainLookup.Medium1);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(_v.saturation);
            color.contrast.Override(_v.contrast);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.Neutral);
        }

        // La caméra du joueur local apparaît après le bunker : on active son post-traitement dès qu'elle existe.
        private void Update()
        {
            if (!_v.postProcessing || Camera.main == null || Camera.main == _camera) return;
            _camera = Camera.main;
            var data = _camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }
    }
}
