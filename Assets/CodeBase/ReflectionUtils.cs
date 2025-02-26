using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeBase
{
    public static class ReflectionUtils
    {
        private const BindingFlags InstanceFieldsBindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type[] DefaultIgnoredTypes = { typeof(object) };

        public static FieldInfo[] GetFieldInfosIncludingBaseClasses(
            Type type,
            BindingFlags bindingFlags = InstanceFieldsBindingFlags,
            Type[] ignoredTypes = null)
        {
            ignoredTypes ??= DefaultIgnoredTypes;

            if (ignoredTypes.Contains(type))
            {
                return Array.Empty<FieldInfo>();
            }

            var currentType = type;
            var fieldComparer = new FieldInfoComparer();
            var fieldInfosSet = new HashSet<FieldInfo>(fieldComparer);
            var needIgnore = false;

            while (needIgnore == false)
            {
                var fieldInfos = currentType.GetFields(bindingFlags);
                fieldInfosSet.UnionWith(fieldInfos);

                currentType = currentType.BaseType;
                needIgnore = ignoredTypes.Contains(currentType);
            }

            return fieldInfosSet.ToArray();
        }

        private class FieldInfoComparer : IEqualityComparer<FieldInfo>
        {
            public bool Equals(FieldInfo x, FieldInfo y)
            {
                return x.DeclaringType == y.DeclaringType && x.Name == y.Name;
            }

            public int GetHashCode(FieldInfo obj)
            {
                return obj.Name.GetHashCode() ^ obj.DeclaringType.GetHashCode();
            }
        }
    }
}