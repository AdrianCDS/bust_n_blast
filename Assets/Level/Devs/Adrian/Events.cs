using System;
using UnityEngine;

public static class Events
{
    public static event Action OnProtectionShieldPlaced;
    public static event Action<Transform> OnProtectionShieldRemoved;

    public static void NotifyProtectionShieldPlaced()
    {
        OnProtectionShieldPlaced?.Invoke();
    }

    public static void NotifyProtectionShieldRemoved(Transform shieldTransform)
    {
        OnProtectionShieldRemoved?.Invoke(shieldTransform);
    }
}
