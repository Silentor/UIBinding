using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UIBindings.Runtime;
using Object = System.Object;

namespace UIBindings.Develop
{
    public class PartyWindowVM : ViewModel, INotifyPropertyChanged
    {
        public HeroesManager HeroesManager { get ; private set ; }
        public IReadOnlyList<Hero> Heroes => HeroesManager.Heroes;

        public Hero SelectedHero
        {
            get => _selectedHero;
            set
            {
                if (_selectedHero == value)
                    return;

                _selectedHero = value;
                DoPropertyChanged( this );
            }
        }

        private Hero _selectedHero;

        private void Awake( )
        {
            HeroesManager = new HeroesManager();
            SelectedHero = HeroesManager.Heroes.First();
        }

        private void DoPropertyChanged( Object sender, [CallerMemberName] String propertyName = null )
        {
            PropertyChanged?.Invoke( sender, propertyName );
        }

        public event Action<Object, String> PropertyChanged;
    }
}
