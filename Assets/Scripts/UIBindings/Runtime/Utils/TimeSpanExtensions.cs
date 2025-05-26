using System;

namespace UIBindings.Runtime.Utils
{
    public static class TimeSpanExtensions
    {
        public const Double TicksPerMicroseconds = 10;
        
        /// <summary>
        /// Mostly for debugging microtimers
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public        static double TotalMicroseconds( this TimeSpan timeSpan )
        {
            return timeSpan.Ticks / TicksPerMicroseconds;
        }
    }
}