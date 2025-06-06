using System;

namespace UIBindings
{
    [Serializable]
    public abstract class BindingBase
    {
        public        Boolean                   Enabled             = true;                        //Checked once on start!

        //Source object (can be serializable Unity object or simple CLR object)
        public UnityEngine.Object Source;
        public String             SourceType;
        public Boolean            BindToType;

        //Path to bindable property or method
        public        String                    Path;
    }
}