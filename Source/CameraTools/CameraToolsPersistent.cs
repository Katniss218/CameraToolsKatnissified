using System;
using UnityEngine;

namespace CameraToolsKatnissified
{
    /// <summary>
    /// A marker attribute to persist the value of a field.
    /// </summary>
    [AttributeUsage( AttributeTargets.Field )]
    public class CameraToolsPersistent : Attribute
    {
        public static string settingsPath = $"GameData/{CamTools.DIRECTORY_NAME}/settings.cfg";

        public CameraToolsPersistent()
        { }

#warning TODO - move these to a more appropriate place.
        /// <summary>
        /// Saves the settings.
        /// </summary>
        public static void Save()
        {
            ConfigNode fileNode = ConfigNode.Load( settingsPath );
            ConfigNode settings = fileNode.GetNode( "CToolsSettings" );

            foreach( var field in typeof( CamTools ).GetFields() )
            {
                if( !field.IsDefined( typeof( CameraToolsPersistent ), false ) )
                {
                    continue;
                }

                settings.SetValue( field.Name, field.GetValue( CamTools.Instance ).ToString(), true );
            }

            fileNode.Save( settingsPath );
        }

        /// <summary>
        /// Loads and deserialized the settings.
        /// </summary>
        public static void Load()
        {
            ConfigNode fileNode = ConfigNode.Load( settingsPath );
            ConfigNode settings = fileNode.GetNode( "CToolsSettings" );

            foreach( var field in typeof( CamTools ).GetFields() )
            {
                if( !field.IsDefined( typeof( CameraToolsPersistent ), false ) )
                {
                    continue;
                }

                if( settings.HasValue( field.Name ) )
                {
                    object parsedValue = ParseAs( field.FieldType, settings.GetValue( field.Name ) );

                    if( parsedValue != null )
                    {
                        field.SetValue( CamTools.Instance, parsedValue );
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