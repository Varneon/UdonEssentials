using UdonSharp;
using UnityEditor;
using UnityEngine.UIElements;
using Varneon.VInspector;

namespace Varneon.UdonPrefabs.Core.Editor
{
    /// <summary>
    /// Custom inspector for EventDispatcher prefab
    /// </summary>
    [CustomEditor(typeof(EventDispatcher))]
    [IgnoreFieldsOfType(typeof(UdonSharpBehaviour))]
    public class EventDispatcherEditor : NeonInspector
    {
        protected override void OnInspectorVisualTreeAssetCloned(VisualElement root)
        {
            VisualElement inspectorPanel = root.Q("InspectorPanel");

            Foldout apiFoldout = new Foldout() { name = "Foldout_API", text = "API", value = false };

            inspectorPanel.Add(apiFoldout);

            APIDocumentationBuilder.BuildAPIDocumentation(apiFoldout, typeof(EventDispatcher));

            base.OnInspectorVisualTreeAssetCloned(root);
        }
    }
}
