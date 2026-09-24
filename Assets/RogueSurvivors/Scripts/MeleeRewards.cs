using UnityEngine;
namespace RogueSurvivors
{
    // 接近して物理武器で倒したときの報酬。端数を蓄積して正確に25%増やす。
    public sealed class MeleeRewards : MonoBehaviour
    {
        float experienceRemainder;
        public int Reward(int baseExperience)
        {
            experienceRemainder+=baseExperience*.25f;
            int bonus=Mathf.FloorToInt(experienceRemainder); experienceRemainder-=bonus;
            return baseExperience+bonus;
        }
    }
}
