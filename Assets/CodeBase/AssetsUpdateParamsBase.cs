#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CodeBase
{
    public class AssetsUpdateParamsBase
    {
        protected const int DefaultMaxAllowedNesting = 10;

        protected static readonly string[] DefaultAllowedFileExtensions =
        {
            AssetsUpdater.PrefabExtension,
            AssetsUpdater.AssetExtension,
            AssetsUpdater.SceneExtension,
            AssetsUpdater.PlayableExtension
        };

        protected static readonly string[] DefaultIgnoredFilepathSubstrings =
        {
            "Lighting",
            "Fonts",
            "AddressableAssetsData"
        };

        protected static readonly Type[] DefaultIgnoredFieldTypes =
        {
            typeof(IntPtr),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
        };

        public readonly Type[] IgnoredBaseClassFieldTypes = 
        {
            typeof(object),
            typeof(Object),
            typeof(GameObject),
            typeof(Component),
            typeof(MonoBehaviour),
            typeof(ScriptableObject),
        };

        public readonly Type ObjectType = typeof(Object);
    }
}
#endif