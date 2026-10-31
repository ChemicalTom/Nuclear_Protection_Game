using System;
using Unity.Collections;
using Unity.Netcode;

namespace NuclearProtection.Phone
{
    /// <summary>État d'un téléphone d'équipe.</summary>
    public enum PhoneMode : byte
    {
        Raccroche = 0,     // combiné posé, silence
        Sonne = 1,         // combiné posé, appel entrant (voir RingKind)
        Tonalite = 2,      // décroché, rien en cours
        Appel = 3,         // décroché, on appelle l'autre bunker (ça sonne en face)
        Occupe = 4,        // décroché, l'autre bunker est occupé
        LigneMorte = 5,    // décroché, l'autre bunker est en panne (bip discret ou silence)
        Communication = 6, // en ligne avec l'autre bunker
        Message = 7,       // on écoute un appel du jeu (ordre, sanction)
        HorsService = 8    // décroché, NOTRE bunker est en panne : silence total
    }

    /// <summary>Origine d'un appel : détermine la sonnerie.</summary>
    public enum CallKind : byte
    {
        Aucun = 0,
        Equipe = 1,          // l'autre bunker
        OrdreNucleaire = 2,  // le jeu (vrai ou faux ordre)
        Sanction = 3         // le jeu (ami abattu)
    }

    /// <summary>État synchronisé d'une ligne (une par équipe).</summary>
    public struct PhoneLine : INetworkSerializable, IEquatable<PhoneLine>
    {
        public const ulong NoHolder = ulong.MaxValue;

        public ulong Holder;            // client qui tient le combiné
        public PhoneMode Mode;
        public CallKind RingKind;       // si Mode == Sonne
        public double RingStart;        // temps serveur
        public CallKind Waiting;        // double appel en attente (Aucun sinon)
        public double WaitingStart;
        public FixedString128Bytes Info; // dernier événement (« Ligne coupée. »…)

        public static PhoneLine Idle => new() { Holder = NoHolder, Mode = PhoneMode.Raccroche };
        public bool IsOffHook => Holder != NoHolder;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref Holder);
            s.SerializeValue(ref Mode);
            s.SerializeValue(ref RingKind);
            s.SerializeValue(ref RingStart);
            s.SerializeValue(ref Waiting);
            s.SerializeValue(ref WaitingStart);
            s.SerializeValue(ref Info);
        }

        public bool Equals(PhoneLine o) => Holder == o.Holder && Mode == o.Mode && RingKind == o.RingKind
            && RingStart.Equals(o.RingStart) && Waiting == o.Waiting && WaitingStart.Equals(o.WaitingStart) && Info.Equals(o.Info);
    }
}
