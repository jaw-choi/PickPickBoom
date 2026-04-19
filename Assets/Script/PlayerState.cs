using System;

[Serializable]
public sealed class PlayerState
{
    public bool HasShield { get; private set; }
    public bool HideNextResultMessage { get; private set; }

    public void ResetSession()
    {
        HasShield = false;
        HideNextResultMessage = false;
    }

    public bool TryGainShield()
    {
        if (HasShield)
        {
            return false;
        }

        HasShield = true;
        return true;
    }

    public bool ConsumeShield()
    {
        if (!HasShield)
        {
            return false;
        }

        HasShield = false;
        return true;
    }

    public void ApplyCurse()
    {
        HideNextResultMessage = true;
    }

    public bool ConsumeCurse()
    {
        if (!HideNextResultMessage)
        {
            return false;
        }

        HideNextResultMessage = false;
        return true;
    }
}
