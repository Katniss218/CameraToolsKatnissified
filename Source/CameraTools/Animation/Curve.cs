using System;
using System.Collections.Generic;
using UnityEngine;

namespace CameraToolsKatnissified.Animation
{
    public class Curve<T> // kind of a spline, but doesn't require the number of points to be a multiple of the degree of the spline.
    {
        struct Entry
        {
            public T value;
            public float time;

            public static int Compare( Entry left, Entry right )
            {
                if( left.time == right.time )
                {
                    return 0;
                }
                return left.time < right.time ? -1 : 1;
            }

            public override string ToString()
            {
                return $"({time}:{value})";
            }
        }

        Entry[] _points;
        float timeStart;
        float timeEnd;

        public Curve( T[] values, float[] times )
        {
            if( values.Length != times.Length )
            {
                throw new ArgumentException( "The length of the Values array must be the same as the length of the Times array." );
            }

            _points = new Entry[values.Length];
            for( int i = 0; i < values.Length; i++ )
            {
                _points[i] = new Entry() { value = values[i], time = times[i] };
            }
            Array.Sort<Entry>( _points, Entry.Compare );
            timeStart = _points[0].time;
            timeEnd = _points[_points.Length - 1].time;
        }

        int FindFirstPointAheadOf( float t )
        {
            for( int i = 0; i < _points.Length; i++ )
            {
                if( _points[i].time > t )
                {
                    return i;
                }
            }
            return -1;
        }

        int FindCurveInterval( float t, int length )
        {
            // find the beginning of the subarray of length l that the time t lies in.
            // TODO - what if the array is not a multiple of length?
            for( int i = 0; i < _points.Length; i++ )
            {
                if( _points[i].time > t ) // what is the start of [i-1..i] lies in?
                {
                    return (i / length) * length;
                }
            }
            return -1;
        }

        public T EvaluateLinearBezier( Func<T, T, float, T> interpolator, float t ) // interpolator is lerp or slerp
        {
            if( t <= timeStart )
                return _points[0].value;
            if( t >= timeEnd )
                return _points[_points.Length - 1].value;

            int indexOfSecondPoint = FindFirstPointAheadOf( t );
            if( indexOfSecondPoint == -1 )
            {
                throw new InvalidOperationException( $"The time '{t}' was outside of the curve [{timeStart}..{timeEnd}]." );
            }

            int i0 = indexOfSecondPoint - 1;
            int i1 = indexOfSecondPoint;

            Entry p0 = _points[i0];
            Entry p1 = _points[i1];

            float intervalTime = p1.time - p0.time;
            if( intervalTime == 0 )
            {
                throw new InvalidOperationException( $"The interval between points {i0} and {i1} was 0." );
            }
            float timeOnInterval = (t - p0.time) / (intervalTime);

            T p0_1 = interpolator( p0.value, p1.value, timeOnInterval );

            return p0_1;
        }

        public T EvaluateQuadraticBezier( Func<T, T, float, T> interpolator, float t ) // interpolator is lerp or slerp
        {
            if( t <= timeStart )
                return _points[0].value;
            if( t >= timeEnd )
                return _points[_points.Length - 1].value;

            int indexOfFirstPoint = FindCurveInterval( t, 3 );
            if( indexOfFirstPoint == -1 )
            {
                throw new InvalidOperationException( $"The time '{t}' was outside of the curve [{timeStart}..{timeEnd}]." );
            }

            // quadratic and higher order curves we don't determine which point lies "after" t, but in which segment the t lies.
            int i0 = indexOfFirstPoint;
            int i1 = indexOfFirstPoint + 1;
            int i2 = indexOfFirstPoint + 2;

            if( i1 >= _points.Length ) i1 = i0;
            if( i2 >= _points.Length ) i2 = i1;

            Entry p0 = _points[i0];
            Entry p1 = _points[i1];
            Entry p2 = _points[i2];

            float intervalTime = p2.time - p0.time;
            if( intervalTime == 0 )
            {
                throw new InvalidOperationException( $"The interval between points {i0} and {i2} was 0." );
            }
            float timeOnInterval = Mathf.InverseLerp( p0.time, p2.time, t );

            // start point can have a time that makes the camera wait.
            // only the start and end points of each subinterval have a time value, and they need the same T as the previous point.
            //Debug.Log( $"{i0}, {i1}, {i2}" );
            //Debug.Log( $"{p0.time}, {p1.time}, {p2.time}, T: {t}, Ti: {timeOnInterval}" );

            T p0_1 = interpolator( p0.value, p1.value, timeOnInterval );
            T p1_2 = interpolator( p1.value, p2.value, timeOnInterval );
            T p01_12 = interpolator( p0_1, p1_2, timeOnInterval );

            return p01_12;
        }

        public T EvaluateCubicBezier( Func<T, T, float, T> interpolator, float t ) // interpolator is lerp or slerp
        {
            if( t <= timeStart )
                return _points[0].value;
            if( t >= timeEnd )
                return _points[_points.Length - 1].value;

            int indexOfFirstPoint = FindCurveInterval( t, 4 );
            if( indexOfFirstPoint == -1 )
            {
                throw new InvalidOperationException( $"The time '{t}' was outside of the curve [{timeStart}..{timeEnd}]." );
            }

            // quadratic and higher order curves we don't determine which point lies "after" t, but in which segment the t lies.
            int i0 = indexOfFirstPoint;
            int i1 = indexOfFirstPoint + 1;
            int i2 = indexOfFirstPoint + 2;
            int i3 = indexOfFirstPoint + 3;

            if( i1 >= _points.Length ) i1 = i0;
            if( i2 >= _points.Length ) i2 = i1;
            if( i3 >= _points.Length ) i3 = i2;

            Entry p0 = _points[i0];
            Entry p1 = _points[i1];
            Entry p2 = _points[i2];
            Entry p3 = _points[i3];

            float intervalTime = p3.time - p0.time;
            if( intervalTime == 0 )
            {
                throw new InvalidOperationException( $"The interval between points {i0} and {i3} was 0." );
            }
            float timeOnInterval = Mathf.InverseLerp( p0.time, p3.time, t );

            // start point can have a time that makes the camera wait.
            // only the start and end points of each subinterval have a time value, and they need the same T as the previous point.
            //Debug.Log( $"{i0}, {i1}, {i2}" );
            //Debug.Log( $"{p0.time}, {p1.time}, {p2.time}, T: {t}, Ti: {timeOnInterval}" );

            T p0_1 = interpolator( p0.value, p1.value, timeOnInterval );
            T p1_2 = interpolator( p1.value, p2.value, timeOnInterval );
            T p2_3 = interpolator( p2.value, p3.value, timeOnInterval );
            T p01_12 = interpolator( p0_1, p1_2, timeOnInterval );
            T p12_23 = interpolator( p1_2, p2_3, timeOnInterval );
            T p0112_1223 = interpolator( p01_12, p12_23, timeOnInterval );

            return p0112_1223;
        }
    }
}