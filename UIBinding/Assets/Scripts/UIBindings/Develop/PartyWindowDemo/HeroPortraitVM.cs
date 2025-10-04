using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings.Develop
{
    public class HeroPortraitVM : CollectionItemViewModel<Hero>
    {
        private PartyWindowVM _partyWindowVM;
        public Sprite Portrait => SourceItem.RaceStats.Portrait;
        public string Name => SourceItem.Name;

        public bool IsSelected => _partyWindowVM.SelectedHero == SourceItem;

        public void Select( )
        {
            _partyWindowVM.SelectedHero = SourceItem;
            Debug.Log( $"Selected {SourceItem.Name}" );
        }

        private void Awake( )
        {
            _partyWindowVM = GetComponentInParent<PartyWindowVM>();
            _partyWindowVM.PropertyChanged += ( sender, propertyName ) =>
            {
                if ( propertyName == nameof( PartyWindowVM.SelectedHero ) )                    
                    OnPropertyChanged( nameof( IsSelected ) );
            };
        }
    }
}
