
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateableData : ScriptableObject
{
    public event System.Action on_value_updated;
    public bool auto_update;
#if UNITY_EDITOR
    protected virtual void OnValidate() { // it is called when value is changed in the unity inspector
        if(auto_update) {
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
        }
    }
#endif

    public void NotifyOfUpdatedValues() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
#endif
        on_value_updated?.Invoke();
    }

}
