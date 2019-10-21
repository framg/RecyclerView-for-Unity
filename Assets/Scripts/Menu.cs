using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerView
{

    public class Menu : MonoBehaviour
    {

        [MenuItem("GameObject/UI/RecyclerView", false, 0)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            Create();
        }

        private static void Create()
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
            else
            {
                GameObject script = new GameObject();
                script.name = "RecyclerView";
                script.AddComponent<RectTransform>();
                script.transform.SetParent(canvas.transform);
                CreateScript(script);
            }
        }

        private static void CreateScript(GameObject obj)
        {
            string name ;
            string copyPath;
            MonoScript script;
            int i = 0;
            do {           
                name = "RecyclerViewAdapter" + i;
                copyPath = "Assets/" + name + ".cs";
                script = (MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript));
                i++;
            } while (script != null);
           
            
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
            AssetDatabase.OpenAsset((MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript)));
        }

    }
}