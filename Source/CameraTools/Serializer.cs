using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CameraToolsKatnissified
{
    public static class Serializer
    {
        static string SETTINGS_PATH = $"GameData/{CameraToolsManager.DIRECTORY_NAME}/settings.cfg";

        /// <summary>
        /// Saves the settings of the <see cref="CameraToolsManager"/>.
        /// </summary>
        public static void SaveFields()
        {
            ConfigNode fileNode = ConfigNode.Load( SETTINGS_PATH );
            ConfigNode settings = fileNode.GetNode( "CToolsSettings" );

            CameraToolsManager ctb = Object.FindObjectOfType<CameraToolsManager>();
            var fields = typeof( CameraToolsManager ).GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

            foreach( var field in fields )
            {
                if( !field.IsDefined( typeof( PersistentField ), false ) )
                {
                    continue;
                }

                settings.SetValue( field.Name, Utils.ValueToString( field.GetValue( ctb ) ), true );
            }

            fileNode.Save( SETTINGS_PATH );
        }

        /// <summary>
        /// Loads and deserializes the settings of the <see cref="CameraToolsManager"/>.
        /// </summary>
        public static void LoadFields()
        {
            ConfigNode fileNode = ConfigNode.Load( SETTINGS_PATH );
            ConfigNode settings = fileNode.GetNode( "CToolsSettings" );

            CameraToolsManager ctb = Object.FindObjectOfType<CameraToolsManager>();
            var fields = typeof( CameraToolsManager ).GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

            foreach( var field in fields )
            {
                if( !field.IsDefined( typeof( PersistentField ), false ) )
                {
                    continue;
                }

                if( settings.HasValue( field.Name ) )
                {
                    object parsedValue = Utils.StringToValue( field.FieldType, settings.GetValue( field.Name ) );

                    field.SetValue( ctb, parsedValue );
                }
            }
        }
    }
}