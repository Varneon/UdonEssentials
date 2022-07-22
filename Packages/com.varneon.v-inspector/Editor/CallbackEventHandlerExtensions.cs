using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

namespace Varneon.VInspector
{
    public static class CallbackEventHandlerExtensions
    {
        public static void RegisterPrefabPropertyOverrideContextClickEvent(this CallbackEventHandler handler, Object target, SerializedProperty property)
        {
            handler.RegisterCallback<ContextClickEvent>(a =>
            {
                property = new SerializedObject(target).FindProperty(property.name);

                if (property.prefabOverride)
                {
                    GenericMenu menu = new GenericMenu();

                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);

                    string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                    menu.AddItem(new GUIContent($"Apply to Prefab '{prefabName}'"), false, () => PrefabUtility.ApplyPropertyOverride(property, prefabPath, InteractionMode.AutomatedAction));
                    menu.AddItem(new GUIContent("Revert"), false, () => PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction));

                    menu.ShowAsContext();
                }
            });
        }
    }
}
