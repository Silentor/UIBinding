using UnityEngine;

namespace UIBindings.Editor.Utils
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Because Unity's GetComponentInParent is not working as expected for non instantiated prefabs (prefab root is effectively disabled)
        /// </summary>
        /// <param name="transform"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetComponentInParentManual<T>(this Transform transform) where T : Component
        {
            if (transform == null)
            {
                return null;
            }

            for( var t = transform; t != null; t = t.parent )
            {
                var result = t.GetComponent<T>();
                if(result != null)
                    return result;
            }

            return null;
        }
    }
}