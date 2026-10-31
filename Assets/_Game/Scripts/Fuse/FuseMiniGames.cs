using UnityEngine;

namespace NuclearProtection.Fuse
{
    /// <summary>
    /// Règles pures des mini-jeux, calculées à l'identique sur le serveur et les clients
    /// à partir de la seed de la panne (rien d'autre à synchroniser).
    /// </summary>
    public static class FuseMiniGames
    {
        // ---------- Aiguille ----------

        /// <summary>Angle de l'aiguille (degrés) au temps serveur donné.</summary>
        public static float NeedleAngle(double serverTime, float speed) => (float)(serverTime * speed % 360d);

        /// <summary>Centre de la zone verte : change après chaque réussite.</summary>
        public static float NeedleZoneCenter(int seed, int progress) => new System.Random(seed * 31 + progress).Next(360);

        public static bool IsInZone(float angle, float center, float size) => Mathf.Abs(Mathf.DeltaAngle(angle, center)) <= size / 2f;

        // ---------- Fils ----------

        /// <summary>Couleur (indice du fil) de chaque prise de droite : permutation tirée de la seed.</summary>
        public static int[] WireSocketOrder(int seed, int count)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            var rng = new System.Random(seed);
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        public static int AllWiresMask(int count) => (1 << count) - 1;
        public static bool IsWireConnected(int mask, int wire) => (mask & (1 << wire)) != 0;
    }
}
