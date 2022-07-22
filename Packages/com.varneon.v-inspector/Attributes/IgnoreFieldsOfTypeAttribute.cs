using System;
using System.Collections.Generic;

namespace Varneon.VInspector
{
    /// <summary>
    /// Attribute for defining type(s) from which all fields should be excluded from the inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IgnoreFieldsOfTypeAttribute : Attribute
    {
        /// <summary>
        /// Name of the VisualElement
        /// </summary>
        public IEnumerable<Type> IgnoredTypes;

        public IgnoreFieldsOfTypeAttribute(params Type[] ignoredTypes)
        {
            IgnoredTypes = ignoredTypes;
        }
    }
}
