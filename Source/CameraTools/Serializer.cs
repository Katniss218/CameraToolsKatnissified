using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToolsKatnissified
{
    public static class Serializer
    {
        static string SETTINGS_PATH = $"GameData/{CameraToolsBehaviour.DIRECTORY_NAME}/settings.cfg";

        /// <summary>
        /// Saves the settings.
        /// </summary>
        public static void SaveFields()
        {
            ConfigNode fileNode = ConfigNode.Load( SETTINGS_PATH );
            ConfigNode settings = fileNode.GetNode( "CToolsSettings" );

            foreach( var field in typeof( CameraToolsBehaviour ).GetFields() )
            {
                if( !field.IsDefined( typeof( PersistentField ), false ) )
                {
                    continue;
                }

                settings.SetValue( field.Name, field.GetValue( CameraToolsBehaviour.Instance ).ToString(), true );
            }

            fileNode.Save( SETTINGS_PATH );
        }

        /// <summary>
        /// Loads and deserialized the settings.
        /// </summary>
        public static void LoadFields()
        {
            ConfigNode fileNode = ConfigNode.Load( SETTINGS_PATH );
            ConfigNode settings = fileNode.GetNode( "CToolsSettings" );

            foreach( var field in typeof( CameraToolsBehaviour ).GetFields() )
            {
                if( !field.IsDefined( typeof( PersistentField ), false ) )
                {
                    continue;
                }

                if( settings.HasValue( field.Name ) )
                {
                    object parsedValue = ParseAs( field.FieldType, settings.GetValue( field.Name ) );

                    if( parsedValue != null )
                    {
                        field.SetValue( CameraToolsBehaviour.Instance, parsedValue );
                    }
                }
            }
        }

        /// <summary>
        /// Converts a string value into an appropriate type.
        /// </summary>
        public static object ParseAs( Type type, string value )
        {
            if( type == typeof( string ) )
            {
                return value;
            }

            if( type == typeof( bool ) )
            {
                return bool.Parse( value );
            }

            if( type.IsEnum )
            {
                return Enum.Parse( type, value );
            }

            if( type == typeof( float ) )
            {
                return float.Parse( value );
            }

            Debug.LogError( $"CameraTools failed to parse field of type '{type}' and value '{value}'." );
            return null;
        }
    }
}