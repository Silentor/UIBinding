using UIBindings.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIBindings.Editor
{
    [CustomPropertyDrawer( typeof(DataBinding.UpdateMode) )]
    public class BindingUpdateModeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {

            // Draw the popup
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel( position, label );

            if ( EditorGUI.indentLevel > 0 )
            {
                position.xMin -= EditorGUI.indentLevel * 15;
            }


            var modeProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.Mode) );
            var delayProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.Delay) );
            var unscaledTimeProp = property.FindPropertyRelative( nameof(DataBinding.UpdateMode.UnscaledTime) );

            if ( ((DataBinding.EUpdateMode)modeProp.intValue) != DataBinding.EUpdateMode.Manual )
            {
                var rects = GUIUtils.GetHorizontalRects( position, 0, 0, 80, 90 );
                EditorGUI.PropertyField( rects.Item1, modeProp, GUIContent.none );
                var delayRects = GUIUtils.GetHorizontalRects( rects.Item2, 0, 20, 60 );
                GUI.Label( delayRects.Item1, Resources.DelayLabel, Resources.RightAlignedLabel );
                EditorGUI.PropertyField( delayRects.Item2, delayProp, GUIContent.none );
                var unscaledRects = GUIUtils.GetHorizontalRects( rects.Item3,  0, 60, 30 );
                GUI.Label( unscaledRects.Item1, Resources.UnscaledTimeLabel, Resources.RightAlignedLabel );
                EditorGUI.PropertyField( unscaledRects.Item2, unscaledTimeProp, GUIContent.none );
            }
            else
            {
                EditorGUI.PropertyField( position, modeProp, GUIContent.none );
            }



            EditorGUI.EndProperty();
        }

        private static class Resources
        {
            public static readonly GUIStyle RightAlignedLabel = new GUIStyle( GUI.skin.label ) { alignment = TextAnchor.MiddleRight };
            public static readonly GUIContent DelayLabel = new GUIContent( "dT", "The delay before the binding is updated." );
            public static readonly GUIContent UnscaledTimeLabel = new GUIContent( "unscaled", "If true, the binding will use unscaled time for the delay." );
        }
    }
}