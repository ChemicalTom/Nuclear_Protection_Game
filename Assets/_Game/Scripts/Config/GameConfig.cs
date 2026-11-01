using UnityEngine;

namespace NuclearProtection.Config
{
    /// <summary>
    /// Configuration centrale du jeu : TOUS les réglages passent par ici (rien en dur).
    /// Créer via : Assets > Create > Nuclear Protection > Game Config.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Nuclear Protection/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public MatchDurationSettings duration = new();
        public DifficultySettings difficulty = new();
        public NetworkSettings network = new();
        public RoomSettings rooms = new();
        public PlayerControlSettings player = new();
        public AircraftSettings aircraft = new();
        public FireSettings fire = new();
        public KeySettings keys = new();
        public PhoneSettings phone = new();
        public CallSettings calls = new();
        public FuseSettings fuse = new();
        public DefeatSettings defeat = new();
        public ScoreSettings score = new();
        public DirectorSettings director = new();
        public DisplaySettings display = new();
        public VisualSettings visuals = new();
        public UiSettings ui = new();
        public AvatarSettings avatar = new();
        public GuideSettings guide = new();

        // Appelé par Unity à chaque modification dans l'Inspector.
        private void OnValidate() => Sanitize();

        /// <summary>Corrige les bornes incohérentes (max inférieur au min, etc.).</summary>
        public void Sanitize()
        {
            difficulty.Sanitize();
            if (aircraft.sidePassMissMax < aircraft.sidePassMissMin)
                aircraft.sidePassMissMax = aircraft.sidePassMissMin;
            if (calls.intervalMax < calls.intervalMin)
                calls.intervalMax = calls.intervalMin;
            if (calls.forceCallAfter < calls.intervalMax)
                calls.forceCallAfter = calls.intervalMax;
            if (calls.trueOrderSecondCallMax < calls.trueOrderSecondCallMin)
                calls.trueOrderSecondCallMax = calls.trueOrderSecondCallMin;
            if (calls.sanctionStaggerMax < calls.sanctionStaggerMin)
                calls.sanctionStaggerMax = calls.sanctionStaggerMin;
            if (duration.customMaxMinutes < duration.customMinMinutes)
                duration.customMaxMinutes = duration.customMinMinutes;
        }
    }
}
