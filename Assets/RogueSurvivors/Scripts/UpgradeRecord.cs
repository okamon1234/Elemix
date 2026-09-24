using System;
namespace RogueSurvivors
{
    [Serializable]
    public sealed class UpgradeRecord
    {
        public UpgradeKind kind;
        public float previousValue;
        public UpgradeRecord(UpgradeKind kind, float value) { this.kind = kind; previousValue = value; }
    }
}
