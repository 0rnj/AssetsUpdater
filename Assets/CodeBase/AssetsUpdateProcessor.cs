#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CodeBase
{
    public class AssetsUpdateProcessor<T>
    {
        private readonly List<string> _sceneFiles = new();
        private readonly AssetsUpdateParams<T> _params;

        private string _currentFile;
        private Object _currentAsset;
        private Component _currentComponent;
        private FieldInfo _currentFieldInfo;
        private bool _setDirty;

        public AssetsUpdateProcessor(AssetsUpdateParams<T> @params)
        {
            _params = @params;
        }

        public void UpdateComponentsInAllPrefabs()
        {
            using var mode = new SafeAssetsEditingMode();
            mode.Execute(ProcessComponentsInAllPrefabs);
        }

        public void UpdateNotNestedComponentsInAllRegularPrefabs(bool setDirty = true)
        {
            _setDirty = setDirty;

            using (var mode = new SafeAssetsEditingMode())
            {
                mode.Execute(ProcessNotNestedComponentsInAllRegularPrefabs);
            }

            if (_sceneFiles.Count == 0)
            {
                return;
            }

            var startScenePath = SceneManager.GetActiveScene().path;

            using (var mode = new SafeAssetsEditingMode())
            {
                mode.Execute(ProcessScenes);
            }

            if (startScenePath != SceneManager.GetActiveScene().path)
            {
                EditorSceneManager.OpenScene(startScenePath);
            }
        }

        private void ProcessComponentsInAllPrefabs()
        {
            foreach (var file in _params.FilePaths)
            {
                if (ShouldIgnore(file))
                {
                    continue;
                }

                _currentFile = file;

                var mainAsset = AssetDatabase.LoadMainAssetAtPath(file);
                if (mainAsset is ScriptableObject scriptableObject)
                {
                    var applied = ApplyToFields(scriptableObject, _params.MaxAllowedNesting);
                    if (applied) TrySetDirty(scriptableObject);
                    continue;
                }

                if (file.EndsWith(AssetsUpdater.AssetExtension))
                {
                    continue;
                }

                var prefab = mainAsset as GameObject;
                if (prefab == null) continue;

                var components = prefab.GetComponentsInChildren<T>(true);
                if (components == null || components.Length == 0) continue;

                for (var i = 0; i < components.Length; i++)
                {
                    var component = components[i];
                    if (component is Component unityComponent)
                    {
                        _currentComponent = unityComponent;
                    }

                    PerformAction(component);
                }
            }
        }

        private void ProcessNotNestedComponentsInAllRegularPrefabs()
        {
            foreach (var file in _params.FilePaths)
            {
                if (ShouldIgnore(file))
                {
                    continue;
                }

                _currentComponent = null;
                _currentFieldInfo = null;
                _currentFile = file;

                var mainAsset = AssetDatabase.LoadMainAssetAtPath(file);

                _currentAsset = mainAsset;

                if (mainAsset is SceneAsset)
                {
                    _sceneFiles.Add(file);
                    continue;
                }

                if (mainAsset is ScriptableObject scriptableObject)
                {
                    ProcessScriptableObject(scriptableObject);
                    continue;
                }

                if (file.EndsWith(AssetsUpdater.AssetExtension))
                {
                    continue;
                }

                var prefab = mainAsset as GameObject;
                if (prefab == null)
                {
                    Debug.LogError($"Prefab by following path is not loaded: {file}");
                    continue;
                }

                var prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);
                if (prefabAssetType != PrefabAssetType.Regular && prefabAssetType != PrefabAssetType.Variant)
                {
                    continue;
                }

                var success = ApplyToParentWithChildren(prefab.transform);
                if (success)
                {
                    TrySetDirty(prefab);
                }
            }
        }

        private void ProcessScriptableObject(ScriptableObject scriptableObject)
        {
            var applied = ApplyToFields(scriptableObject, _params.MaxAllowedNesting);
            if (applied)
            {
                TrySetDirty(scriptableObject);
            }
        }

        private void ProcessScenes()
        {
            foreach (var sceneFile in _sceneFiles)
            {
                _currentFile = sceneFile;

                ProcessScene(sceneFile);
            }
        }

        private void ProcessScene(string file)
        {
            var scene = EditorSceneManager.OpenScene(file);
            var objects = scene.GetRootGameObjects();
            var sceneChanged = false;

            for (var i = 0; i < objects.Length; i++)
            {
                var gameObject = objects[i];
                var applied = ApplyToParentWithChildren(gameObject.transform);

                if (applied)
                {
                    sceneChanged = true;
                }
            }

            if (sceneChanged)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private bool ApplyToParentWithChildren(Transform transform)
        {
            var applied = ApplyWithCheck(transform);

            if (ApplyToChildren(transform))
            {
                applied = true;
            }

            return applied;
        }

        private bool ApplyToChildren(Transform transform)
        {
            var applied = false;

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);

                if (ApplyWithCheck(child))
                {
                    applied = true;
                }

                if (ApplyToChildren(child))
                {
                    applied = true;
                }
            }

            return applied;
        }

        private bool ApplyWithCheck(Transform transform)
        {
            var withPrefabVariantCheck = false;
            var transformGameObject = transform.gameObject;
            var isOutermostRoot = PrefabUtility.IsOutermostPrefabInstanceRoot(transformGameObject);

            if (isOutermostRoot)
            {
                withPrefabVariantCheck = true;

                var hasOverrides = PrefabUtility.HasPrefabInstanceAnyOverrides(transformGameObject, false);

                if (hasOverrides)
                {
                    var modifications = PrefabUtility.GetPropertyModifications(transformGameObject);

                    if (modifications is { Length: > 0 })
                    {
                        withPrefabVariantCheck = false;
                    }
                }
            }

            var applied = withPrefabVariantCheck
                ? ApplyWithPrefabVariantCheck(transform)
                : ApplyToAll(transform);

            return applied;
        }

        private bool ApplyWithPrefabVariantCheck(Transform transform)
        {
            var result = false;
            var components = transform.GetComponents<Component>();

            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];

                if (component == null)
                {
                    continue;
                }

                if (PrefabUtility.IsPartOfVariantPrefab(component) == false)
                {
                    continue;
                }

                _currentComponent = component;

                if (_currentComponent is T requiredComponent)
                {
                    var performed = PerformAction(requiredComponent);
                    if (performed)
                    {
                        result = true;
                    }
                }

                var applied = ApplyToFields(_currentComponent, _params.MaxAllowedNesting);
                if (applied)
                {
                    result = true;
                }
            }

            return result;
        }

        private bool ApplyToAll(Transform transform)
        {
            var result = false;
            var components = transform.GetComponents<Component>();

            for (var i = 0; i < components.Length; i++)
            {
                _currentComponent = components[i];

                if (_currentComponent is T requiredComponent)
                {
                    var performed = PerformAction(requiredComponent);
                    if (performed)
                    {
                        result = true;
                    }
                }

                var applied = ApplyToFields(_currentComponent, _params.MaxAllowedNesting);
                if (applied)
                {
                    result = true;
                }
            }

            return result;
        }

        private bool ApplyToFields(object obj, int allowedNesting)
        {
            if (obj == null)
            {
                return false;
            }

            var result = false;
            var requiredType = typeof(T);
            var fields = ReflectionUtils.GetFieldInfosIncludingBaseClasses(
                obj.GetType(),
                ignoredTypes: _params.IgnoredBaseClassFieldTypes);

            allowedNesting--;

            for (var i = 0; i < fields.Length; i++)
            {
                var fieldInfo = fields[i];
                var fieldType = fieldInfo.FieldType;

                _currentFieldInfo = fieldInfo;

                if (fieldType == requiredType)
                {
                    var value = (T)fieldInfo.GetValue(obj);

                    var performed = PerformAction(value);
                    if (performed)
                    {
                        result = true;
                    }

                    continue;
                }

                if (allowedNesting <= 0)
                {
                    continue;
                }

                if (ShouldIgnore(fieldType))
                {
                    continue;
                }

                if (typeof(IList<T>).IsAssignableFrom(fieldType))
                {
                    var list = (IList<T>)fieldInfo.GetValue(obj);
                    if (list == null)
                    {
                        continue;
                    }

                    for (var k = 0; k < list.Count; k++)
                    {
                        var element = list[k];
                        if (element is Component component)
                        {
                            _currentComponent = component;
                        }

                        var performed = PerformAction(element);
                        if (performed)
                        {
                            result = true;
                        }
                    }

                    continue;
                }

                if (typeof(IList).IsAssignableFrom(fieldType))
                {
                    var list = (IList)fieldInfo.GetValue(obj);

                    if (list == null || list.Count == 0)
                    {
                        continue;
                    }

                    if (ShouldIgnore(list))
                    {
                        continue;
                    }

                    for (var k = 0; k < list.Count; k++)
                    {
                        var value = list[k];
                        var localResult = ApplyToFields(value, allowedNesting);
                        if (localResult)
                        {
                            result = true;
                        }
                    }

                    continue;
                }

                var fieldValue = fieldInfo.GetValue(obj);
                var applied = ApplyToFields(fieldValue, allowedNesting);
                if (applied)
                {
                    result = true;
                }
            }

            _currentFieldInfo = null;

            return result;
        }

        private bool PerformAction(T obj)
        {
            bool processed;

            if (_params.DetailedAction != null)
            {
                var @params = new Context<T>
                {
                    Value = obj,
                    FilePath = _currentFile,
                    Asset = _currentAsset,
                    Component = _currentComponent,
                    FieldInfo = _currentFieldInfo
                };

                processed = _params.DetailedAction.Invoke(@params);
            }
            else
            {
                processed = _params.Action.Invoke(obj);
            }

            if (processed == false)
            {
                return false;
            }

            var fileName = _currentFile != null
                ? $"\n<color=green>File path:</color> {_currentFile}"
                : string.Empty;

            var hierarchy = _currentComponent != null
                ? $"\n<color=green>Hierarchy:</color> {LogHelper.GetHierarchy(_currentComponent.transform)}"
                : string.Empty;

            var field = _currentFieldInfo != null
                ? $"\n<color=green>Field name:</color> {_currentFieldInfo.Name}"
                : string.Empty;

            Debug.Log($"<color=green>Processed</color> {fileName}{obj}{hierarchy}{field}\n\n");

            if (_currentComponent != null)
            {
                TrySetDirty(_currentComponent);
            }

            return true;
        }

        private bool ShouldIgnore(string file)
        {
            var isAllowedFileType = false;

            for (var i = 0; i < _params.AllowedFileExtensions.Length; i++)
            {
                var extension = _params.AllowedFileExtensions[i];
                if (!file.EndsWith(extension))
                {
                    continue;
                }

                isAllowedFileType = true;
                break;
            }

            if (isAllowedFileType == false)
            {
                return true;
            }

            for (var i = 0; i < _params.IgnoredFilepathSubstrings.Length; i++)
            {
                var substring = _params.IgnoredFilepathSubstrings[i];
                if (file.Contains(substring))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldIgnore(Type fieldType)
        {
            // shouldn't always ignore, because they might be not mono behaviours
            if (fieldType == _params.ObjectType || fieldType.IsSubclassOf(_params.ObjectType))
            {
                return true;
            }

            return _params.IgnoredFieldTypes.Contains(fieldType);
        }

        private bool ShouldIgnore(IList list)
        {
            object value = null;

            for (var i = 0; i < list.Count; i++)
            {
                value = list[i];

                if (value != null)
                {
                    break;
                }
            }

            if (value == null)
            {
                return true;
            }

            var valueType = value.GetType();

            for (var i = 0; i < _params.IgnoredFieldTypes.Length; i++)
            {
                var ignoredType = _params.IgnoredFieldTypes[i];
                if (valueType == ignoredType)
                {
                    return true;
                }
            }

            return false;
        }

        private void TrySetDirty(Object obj)
        {
            if (_setDirty)
            {
                EditorUtility.SetDirty(obj);
            }
        }
    }
}
#endif