using System;
using UIBindings.Runtime;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Develop
{
    public class HeroPortraitVM : CollectionItemViewModel<Hero>
    {
        private PartyWindowVM _partyWindowVM;
        public Sprite Portrait => Source.RaceStats.Portrait;
        public string Name => Source.Name;

        public bool IsSelected => _partyWindowVM.SelectedHero == Source;

        public void Select( )
        {
            _partyWindowVM.SelectedHero = Source;
            Debug.Log( $"Selected {Source.Name}" );
        }

        private void Awake( )
        {
            _partyWindowVM = GetComponentInParent<PartyWindowVM>();
        }
    }
}
