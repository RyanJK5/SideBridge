using System;

namespace SideBridge;

public enum SoundEffectID {
    Block1,
    Block2,
    Block3,
    Block4,
    Block5,
    Block6,
    Block7,
    Block8,
    BreakBlock,
    SwordHit,
    ArrowHit,
    Kill,
    Tick,
    Win,
    Bow1,
    Bow2,
    Bow3,
    Bow4,
    Bow5,
    Bow6
}

public static class SoundEffects {
    private static readonly Random s_random = new();

    public static SoundEffectID GetRandomBlockSound() =>
        (SoundEffectID) s_random.Next((int) SoundEffectID.Block1, (int) SoundEffectID.Block8)
    ;
    

    public static SoundEffectID GetRandomBowSound() =>
        (SoundEffectID) s_random.Next((int) SoundEffectID.Bow1, (int) SoundEffectID.Bow6)
    ;
}