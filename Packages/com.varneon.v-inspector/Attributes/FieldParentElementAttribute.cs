using System;

namespace Varneon.VInspector
{
    /// <summary>
    /// Attribute for defining the name of the VisualElement that a field will be parented to
    /// </summary>
    /// <remarks>
    /// If the attribute is defined on the class, it will set the default parent for all fields (can be overriden individually by defining on a field)
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class FieldParentElementAttribute : Attribute
    {
        /// <summary>
        /// Name of the VisualElement
        /// </summary>
        public string ParentName;

        public FieldParentElementAttribute(string parentName)
        {
            ParentName = parentName;
        }
    }
}
