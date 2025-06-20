using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings.Develop
{
    public class HeroPortraitVM : ViewModel<Hero>
    {
        public Sprite Portrait => DataSource.RaceStats.Portrait;
        public string Name => DataSource.Name;

    }
}
