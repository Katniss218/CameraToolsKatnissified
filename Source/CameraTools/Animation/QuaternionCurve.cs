﻿using UnityEngine;

namespace CameraToolsKatnissified.Animation
{
    /// <summary>
    /// Used for pathing.
    /// </summary>
    public class QuaternionCurve
    {
        Quaternion[] _rotations;
        float[] _times;

        public QuaternionCurve( Quaternion[] rots, float[] times )
        {
            this._rotations = rots;
            this._times = times;
        }

        public Quaternion Evaluate( float t )
        {
            int startIndex = 0;
            for( int i = 0; i < _times.Length; i++ )
            {
                if( t >= _times[i] )
                {
                    startIndex = i;
                }
                else
                {
                    break;
                }
            }

            int nextIndex = Mathf.RoundToInt( Mathf.Min( startIndex + 1, _times.Length - 1 ) );

            float overTime = t - _times[startIndex];
            float intervalTime = _times[nextIndex] - _times[startIndex];
            if( intervalTime <= 0 )
            {
                return _rotations[nextIndex];
            }

            float normTime = overTime / intervalTime;
#warning TODO - slerp?
            return Quaternion.Lerp( _rotations[startIndex], _rotations[nextIndex], normTime );
        }
    }
}