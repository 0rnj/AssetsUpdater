#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace CodeBase
{
    public class SafeAssetsEditingMode : IDisposable
    {
        public SafeAssetsEditingMode()
        {
            AssetDatabase.StartAssetEditing();
        }

        public void Execute(Action action)
        {
            if (action == null)
            {
                return;
            }

            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        public void Dispose()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif