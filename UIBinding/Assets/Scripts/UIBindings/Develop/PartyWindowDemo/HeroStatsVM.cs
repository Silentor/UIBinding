using System;
using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings.Develop
{
    public class HeroStatsVM : ViewModel<Hero>
    {
        public string Name => Source.Name;

        public int Level => Source.Level;

        public ERace Race => Source.Race;

        public EClass Class => Source.Class;
    }
}
