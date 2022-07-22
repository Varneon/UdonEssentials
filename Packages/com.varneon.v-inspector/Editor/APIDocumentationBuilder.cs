using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using System.Reflection;
using JetBrains.Annotations;
using System.Linq;
using UnityEditor;

namespace Varneon.VInspector
{
    public static class APIDocumentationBuilder
    {
        public static void BuildAPIDocumentation(this VisualElement root, Type type, Type[] excludedParentsAndInterfaces = null)
        {
            // Get all parent types of the class
            IEnumerable<Type> baseTypes = TypeUtilities.GetParentsAndInterfaces(type);

            if(excludedParentsAndInterfaces != null && excludedParentsAndInterfaces.Length > 0)
            {
                baseTypes = baseTypes.Except(excludedParentsAndInterfaces);
            }

            // Get all runtime fields from all types
            IEnumerable<MethodInfo> fields = type.GetRuntimeMethods().Union(baseTypes.SelectMany(t => t.GetRuntimeMethods()));

            foreach (MethodInfo method in type.GetRuntimeMethods())
            {
                if (method.DeclaringType == type && Attribute.IsDefined(method, typeof(PublicAPIAttribute)))
                {
                    Label newLabel = new Label(string.Format("{0}({1})", method.Name, string.Join(", ", method.GetParameters().Select(p => string.Format("<{0}> {1}", p.ParameterType.Name, p.Name)))));

                    newLabel.userData = method.Name;

                    PublicAPIAttribute publicAPIAttribute = method.GetCustomAttribute<PublicAPIAttribute>();

                    bool hasPublicAPIAttributeComment = !string.IsNullOrEmpty(publicAPIAttribute.Comment);

                    if (hasPublicAPIAttributeComment)
                    {
                        newLabel.tooltip = publicAPIAttribute.Comment;
                    }

                    newLabel.RegisterCallback<ContextClickEvent>(a =>
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("Copy Method Name"), false, () => EditorGUIUtility.systemCopyBuffer = (string)newLabel.userData);

                        menu.ShowAsContext();
                    });

                    root.Add(newLabel);
                }
            }
        }
    }
}
