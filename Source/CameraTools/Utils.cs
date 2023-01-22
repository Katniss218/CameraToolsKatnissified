using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public static class Utils
    {
        public static Part GetPartFromMouse()
        {
            Vector3 mouseAim = new Vector3( Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0 );
            Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay( mouseAim );
            RaycastHit hit;
            if( Physics.Raycast( ray, out hit, 10000, 1 << 0 ) )
            {
                Part p = hit.transform.GetComponentInParent<Part>();
                return p;
            }
            else return null;
        }

        public static Vector3? GetPosFromMouse()
        {
            Vector3 mouseAim = new Vector3( Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0 );
            Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay( mouseAim );

            const int layerMask = 0b10001000000000000001;

            if( Physics.Raycast( ray, out RaycastHit hit, 15000.0f, layerMask ) )
            {
                return hit.point - (10 * ray.direction);
            }

            return null;
        }

        public static string WriteVectorList( List<Vector3> list )
        {
            string output = string.Empty;
            foreach( var val in list )
            {
                output += ConfigNode.WriteVector( val ) + ";";
            }
            return output;
        }

        public static string WriteQuaternionList( List<Quaternion> list )
        {
            string output = string.Empty;
            foreach( var val in list )
            {
                output += ConfigNode.WriteQuaternion( val ) + ";";
            }
            return output;
        }

        public static string WriteFloatList( List<float> list )
        {
            string output = string.Empty;
            foreach( var val in list )
            {
                output += val.ToString() + ";";
            }
            return output;
        }

        public static List<Vector3> ParseVectorList( string arrayString )
        {
            string[] vectorStrings = arrayString.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
            List<Vector3> vList = new List<Vector3>();
            for( int i = 0; i < vectorStrings.Length; i++ )
            {
                Debug.Log( "attempting to parse vector: --" + vectorStrings[i] + "--" );
                vList.Add( ConfigNode.ParseVector3( vectorStrings[i] ) );
            }

            return vList;
        }

        public static List<Quaternion> ParseQuaternionList( string arrayString )
        {
            string[] qStrings = arrayString.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
            List<Quaternion> qList = new List<Quaternion>();
            for( int i = 0; i < qStrings.Length; i++ )
            {
                qList.Add( ConfigNode.ParseQuaternion( qStrings[i] ) );
            }

            return qList;
        }

        public static List<float> ParseFloatList( string arrayString )
        {
            string[] fStrings = arrayString.Split( new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
            List<float> fList = new List<float>();
            for( int i = 0; i < fStrings.Length; i++ )
            {
                fList.Add( float.Parse( fStrings[i] ) );
            }

            return fList;
        }

    }
}