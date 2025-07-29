using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UIBindings.Runtime;
using UIBindings.SourceGen;
using Object = System.Object;

namespace UIBindings.Develop
{
    public partial class PartyWindowVM : ViewModel
    {
        public HeroesManager HeroesManager { get ; private set ; }
        public IReadOnlyList<Hero> Heroes => HeroesManager.Heroes;

        public void Close( )
        {
            gameObject.SetActive( false );
        }

        [ObservableProperty]
        private Hero _selectedHero;

        private void Awake( )
        {
            HeroesManager = new HeroesManager();
            SelectedHero = HeroesManager.Heroes.First();
        }
    }
}
