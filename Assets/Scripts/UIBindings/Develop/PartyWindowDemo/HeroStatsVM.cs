using System;
using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings.Develop
{
    public class HeroStatsVM : ViewModel<Hero>
    {
        public string Name => DataSource.Name;

        public bool EnableGO => !String.IsNullOrEmpty( Name );
    }
}
