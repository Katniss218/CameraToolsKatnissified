using UnityEngine;

namespace CameraToolsKatnissified.Animation
{
    /// <summary>
    /// Used for pathing.
    /// </summary>
    public class Vector3Curve
    {
        Vector3[] _positions;
        float[] _times;

        public Vector3Curve( Vector3[] pos, float[] times )
        {
            this._positions = pos;
            this._times = times;
        }

        public Vector3 Evaluate( float t )
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
                return _positions[nextIndex];
            }

            float normTime = overTime / intervalTime;

            // This should probably do a cubic or something, maybe swap between different modes. Lerp is way too jittery.
            return Vector3.Lerp( _positions[startIndex], _positions[nextIndex], normTime );
        }
    }
}

