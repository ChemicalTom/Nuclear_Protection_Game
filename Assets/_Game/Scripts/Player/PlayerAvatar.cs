using NuclearProtection.Config;
using NuclearProtection.Core;
using NuclearProtection.Network;
using NuclearProtection.Rooms;
using NuclearProtection.Rooms.Visuals;
using UnityEngine;

namespace NuclearProtection.Player
{
    /// <summary>
    /// Personnage visible (soldat en formes simples, sans collision) qui remplace la capsule.
    /// Uniforme aux couleurs de l'équipe, bras et jambes qui se balancent selon la vitesse.
    /// Pour soi-même : seulement l'ombre (vue à la première personne).
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        private AvatarSettings _s;
        private ulong _ownerId;
        private bool _isOwner, _teamApplied;
        private Material _uniform, _trousers, _patch;
        private Transform _leftArm, _rightArm, _leftLeg, _rightLeg;
        private TextMesh _nameTag;
        private Vector3 _lastPos;
        private float _phase, _swing;

        /// <summary>Construit le personnage sous le joueur et cache la capsule.</summary>
        public static void Attach(GameObject player, Renderer capsule, ulong ownerId, bool isOwner, AvatarSettings s)
        {
            var avatar = player.AddComponent<PlayerAvatar>();
            avatar._s = s;
            avatar._ownerId = ownerId;
            avatar._isOwner = isOwner;
            avatar.Build(capsule.sharedMaterial);
            capsule.enabled = false;
        }

        private void Build(Material baseMat)
        {
            var root = new GameObject("Personnage").transform;
            root.SetParent(transform, false);

            _uniform = Tint(baseMat, _s.unknownUniform);
            _trousers = Tint(baseMat, _s.unknownUniform * _s.trousersDarkening);
            _patch = Tint(baseMat, _s.unknownUniform);
            var skin = Tint(baseMat, _s.skin);
            var helmet = Tint(baseMat, _s.helmet);
            var boots = Tint(baseMat, _s.boots);
            var belt = Tint(baseMat, _s.belt);

            // Tronc (le pivot du joueur est au niveau des pieds).
            Shapes.Box(root, "Torse", new Vector3(0, 1.22f, 0), new Vector3(0.46f, 0.56f, 0.26f), _uniform);
            Shapes.Box(root, "Ceinture", new Vector3(0, 0.95f, 0), new Vector3(0.48f, 0.08f, 0.28f), belt);
            Shapes.Box(root, "Ecusson", new Vector3(0.12f, 1.36f, 0.135f), new Vector3(0.1f, 0.08f, 0.01f), _patch);
            Shapes.Box(root, "Cou", new Vector3(0, 1.53f, 0), new Vector3(0.12f, 0.08f, 0.12f), skin);

            // Tête, yeux et casque.
            Shapes.Box(root, "Tete", new Vector3(0, 1.67f, 0), new Vector3(0.22f, 0.24f, 0.24f), skin);
            Shapes.Box(root, "OeilG", new Vector3(-0.05f, 1.69f, 0.121f), new Vector3(0.035f, 0.025f, 0.01f), boots);
            Shapes.Box(root, "OeilD", new Vector3(0.05f, 1.69f, 0.121f), new Vector3(0.035f, 0.025f, 0.01f), boots);
            Shapes.Sphere(root, "Casque", new Vector3(0, 1.78f, -0.01f), 0.3f, helmet).transform.localScale = new Vector3(0.3f, 0.2f, 0.32f);
            Shapes.Cylinder(root, "Bord", new Vector3(0, 1.76f, -0.01f), 0.33f, 0.025f, helmet);

            // Membres : un pivot à l'épaule / à la hanche pour le balancement.
            _leftArm = Limb(root, "BrasG", new Vector3(-0.3f, 1.47f, 0), new Vector3(0.12f, 0.56f, 0.14f), _uniform);
            _rightArm = Limb(root, "BrasD", new Vector3(0.3f, 1.47f, 0), new Vector3(0.12f, 0.56f, 0.14f), _uniform);
            Shapes.Box(_leftArm, "Brassard", new Vector3(0, -0.12f, 0), new Vector3(0.13f, 0.07f, 0.15f), _patch);
            Shapes.Box(_leftArm, "MainG", new Vector3(0, -0.61f, 0), new Vector3(0.1f, 0.1f, 0.11f), skin);
            Shapes.Box(_rightArm, "MainD", new Vector3(0, -0.61f, 0), new Vector3(0.1f, 0.1f, 0.11f), skin);

            _leftLeg = Limb(root, "JambeG", new Vector3(-0.12f, 0.92f, 0), new Vector3(0.17f, 0.82f, 0.19f), _trousers);
            _rightLeg = Limb(root, "JambeD", new Vector3(0.12f, 0.92f, 0), new Vector3(0.17f, 0.82f, 0.19f), _trousers);
            Shapes.Box(_leftLeg, "BotteG", new Vector3(0, -0.85f, 0.04f), new Vector3(0.19f, 0.14f, 0.29f), boots);
            Shapes.Box(_rightLeg, "BotteD", new Vector3(0, -0.85f, 0.04f), new Vector3(0.19f, 0.14f, 0.29f), boots);

            // Soi-même : on ne voit que son ombre.
            if (_isOwner)
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            if (_s.showNameTag && !_isOwner)
            {
                _nameTag = TextLabel.Create(root, "Nom", "", _s.nameTagSize);
                _nameTag.color = _s.nameTagColor;
                _nameTag.transform.localPosition = new Vector3(0, 2.05f, 0);
                _nameTag.gameObject.AddComponent<LabelBillboard>();
            }
            _lastPos = transform.position;
        }

        // Membre suspendu à son pivot (épaule ou hanche).
        private static Transform Limb(Transform root, string name, Vector3 pivot, Vector3 size, Material mat)
        {
            var p = new GameObject(name).transform;
            p.SetParent(root, false);
            p.localPosition = pivot;
            Shapes.Box(p, name + "_Forme", new Vector3(0, -size.y / 2f, 0), size, mat);
            return p;
        }

        private static Material Tint(Material baseMat, Color c)
        {
            var m = new Material(baseMat);
            SetColor(m, c);
            return m;
        }

        private static void SetColor(Material m, Color c)
        {
            c.a = 1f;
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        private void Update()
        {
            if (!_teamApplied) TryApplyTeam();
            Animate();
        }

        // L'équipe arrive par le réseau (liste des joueurs) : on colore l'uniforme dès qu'elle est connue.
        private void TryApplyTeam()
        {
            var session = MatchSession.Instance;
            if (session == null) return;
            foreach (var p in session.Players)
            {
                if (p.ClientId != _ownerId) continue;
                bool radar = p.Team == TeamId.Radar;
                var uniform = radar ? _s.radarUniform : _s.tirUniform;
                SetColor(_uniform, uniform);
                SetColor(_trousers, uniform * _s.trousersDarkening);
                SetColor(_patch, radar ? _s.radarPatch : _s.tirPatch);
                if (_nameTag != null) _nameTag.text = p.Name.ToString();
                _teamApplied = true;
                return;
            }
        }

        // Balancement d'après le déplacement réel (marche aussi pour les joueurs distants).
        private void Animate()
        {
            Vector3 delta = transform.position - _lastPos;
            _lastPos = transform.position;
            delta.y = 0f;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = delta.magnitude / dt;

            _phase += delta.magnitude * _s.stepsPerMeter * Mathf.PI;
            float target = Mathf.Clamp01(speed / _s.fullSwingSpeed);
            _swing = Mathf.MoveTowards(_swing, target, dt * 4f); // retour en douceur à l'arrêt
            float a = Mathf.Sin(_phase) * _s.swingAngle * _swing;

            _leftLeg.localRotation = Quaternion.Euler(a, 0, 0);
            _rightLeg.localRotation = Quaternion.Euler(-a, 0, 0);
            _leftArm.localRotation = Quaternion.Euler(-a * 0.8f, 0, 0);
            _rightArm.localRotation = Quaternion.Euler(a * 0.8f, 0, 0);
        }
    }
}
