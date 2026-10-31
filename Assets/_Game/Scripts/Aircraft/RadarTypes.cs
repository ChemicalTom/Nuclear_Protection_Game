using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NuclearProtection.Aircraft
{
    public enum ContactKind : byte
    {
        Avion = 0,
        Missile = 1
    }

    /// <summary>
    /// Écho radar synchronisé. Volontairement SANS le camp (ami/ennemi) : seul le registre le révèle.
    /// La position se calcule à partir du temps serveur (pas besoin d'envoyer la position chaque frame).
    /// </summary>
    public struct RadarContact : INetworkSerializable, IEquatable<RadarContact>
    {
        public int Id;
        public ContactKind Kind;
        public FixedString32Bytes Code;   // vide pour un missile
        public FixedString32Bytes FireCode; // 1 lettre + 1 chiffre, affiché seulement sur l'écran radar
        public float AngleDeg;            // direction d'où vient le contact (0 = nord)
        public float StartDistance;       // 1 = bord du radar, 0 = base
        public double StartTime;          // temps serveur au départ
        public float FlightTime;          // temps pour aller de StartDistance à la base (s)

        /// <summary>Distance normalisée à un instant serveur donné (0 = sur la base).</summary>
        public float DistanceAt(double serverTime) =>
            Mathf.Max(0f, StartDistance * (1f - (float)((serverTime - StartTime) / FlightTime)));

        /// <summary>Temps restant avant d'atteindre la base (s).</summary>
        public float EtaAt(double serverTime) => Mathf.Max(0f, (float)(StartTime + FlightTime - serverTime));

        /// <summary>Position 2D sur l'écran radar (rayon 1), nord en haut.</summary>
        public Vector2 ScreenPosAt(double serverTime)
        {
            float d = DistanceAt(serverTime), a = AngleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * d;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Id);
            s.SerializeValue(ref Kind);
            s.SerializeValue(ref Code);
            s.SerializeValue(ref FireCode);
            s.SerializeValue(ref AngleDeg);
            s.SerializeValue(ref StartDistance);
            s.SerializeValue(ref StartTime);
            s.SerializeValue(ref FlightTime);
        }

        public bool Equals(RadarContact o) => Id == o.Id && Kind == o.Kind && Code.Equals(o.Code) && FireCode.Equals(o.FireCode)
            && AngleDeg.Equals(o.AngleDeg) && StartDistance.Equals(o.StartDistance)
            && StartTime.Equals(o.StartTime) && FlightTime.Equals(o.FlightTime);
    }

    /// <summary>Ligne du registre : un code et son camp (consulté au poste Registre).</summary>
    public struct RegistryEntry : INetworkSerializable, IEquatable<RegistryEntry>
    {
        public FixedString32Bytes Code;
        public bool IsEnemy;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Code);
            s.SerializeValue(ref IsEnemy);
        }

        public bool Equals(RegistryEntry o) => Code.Equals(o.Code) && IsEnemy == o.IsEnemy;
    }
}
