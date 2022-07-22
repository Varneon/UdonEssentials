using System.Collections.Generic;
using UdonSharp;
using UnityEditor;
using UnityEngine.UIElements;
using Varneon.VInspector;
using Varneon.UdonPrefabs.Core;

namespace Varneon.UdonPrefabs.RuntimeTools.Editor
{
    /// <summary>
    /// Custom inspector for UdonConsole prefab
    /// </summary>
    [CustomEditor(typeof(UdonConsole))]
    [IgnoreFieldsOfType(typeof(UdonSharpBehaviour))]
    public class UdonConsoleEditor : NeonInspector
    {
        private const string FOLDOUT_PERSISTENCE_KEY = "Varneon/UdonPrefabs/RuntimeTools/UdonConsole/Editor/Foldouts";

        private List<Foldout> foldouts;

        protected override void OnInspectorVisualTreeAssetCloned(VisualElement root)
        {
            base.OnInspectorVisualTreeAssetCloned(root);

            VisualElement inspectorPanel = root.Q("InspectorPanel");

            inspectorPanel.Add(new Foldout() { name = "Foldout_Settings", text = "Settings" });
            inspectorPanel.Add(new Foldout() { name = "Foldout_References", text = "References" });
            inspectorPanel.Add(new Foldout() { name = "Foldout_API", text = "API" });

            foldouts = root.Query<Foldout>().Build().ToList();

            if (EditorPrefs.HasKey(FOLDOUT_PERSISTENCE_KEY))
            {
                int states = EditorPrefs.GetInt(FOLDOUT_PERSISTENCE_KEY);

                for (int i = 0; i < foldouts.Count; i++)
                {
                    foldouts[i].value = (states & (1 << i)) != 0;
                }
            }

            APIDocumentationBuilder.BuildAPIDocumentation(root.Q<Foldout>("Foldout_API"), typeof(UdonLogger));
        }

        private void OnDestroy()
        {
            // If foldouts is null, then OnDestroy was most likely called by prefab override preview
            if (foldouts == null) { return; }

            int states = 0;

            for (int i = 0; i < foldouts.Count; i++)
            {
                if (foldouts[i].value)
                {
                    states |= 1 << i;
                }
            }

            EditorPrefs.SetInt(FOLDOUT_PERSISTENCE_KEY, states);
        }
    }
}
