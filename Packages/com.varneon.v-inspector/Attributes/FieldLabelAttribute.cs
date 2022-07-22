using System;

namespace Varneon.VInspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FieldLabelAttribute : Attribute
    {
        public string FieldName;

        public FieldLabelAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
