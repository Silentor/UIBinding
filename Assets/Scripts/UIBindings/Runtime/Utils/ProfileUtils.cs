using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UIBindings.Runtime.Utils
{
    public static class ProfileUtils
    {
        public static TraceTimer GetTraceTimer( )
        {
            return new TraceTimer( );
        }

        public class TraceTimer
        {
            public readonly Stopwatch Timer = new ( );
            public          TimeSpan Elapsed => Timer.Elapsed;

            private readonly List<(String, TimeSpan)> Markers = new ( 16 );

            public TraceTimer( )
            {
                Timer.Start();
            }

            public void AddMarker( String name )
            {
                Markers.Add( (name, Timer.Elapsed) );
            }

            public String GetReport( )
            {
                Timer.Stop();

                if( Markers.Count == 0 )
                    return "No markers recorded.";

                var result = new StringBuilder(1024);
                result.Append( Markers[ 0 ].Item1 );
                result.Append( ": " );
                result.Append( Markers[ 0 ].Item2.TotalMicroseconds() );
                result.Append( " mks, " );

                for ( int i = 1; i < Markers.Count; i++ )
                {
                    result.Append( Markers[ i ].Item1 );
                    result.Append( ": " );
                    result.Append( (Markers[ i ].Item2 - Markers[ i-1 ].Item2).TotalMicroseconds() );
                    result.Append( " mks, " );
                }

                return result.ToString();
            }
        }        
    }
}