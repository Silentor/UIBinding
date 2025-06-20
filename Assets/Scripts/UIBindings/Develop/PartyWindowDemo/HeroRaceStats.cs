using UnityEngine;

namespace UIBindings.Develop
{
    [CreateAssetMenu(fileName = "HeroRaceStats", menuName = "UIBindings/Develop/PartyWindowDemo/HeroRaceStats")]
    public class HeroRaceStats : ScriptableObject
    {
        public Sprite    Portrait;
        public AudioClip VoiceLine;
        public int       BaseHealth     = 50;
        public int       HealthPerLevel = 5;
        public int       BaseMana       = 30;
        public int       ManaPerLevel   = 3;
    }
}