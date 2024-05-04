namespace SideBridge;

public class ChargedValue {
    public readonly float MaxCharge;
    public float Charge;

    public readonly float Cooldown;
    public float TimeSince;

    public bool OnCooldown => TimeSince < Cooldown;
    public bool Charged => Charge >= MaxCharge;

    public ChargedValue(float maxCharge, float cooldown) {
        MaxCharge = maxCharge;
        Charge = 0;
        
        Cooldown = cooldown;
        TimeSince = Cooldown;
    }

    public bool Increment(float amount, bool charging) {
        if (OnCooldown) {
            TimeSince += amount;
            return false;
        }
        if (!charging) {
            return false;
        }
        Charge += amount;
        if (Charged) {
            Charge = MaxCharge;
        }
        return Charged;
    }

    public void Restart() {
        Charge = 0;
        TimeSince = 0;
    }

    public void Reset() {
        Charge = 0;
        TimeSince = Cooldown;
    }
}