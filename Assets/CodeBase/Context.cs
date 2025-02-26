#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace CodeBase
{
    public sealed class Context<T>
    {
        public T Value;
        public string FilePath;
        public Object Asset;
        public Component Component;
        public FieldInfo FieldInfo;

        public override string ToString()
        {
            return $"Path: {FilePath}:\n" +
                   $"Location in hierarchy: {(Component != null ? LogHelper.GetHierarchy(Component.transform) : "N/A")}\n" +
                   $"Component type: {(Component != null ? Component.GetType().Name : "N/A")}\n" +
                   $"Field: {FieldInfo.Name}";
        }
    }
}
#endif