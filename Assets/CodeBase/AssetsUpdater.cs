#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CodeBase
{
    public static class AssetsUpdater
    {
        public const string PrefabExtension = ".prefab";
        public const string AssetExtension = ".asset";
        public const string SceneExtension = ".unity";
        public const string PlayableExtension = ".playable";

        private static readonly FilesMap FilesMap = new();

        public static void UpdateComponentsInAllPrefabs<T>(Func<Context<T>, bool> action, Params @params = null)
        {
            var assetsUpdateParams = new AssetsUpdateParams<T>(GetFilePaths(@params), action);
            var processor = new AssetsUpdateProcessor<T>(assetsUpdateParams);

            processor.UpdateComponentsInAllPrefabs();
        }

        public static void UpdateComponentsInAllPrefabs<T>(Func<T, bool> action, Params @params = null)
        {
            var assetsUpdateParams = new AssetsUpdateParams<T>(GetFilePaths(@params), action);
            var processor = new AssetsUpdateProcessor<T>(assetsUpdateParams);

            processor.UpdateComponentsInAllPrefabs();
        }

        public static void UpdateNotNestedComponentsInAllRegularPrefabs<T>(Func<T, bool> action, bool setDirty = true,
            Params @params = null)
        {
            var assetsUpdateParams = new AssetsUpdateParams<T>(GetFilePaths(@params), action);
            var processor = new AssetsUpdateProcessor<T>(assetsUpdateParams);

            processor.UpdateNotNestedComponentsInAllRegularPrefabs(setDirty);
        }

        public static void UpdateNotNestedComponentsInAllRegularPrefabs<T>(Func<Context<T>, bool> action,
            bool setDirty = true, Params @params = null)
        {
            var assetsUpdateParams = new AssetsUpdateParams<T>(GetFilePaths(@params), action);
            var processor = new AssetsUpdateProcessor<T>(assetsUpdateParams);

            processor.UpdateNotNestedComponentsInAllRegularPrefabs(setDirty);
        }

        public static GameObject CreatePrefabVariant(
            string from,
            string to,
            bool saveAssets = false,
            Action<GameObject> actionOnInstance = null)
        {
            var prefab = AssetDatabase.LoadMainAssetAtPath(from) as GameObject;
            return CreatePrefabVariant(prefab, to, saveAssets, actionOnInstance);
        }

        public static GameObject CreatePrefabVariant(
            GameObject gameObject,
            string to,
            bool saveAssets = false,
            Action<GameObject> actionOnInstance = null)
        {
            if (TryCreatePrefabInstance(gameObject, out var prefabInstance, out _) == false)
            {
                return null;
            }

            try
            {
                actionOnInstance?.Invoke(prefabInstance);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return SaveInstanceAsPrefabVariant(prefabInstance, to, saveAssets);
        }

        public static bool TryCreatePrefabInstance(
            GameObject gameObject,
            out GameObject prefabInstance,
            out string assetPath)
        {
            prefabInstance = null;
            assetPath = null;

            if (gameObject == null)
            {
                return false;
            }

            prefabInstance = PrefabUtility.InstantiatePrefab(gameObject) as GameObject;

            if (prefabInstance != null)
            {
                assetPath = AssetDatabase.GetAssetPath(gameObject);
                return true;
            }

            var origin = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            prefabInstance = PrefabUtility.InstantiatePrefab(origin) as GameObject;

            var isSuccess = prefabInstance != null;
            assetPath = isSuccess ? AssetDatabase.GetAssetPath(origin) : default;

            return isSuccess;
        }

        public static GameObject SaveInstanceAsPrefabVariant(GameObject gameObject, string to, bool saveAssets)
        {
            var prefabVariant = PrefabUtility.SaveAsPrefabAsset(gameObject, to);

            if (saveAssets == false)
            {
                return prefabVariant;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return prefabVariant;
        }

        private static IReadOnlyList<string> GetFilePaths(Params @params)
        {
            IReadOnlyList<string> filePaths;
            if (@params?.FilePaths != null)
            {
                filePaths = @params.FilePaths;
            }
            else
            {
                FilesMap.PrepareFiles(@params?.Directory);
                filePaths = FilesMap.AllFiles;
            }

            return filePaths;
        }

        public class Params
        {
            public IReadOnlyList<string> FilePaths;
            public string Directory;
        }
    }
}
#endif