using System;
using System.Linq.Expressions;

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
}

public static class SoundEffects {
    private static readonly Random s_random = new();

    public static SoundEffectID GetRandomBlockSound() {
        return (SoundEffectID)s_random.Next((int) SoundEffectID.Block1, (int) SoundEffectID.Block8);
    }
}