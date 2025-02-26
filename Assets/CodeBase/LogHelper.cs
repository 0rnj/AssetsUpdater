using UnityEngine;

namespace CodeBase
{
    public static class LogHelper
    {
        public static string GetHierarchy(Transform transform)
        {
            var result = transform.name;
            var parent = transform.parent;

            while (parent != null)
            {
                result = $"{parent.name} ► {result}";
                parent = parent.parent;
            }

            return result;
        }
    }
}