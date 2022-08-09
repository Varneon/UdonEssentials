using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Varneon.UdonPrefabs.Abstract.Editor
{
    [CustomEditor(typeof(VisibilitySensorDescriptor))]
    public class VisibilitySensorDescriptorEditor : UnityEditor.Editor
    {
        private VisibilitySensorDescriptor descriptor;

        private Transform root;

        private LODGroup lodGroup;

        private MeshFilter meshFilter;

        private MeshRenderer meshRenderer;

        private SerializedProperty transitionHeightProperty;

        private SerializedProperty targetProperty;

        private SerializedObject so;

        private bool hasValidEncapsulationTarget;

        private readonly List<Type> validEncapsulationComponents = new List<Type>()
        {
            typeof(BoxCollider),
            typeof(Canvas),
            typeof(MeshCollider),
            typeof(MeshFilter),
            typeof(SkinnedMeshRenderer),
            typeof(SphereCollider)
        };

        private void OnEnable()
        {
            descriptor = (VisibilitySensorDescriptor)target;

            root = descriptor.transform;

            so = new SerializedObject(descriptor);

            transitionHeightProperty = so.FindProperty("TransitionHeight");

            targetProperty = so.FindProperty("EncapsulationTarget");

            lodGroup = descriptor.GetComponent<LODGroup>();

            meshFilter = descriptor.GetComponent<MeshFilter>();

            meshRenderer = descriptor.GetComponent<MeshRenderer>();

            hasValidEncapsulationTarget = descriptor.EncapsulationTarget != null;

            if (descriptor.Initialized) { return; }

            Undo.RecordObject(descriptor.gameObject, "Initialize Visibility Sensor");

            lodGroup.hideFlags = HideFlags.HideInInspector;

            meshFilter.hideFlags = HideFlags.HideInInspector;

            meshRenderer.hideFlags = HideFlags.HideInInspector;

            ApplyTransitionHeight(0.4f);

            Mesh cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            if(meshFilter.sharedMesh != cubeMesh)
            {
                meshFilter.sharedMesh = cubeMesh;
            }

            meshRenderer.materials = new Material[0];

            descriptor.Initialized = true;
        }

        public override void OnInspectorGUI()
        {
            so.Update();

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(transitionHeightProperty);

                if (scope.changed)
                {
                    so.ApplyModifiedProperties();

                    ApplyTransitionHeight(descriptor.TransitionHeight);
                }
            }

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(targetProperty);

                if (scope.changed)
                {
                    so.ApplyModifiedProperties();

                    hasValidEncapsulationTarget = descriptor.EncapsulationTarget != null;
                }
            }

            if (hasValidEncapsulationTarget)
            {
                if (GUILayout.Button("Encapsulate"))
                {
                    GenericMenu menu = new GenericMenu();

                    foreach (Component component in descriptor.EncapsulationTarget.GetComponentsInChildren<Component>().Where(c => validEncapsulationComponents.Contains(c.GetType())))
                    {
                        string path = AnimationUtility.CalculateTransformPath(component.transform, descriptor.EncapsulationTarget.transform);

                        menu.AddItem(new GUIContent(string.Format("{0}/{1}", string.IsNullOrEmpty(path) ? "<SELF>" : path, component.GetType().Name)), false, () => EncapsulateComponent(component));
                    }

                    menu.ShowAsContext();
                }
            }
        }

        private void ApplyTransitionHeight(float transitionHeight)
        {
            lodGroup.SetLODs(new LOD[] {
                new LOD(transitionHeight, new Renderer[]{ meshRenderer }),
                new LOD(0f, new Renderer[0])
            });
        }

        private void OnSceneGUI()
        {
            using (new Handles.DrawingScope())
            {
                Handles.zTest = CompareFunction.Less;
                Handles.color = meshRenderer.isVisible ? Color.green : Color.red;
                Handles.matrix = Matrix4x4.TRS(root.position, root.rotation, root.lossyScale);
                Handles.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }

        private void EncapsulateComponent(Component component)
        {
            Transform transform = component.transform;

            root.SetPositionAndRotation(transform.position, transform.rotation);

            Bounds bounds = new Bounds();

            if (component.GetType() == typeof(Canvas))
            {
                Canvas canvas = (Canvas)component;

                RectTransform rectTransform = canvas.GetComponent<RectTransform>();

                Rect rect = rectTransform.rect;

                Vector3 lossyScale = rectTransform.lossyScale;

                Vector3[] corners = new Vector3[]
                {
                    Vector3.Scale(new Vector3(-rect.width / 2f, -rect.height / 2f, 0f), lossyScale),
                    Vector3.Scale(new Vector3(rect.width / 2f, -rect.height / 2f, 0f), lossyScale),
                    Vector3.Scale(new Vector3(rect.width / 2f, rect.height / 2f, 0f), lossyScale),
                    Vector3.Scale(new Vector3(-rect.width / 2f, rect.height / 2f, 0f), lossyScale),
                };

                foreach (Vector3 v in corners)
                {
                    bounds.Encapsulate(v);
                }

                root.localScale = bounds.extents * 2f;
            }
            else if (component.GetType() == typeof(MeshFilter))
            {
                MeshFilter filter = (MeshFilter)component;

                EncapsulateByLocalBounds(filter.transform, filter.sharedMesh.bounds);
            }
            else if (component.GetType() == typeof(SkinnedMeshRenderer))
            {
                SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)component;

                EncapsulateByLocalBounds(renderer.transform, renderer.bounds);
            }
            else if (component.GetType() == typeof(MeshCollider))
            {
                MeshCollider collider = (MeshCollider)component;

                EncapsulateByLocalBounds(collider.transform, collider.bounds);
            }
            else if (component.GetType().IsSubclassOf(typeof(Collider)))
            {
                Collider collider = (Collider)component;

                Vector3 lossyScale = transform.lossyScale;

                switch (component.GetType().Name)
                {
                    case "BoxCollider":
                        BoxCollider boxCollider = (BoxCollider)collider;

                        root.Translate(Vector3.Scale(boxCollider.center, lossyScale), Space.Self);

                        root.localScale = Vector3.Scale(boxCollider.size, lossyScale);
                        break;
                    case "SphereCollider":
                        SphereCollider sphereCollider = (SphereCollider)collider;

                        root.Translate(Vector3.Scale(sphereCollider.center, lossyScale), Space.Self);

                        root.localScale = lossyScale * sphereCollider.radius * 2f;
                        break;
                }
            }
        }

        private void EncapsulateByLocalBounds(Transform transform, Bounds bounds)
        {
            Vector3 lossyScale = transform.lossyScale;

            root.Translate(Vector3.Scale(bounds.center, lossyScale), Space.Self);

            root.localScale = Vector3.Scale(bounds.extents * 2f, lossyScale);
        }
    }
}
