using UnityEditor;
using UnityEngine;

namespace UIBindings.Runtime.Utils
{
    public class EditorGUIUtils
    {
        public class ZeroLevelScope : GUI.Scope
        {
            private readonly int m_IndentOffset;

            public ZeroLevelScope()
            {
                m_IndentOffset = EditorGUI.indentLevel;
                EditorGUI.indentLevel -= m_IndentOffset;
            }

            protected override void CloseScope() => EditorGUI.indentLevel += m_IndentOffset;
        }
    }
}