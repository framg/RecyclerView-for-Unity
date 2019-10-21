using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RecyclerView
{
    [ExecuteInEditMode]
    public class CompileScript : MonoBehaviour
    {
        public string ScriptName;

        void Awake()
        {
           // StartCoroutine(addComponentAfterCompiling());
        }
        
        void Update()
        {
            if (!EditorApplication.isCompiling)
            {
                gameObject.AddComponent(System.Type.GetType(ScriptName));
                DestroyImmediate(this);
            }
            

        }



    }
}
