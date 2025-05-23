﻿using JetBrains.Annotations;

namespace UIBindings.Editor.Utils
{
    public static class RectExtensions
    {
        [Pure]
        public static UnityEngine.Rect Translate(this UnityEngine.Rect rect, UnityEngine.Vector2 offset)
        {
            return new UnityEngine.Rect(rect.position + offset, rect.size);
        }
    }
}