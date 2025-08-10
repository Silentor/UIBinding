using System;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Editor.Utils
{
    public static class EditorGUIUtils
    {
        public static IDisposable ChangeContentColor( Color contentColor )
        {
            return new ContentColorScope( contentColor );
        }

        public static IDisposable ZeroIndent()
        {
            return new ZeroIndentScope( );
        }

        public static (Rect, Rect, Rect) GetHorizontalRects(Rect totalPosition, float padding, ControlRectSettings control1, ControlRectSettings control2, ControlRectSettings control3, bool debugDrawRects = false )
        {
            float totalFixedWidth = 0f;
            int autoCount = 0;
            ControlRectSettings[] settings = { control1, control2, control3 };
            for (int i = 0; i < 3; i++)
            {
                if (settings[i].Width > 0)
                    totalFixedWidth += settings[i].Width;
                else
                    autoCount++;
            }
            float totalPadding = padding * 2;
            float remainingWidth = totalPosition.width - totalFixedWidth - totalPadding;
            float autoWidth = autoCount > 0 ? Mathf.Max(0, remainingWidth / autoCount) : 0f;
            float x = totalPosition.x;
            Rect[] rects = new Rect[3];
            for (int i = 0; i < 3; i++)
            {
                float width = settings[i].Width > 0 ? settings[i].Width : autoWidth;
                rects[i] = new Rect(x, totalPosition.y, width, totalPosition.height);
                x += width + padding;
            }

#if UNITY_EDITOR
            if( debugDrawRects && Event.current.type == EventType.Repaint )
            {
                GUI.DrawTexture( rects[0], Texture2D.blackTexture, ScaleMode.StretchToFill, false );
                GUI.DrawTexture( rects[1], Texture2D.whiteTexture, ScaleMode.StretchToFill, false );
                GUI.DrawTexture( rects[2], Texture2D.redTexture,   ScaleMode.StretchToFill, false );
            }
#endif
            return (rects[0], rects[1], rects[2]);
        }

        public static (Rect, Rect) GetHorizontalRects(Rect totalPosition, float padding, ControlRectSettings control1, ControlRectSettings control2)
        {
            float totalFixedWidth = 0f;
            int autoCount = 0;
            ControlRectSettings[] settings = { control1, control2 };
            for (int i = 0; i < 2; i++)
            {
                if (settings[i].Width > 0)
                    totalFixedWidth += settings[i].Width;
                else
                    autoCount++;
            }
            float totalPadding = padding * 1;
            float remainingWidth = totalPosition.width - totalFixedWidth - totalPadding;
            float autoWidth = autoCount > 0 ? Mathf.Max(0, remainingWidth / autoCount) : 0f;
            float x = totalPosition.x;
            Rect[] rects = new Rect[2];
            for (int i = 0; i < 2; i++)
            {
                float width = settings[i].Width > 0 ? settings[i].Width : autoWidth;
                rects[i] = new Rect(x, totalPosition.y, width, totalPosition.height);
                x += width + padding;
            }
            return (rects[0], rects[1]);
        }

        public static (Rect, Rect, Rect) Translate( this (Rect, Rect, Rect) rects, Vector2 offset ) 
        {
            return ( rects.Item1.Translate( offset ), rects.Item2.Translate( offset ), rects.Item3.Translate( offset ) );
        }

        public static (Rect, Rect) Translate( this (Rect, Rect) rects, Vector2 offset ) 
        {
            return ( rects.Item1.Translate( offset ), rects.Item2.Translate( offset ) );
        }

        public static bool TryGetCursorPositionInTextField( out int cursorCharPosition )
        {
            cursorCharPosition = -1;
            // Get the TextEditor object for the focused control.
            // This is a bit of a "hack" but is the standard approach for IMGUI.
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            
            if (editor != null)
            {
                cursorCharPosition = editor.cursorIndex;
                return true;
            }

            return false;
        }


        private class ContentColorScope : GUI.Scope
        {
            private readonly Color _oldContentColor;

            public ContentColorScope( Color contentColor )
            {
                _oldContentColor = GUI.contentColor;
                GUI.contentColor = contentColor;
            }

            protected override void CloseScope( )
            {
                GUI.contentColor = _oldContentColor;
            }
        }

        private class ZeroIndentScope : GUI.Scope
        {
            private readonly int _oldIndent;

            public ZeroIndentScope(  )
            {
                _oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
            }

            protected override void CloseScope( )
            {
                EditorGUI.indentLevel = _oldIndent;
            }
        }

        public readonly struct ControlRectSettings
        {
            //Fixed width or 0 for auto
            public readonly float Width;

            public ControlRectSettings(Single width )
            {
                Width = width;
            }

            public static implicit operator ControlRectSettings(Single width )
            {
                return new ControlRectSettings( width );
            }
        }
    }
}

