using UnityEngine;

namespace UIBindings.Develop
{
    [CreateAssetMenu(fileName = "HeroClassStats", menuName = "UIBindings/Develop/PartyWindowDemo/HeroClassStats")]
    public class HeroClassStats : ScriptableObject
    {
        public int BaseHealth     = 50;
        public int HealthPerLevel = 5;
        public int BaseMana       = 30;
        public int ManaPerLevel   = 3;
    }
}