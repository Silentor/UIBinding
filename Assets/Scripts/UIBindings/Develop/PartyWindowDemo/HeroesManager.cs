using System.Collections.Generic;

namespace UIBindings.Develop
{
    public class HeroesManager
    {
        public IReadOnlyList<Hero> Heroes => _heroes;

        public HeroesManager( )
        {
            _heroes = new []
                      {
                              new Hero( "Robin Good", 14, ERace.Human, EClass.Rogue, new Stats( 15, 5 ) ),
                              new Hero( "Elrond", 21, ERace.Elf, EClass.Warrior, new Stats( 30, 30 ) ),
                              new Hero( "Guldan", 24, ERace.Orc, EClass.Mage, new Stats( 15, 100 ) ),
                      };
        }

        private readonly IReadOnlyList<Hero> _heroes;
    }
}