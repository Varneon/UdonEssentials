using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Varneon.VInspector
{
    /// <summary>
    /// Base class for custom inspector
    /// </summary>
    public abstract class InspectorBase : Editor
    {
        /// <summary>
        /// UXML root asset for the inspector
        /// </summary>
        [SerializeField]
        private VisualTreeAsset inspectorUxml;

        /// <summary>
        /// Ranged field types that don't have a native UIElements field and require a custom field
        /// </summary>
        private readonly Type[] rangedValueFieldTypes = new Type[]
        {
            typeof(float),
            typeof(int),
            typeof(long)
        };

        public override VisualElement CreateInspectorGUI()
        {
            // Create a new root visual element
            VisualElement rootVisualElement = new VisualElement();

            rootVisualElement.Clear();

            // Clone the visual tree asset
            inspectorUxml.CloneTree(rootVisualElement);

            // Invoke the virtual method for when visual tree asset has been cloned
            OnInspectorVisualTreeAssetCloned(rootVisualElement);

            // Create a new serialized object for the target
            SerializedObject obj = new SerializedObject(target);

            // Get the type of the target class
            Type targetType = target.GetType();

            // Get the type of the current derived class
            Type selfType = GetType();

            // Define the default visual element under which all fields will be parented
            VisualElement defaultFieldParent = rootVisualElement;

            // Check if the target class has default field element defined
            if (Attribute.IsDefined(targetType, typeof(FieldParentElementAttribute)))
            {
                // Try to find a visual element with the same name as the one provided on the attribute
                VisualElement foundElement = rootVisualElement.Q(targetType.GetCustomAttribute<FieldParentElementAttribute>().ParentName);

                // If the visual element was found, apply it as the default parent
                if(foundElement != null)
                {
                    defaultFieldParent = foundElement;
                }
            }
            // Check if the derived custom editor class has default field element defined
            else if (Attribute.IsDefined(selfType, typeof(FieldParentElementAttribute)))
            {
                // Try to find a visual element with the same name as the one provided on the attribute
                VisualElement foundElement = rootVisualElement.Q(selfType.GetCustomAttribute<FieldParentElementAttribute>().ParentName);

                // If the visual element was found, apply it as the default parent
                if (foundElement != null)
                {
                    defaultFieldParent = foundElement;
                }
            }

            // Get all parent types of the class
            IEnumerable<Type> baseTypes = TypeUtilities.GetParentsAndInterfaces(targetType);

            // Check if the derived class has the attribute for ignoring fields from specific types
            if (Attribute.IsDefined(selfType, typeof(IgnoreFieldsOfTypeAttribute)))
            {
                // If the attribute exists, exclude those types from the field lookup
                baseTypes = baseTypes.Except(selfType.GetCustomAttribute<IgnoreFieldsOfTypeAttribute>().IgnoredTypes);
            }

            // Get all runtime fields from all types
            IEnumerable<FieldInfo> fields = targetType.GetRuntimeFields().Union(baseTypes.SelectMany(t => t.GetRuntimeFields()));

            // Iterate through all runtime fields
            foreach (FieldInfo field in fields)
            {
                // If the field has been declared by the target class type and it's serialized, add it to the inspector
                if (!field.IsNotSerialized)
                {
                    // Define the default field parent
                    VisualElement fieldParent = defaultFieldParent;

                    // Check if the field has parent override attribute
                    if (Attribute.IsDefined(field, typeof(FieldParentElementAttribute)))
                    {
                        // Try to find a visual element with the same name as the one provided on the attribute
                        VisualElement foundElement = rootVisualElement.Q(field.GetCustomAttribute<FieldParentElementAttribute>().ParentName);

                        // If the visual element was found, apply it as the default parent
                        if (foundElement != null)
                        {
                            fieldParent = foundElement;
                        }
                    }

                    // Define an optional custom label for the field
                    string customLabel = null;

                    // Check if the field has an override attribute for the label
                    bool hasCustomLabelAttribute = Attribute.IsDefined(field, typeof(FieldLabelAttribute));

                    if(hasCustomLabelAttribute)
                    {
                        // If the label override attribute exists, get it
                        FieldLabelAttribute labelAttribute = field.GetCustomAttribute<FieldLabelAttribute>();

                        // If the label text is not null or empty, override the label with it
                        if (!string.IsNullOrEmpty(labelAttribute.FieldName))
                        {
                            customLabel = labelAttribute.FieldName;
                        }
                    }

                    // Check if the field has a range attribute
                    bool hasRangeAttribute = Attribute.IsDefined(field, typeof(RangeAttribute));

                    string tooltip = null;

                    // Check if the field has a tooltip attribute
                    bool hasTooltipAttribute = Attribute.IsDefined(field, typeof(TooltipAttribute));

                    // If the tooltip attribute exists, get it and apply the text
                    if (hasTooltipAttribute)
                    {
                        tooltip = field.GetCustomAttribute<TooltipAttribute>().tooltip;
                    }

                    // Get the serialized property of the field
                    SerializedProperty property = obj.FindProperty(field.Name);

                    // Declare a placeholder element
                    VisualElement newField;

                    // Get the type of the field
                    Type fieldType = field.FieldType;

                    // If the field has a range attribute and the type has an override field, use it instead of the default property field
                    if (rangedValueFieldTypes.Contains(fieldType) && hasRangeAttribute)
                    {
                        newField = RangedValueFieldBuilder.Build(target, property, field.GetCustomAttribute<RangeAttribute>(), customLabel, tooltip);
                    }
                    else
                    {
                        newField = hasCustomLabelAttribute ? new PropertyField(property, customLabel) : new PropertyField(property);

                        if (hasTooltipAttribute)
                        {
                            newField.tooltip = tooltip;
                        }
                    }

                    // Add the new field to the field's parent
                    fieldParent.Add(newField);
                }
            }

            // Invoke the virtual method for when the fields have been generated
            OnInspectorFieldsGenerated(rootVisualElement);

            return rootVisualElement;
        }

        protected virtual void OnInspectorVisualTreeAssetCloned(VisualElement root) { }

        protected virtual void OnInspectorFieldsGenerated(VisualElement root) { }
    }
}
