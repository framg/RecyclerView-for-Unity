using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerView
{

    public class Menu : EditorWindow
    {
      // string inputName = "";


        string objNames = "";


        void OnGUI()
        {
            EditorGUI.DropShadowLabel(new Rect(0, 0, position.width, 20),
                "Choose a name for the adapter:");

            objNames = EditorGUI.TextField(new Rect(10, 25, position.width - 20, 20),
                "Name:",
                objNames);

           // if (Selection.activeTransform)
         //   {
                if (GUI.Button(new Rect(0, 50, position.width, 30), "Create"))
                {
                
                //foreach (Transform t in Selection.transforms)
                //{
                //    t.name = objNames;
                //  //  var window = GetWindow<Menu>();
                //    this.Close();
                //}
                
                Selection.activeTransform = Create(objNames);
                Close();
                }
          //  }
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        [MenuItem("GameObject/UI/RecyclerView", false, 0)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            var window = ScriptableObject.CreateInstance<Menu>();
            window.Show();
            //Menu window = ScriptableObject.CreateInstance<Menu>();
            //window.position = new Rect(Screen.width * 2, Screen.height / 2, 250, 150);
            //window.ShowPopup();

            //Create();
        }

        private static Transform Create(string name)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if(canvas == null)
            {
                GameObject canvasObj = new GameObject();
                canvasObj.AddComponent<RectTransform>();
                canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject script = new GameObject();
            script.name = name;
            script.AddComponent<RectTransform>();
            script.transform.SetParent(canvas.transform);
            CreateScript(script);
            return script.transform;
        }


        //void OnGUI()
        //{
        //    //EditorGUILayout.LabelField("Choose a name for the adapter:", EditorStyles.wordWrappedLabel);
        //    EditorGUI.LabelField(new Rect(10, 25, 240, 25), inputName);
        // //  EditorGUI.LabelField(new Rect(10, 25, 240, 25), "Choose a name for the adapter:");
        //    EditorGUI.TextField(new Rect(10, 50, 240, 25), inputName);
        //   // Debug.Log(inputName);
        //    GUILayout.Space(85);
        //    if (GUILayout.Button("Create"))
        //    {
        //       // if (inputName != null && !inputName.Equals(""))
        //        {
        //           // Create(inputName);
        //            this.Close();
        //        }
        //    }
        //}

        private static void CreateScript(GameObject obj)
        {
            string name = obj.name;
            string copyPath;
            MonoScript script;
            int i = 1;
            copyPath = "Assets/" + name + ".cs";
            script = (MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript));
            if (script != null)
            {
                do
                {
                    name = obj.name + i;
                    copyPath = "Assets/" + name + ".cs";
                    script = (MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript));
                    i++;
                } while (script != null);
            }
            
            if (File.Exists(copyPath) == false)
            { // do not overwrite
                using (StreamWriter outfile =
                    new StreamWriter(copyPath))
                {
                    string file = "using UnityEngine;\n" +
                "using System.Collections;\n" +
                "\n" +
                "public class {{Name}} : RecyclerView.Adapter<{{Name}}.ViewHolder> {\n" +
                "\n" +
                "    public override int GetItemCount()\n" +
                "    {\n" +
                "        throw new System.NotImplementedException();\n" +
                "    }\n" +
                "\n" +
                "    public override void OnBindViewHolder(ViewHolder holder, int i)\n" +
                "    {\n" +
                "        throw new System.NotImplementedException();\n" +
                "    }\n" +
                "\n" +
                "    public override GameObject OnCreateViewHolder(Transform parent)\n" +
                "    {\n" +
                "        throw new System.NotImplementedException();\n" +
                "    }\n" +
                "\n" +
                "    public class ViewHolder : RecyclerView.ViewHolder\n" +
                "    {\n" +
                "        public ViewHolder(GameObject itemView) : base(itemView)\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

                    outfile.WriteLine(file.Replace("{{Name}}", name));

                    //outfile.WriteLine("using UnityEngine;");
                    //outfile.WriteLine("using System.Collections;");
                    //outfile.WriteLine("");
                    //outfile.WriteLine("public class " + name + " : MonoBehaviour {");
                    //outfile.WriteLine(" ");
                    //outfile.WriteLine(" ");
                    //outfile.WriteLine(" // Use this for initialization");
                    //outfile.WriteLine(" void Start () {");
                    //outfile.WriteLine(" ");
                    //outfile.WriteLine(" }");
                    //outfile.WriteLine(" ");
                    //outfile.WriteLine(" ");
                    //outfile.WriteLine(" // Update is called once per frame");
                    //outfile.WriteLine(" void Update () {");
                    //outfile.WriteLine(" ");
                    //outfile.WriteLine(" }");
                    //outfile.WriteLine("}");
                }//File written
            }
            AssetDatabase.Refresh();
            CompileScript compileScript = obj.AddComponent<CompileScript>();
            compileScript.ScriptName = name;
           // AssetDatabase.OpenAsset((MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript)));
        }

    }
}