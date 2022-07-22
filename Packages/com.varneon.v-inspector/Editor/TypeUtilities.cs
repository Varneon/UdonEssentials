using System;
using System.Collections.Generic;

namespace Varneon.VInspector
{
    public static class TypeUtilities
    {
        public static IEnumerable<Type> GetParentsAndInterfaces(Type type)
        {
            if (type == null)
            {
                throw new NullReferenceException("Type is null!");
            }

            foreach (Type i in type.GetInterfaces())
            {
                yield return i;
            }

            Type baseType = type.BaseType;

            while (baseType != null)
            {
                yield return baseType;

                baseType = baseType.BaseType;
            }
        }
    }
}
