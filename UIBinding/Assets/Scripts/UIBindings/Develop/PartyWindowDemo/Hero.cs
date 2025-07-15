using System;

namespace UIBindings.Develop
{
    public class Hero
    {
        public String Name { get; set; }
        public int Level { get; set; }
        public Stats Stats {get; set; }

        public ERace Race { get; set; }
        public EClass Class { get; set; }

        public HeroRaceStats RaceStats;
        public HeroClassStats ClassStats;

        public int MaxHealth => RaceStats.BaseHealth + ClassStats.BaseHealth + Level * (ClassStats.HealthPerLevel + RaceStats.HealthPerLevel);
        public int MaxMana => RaceStats.BaseMana + ClassStats.BaseMana + Level * (ClassStats.ManaPerLevel + RaceStats.ManaPerLevel);

        public Hero(String name, Int32 level, ERace race, EClass @class, Stats stats )
        {
            Name = name;
            Level = level;
            Race = race;
            Class = @class;
            Stats = stats;

            RaceStats = UnityEngine.Resources.Load<HeroRaceStats>( race switch
                                                                   {
                                                                           ERace.Human => "HumanStats",
                                                                           ERace.Elf   => "ElfStats",
                                                                           ERace.Orc   => "OrcStats",
                                                                           _           => throw new ArgumentOutOfRangeException( nameof(race), race, null )
                                                                   } );
            ClassStats = UnityEngine.Resources.Load<HeroClassStats>( @class switch
                                                                   {
                                                                           EClass.Warrior => "FighterStats",
                                                                           EClass.Mage    => "MageStats",
                                                                           EClass.Rogue   => "RogueStats",
                                                                           _              => throw new ArgumentOutOfRangeException( nameof(@class), @class, null )
                                                                   } );
            Stats.Health = Math.Min( Stats.Health, MaxHealth );
            Stats.Mana = Math.Min( Stats.Mana, MaxMana );
        }

        public void Heal( )
        {
            var healAmount = MaxHealth / 10;
            Stats.Health = Math.Min( Stats.Health + healAmount, MaxHealth );
        }
    }

    public class Stats
    {
        public int Health { get; set; }
        public int Mana { get; set; }

        public Stats(int health, int mana )
        {
            Health = health;
            Mana = mana;
        }
    }

    public enum ERace
    {
        Human,
        Elf,
        Orc,
    }

    public enum EClass
    {
        Warrior,
        Mage,
        Rogue,
    }
}