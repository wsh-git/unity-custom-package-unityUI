using System;
using UnityEngine;

namespace Wsh.UI {
    public interface iUILoader {
        string GetUIRootPrefabPath();
        string GetName(int id);
        string GetPrefabPath(int id);
        Type GetClassType(int id);//return System.Reflection.Assembly.Load("Assembly-CSharp").GetType(className);
        bool IsLoadingUI(int id);
        float GetDebugMaskAlpha(int id);
        bool CanCloseByEsc(int id);
        void InstanceAsync(string prefabPath, GameObject parent, Action<GameObject> onComplete);
    }
}