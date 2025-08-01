using UnityEngine;

namespace UIBindings.Develop
{
    /// <summary>
    /// Its a simple logic class, no marked as a ViewModel, but anyway can be used as a source for bindings. Binding will be updated regularly by property polling 
    /// </summary>
    public class Player : MonoBehaviour
    {
        public string Name { get; private set; }    = "Not inited";

        public void Init( string name )
        {
            Name = name;
        }
    }
}