using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UIBindings.Editor
{
    [CustomEditor( typeof(BinderBase), editorForChildClasses: true)]
    public class BinderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script" || iterator.name == BinderBase.ConvertersFieldName )
                    continue;
                EditorGUILayout.PropertyField(iterator, true);
            }



            // Draw Converters field last
            var converters = serializedObject.FindProperty( BinderBase.ConvertersFieldName  );
            if (converters != null)
                EditorGUILayout.PropertyField(converters, true);

            //serializedObject.ApplyModifiedProperties();
        }
    }
}

