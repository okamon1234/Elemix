using System;
namespace RogueSurvivors
{
    [Serializable]
    public sealed class UpgradeRecord
    {
        public UpgradeKind kind;
        public float previousValue;
        public bool hasPhysicalSnapshot;
        public System.Collections.Generic.List<WeaponSaveData> physicalWeapons;
        public System.Collections.Generic.List<string> physicalFusions;
        public UpgradeRecord(UpgradeKind kind, float value) { this.kind = kind; previousValue = value; }
    }
}
