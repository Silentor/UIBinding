using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Profiling;

namespace UIBindings.Runtime.Utils
{
    public static class ProfileUtils
    {
        public static TraceTimer GetTraceTimer( bool trackAllocations = false )
        {
            return new TraceTimer( trackAllocations );
        }

        public class TraceTimer
        {
            public readonly Stopwatch Timer = new ( );
            public          TimeSpan Elapsed => Timer.Elapsed;

            private readonly List<(String, TimeSpan, long)> Markers = new ( 16 );
            private          ProfilerRecorder _gcAllocationsCounter;
            private readonly Int64 _startAllocated;

            public TraceTimer( bool trackAllocations = false )
            {
                if ( trackAllocations )
                {
                    _gcAllocationsCounter = new ProfilerRecorder(ProfilerCategory.Memory, "GC Allocated In Frame", 0);
                    _startAllocated = _gcAllocationsCounter.CurrentValue;
                }
                Timer.Start();
            }

            public void AddMarker( String name )
            {
                var allocated = _gcAllocationsCounter.Valid ? _gcAllocationsCounter.CurrentValue : 0;
                Markers.Add( (name, Timer.Elapsed, allocated) );
            }

            public String StopAndGetReport( )
            {
                Timer.Stop();
                var allocRecorderValid = _gcAllocationsCounter.Valid;
                _gcAllocationsCounter.Dispose();

                if( Markers.Count == 0 )
                    return "No markers recorded.";

                if( Markers.Count == 1 )
                    return "Only one marker recorded, no useful report can be generated.";

                var result = new StringBuilder(1024);
                result.Append( " " );
                result.Append( Markers[ 0 ].Item1 );
                result.Append( ": " );
                result.Append( Markers[ 0 ].Item2.TotalMicroseconds() );
                result.Append( " mks" );
                if ( allocRecorderValid )
                {
                    result.Append( ", " );
                    result.Append( Markers[ 0 ].Item3 - _startAllocated );
                    result.Append( " b" );
                }
                result.Append( ";" );

                for ( int i = 1; i < Markers.Count; i++ )
                {
                    result.Append( " " );
                    result.Append( Markers[ i ].Item1 );
                    result.Append( ": " );
                    result.Append( (Markers[ i ].Item2 - Markers[ i - 1 ].Item2).TotalMicroseconds() );
                    result.Append( " mks" );
                    if ( allocRecorderValid )
                    {
                        result.Append( ", " );
                        result.Append( Markers[ i ].Item3 - Markers[ i - 1 ].Item3 );
                        result.Append( " b" );
                    }
                    result.Append( ";" );
                }

                return result.ToString();
            }
        }        
    }
}