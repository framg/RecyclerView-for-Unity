using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExampleAdapter : RecyclerView.Adapter<ExampleAdapter.ViewHolder>
{

    public int i = 50;

    public List<GameObject> list = new List<GameObject>();
    void Start(){
     
        
       // Camera.main.
        //         Vector3[] objectCorners = new Vector3[4];
        // list[0].GetComponent<RectTransform>().GetWorldCorners(objectCorners);
        // Debug.Log(objectCorners);
        // list[9].GetComponent<RectTransform>().GetWorldCorners(objectCorners);
        // Debug.Log(objectCorners);
    }
    public override int GetItemCount()
    {
        return 50;
    }

    public override void OnBindViewHolder(ViewHolder holder, int i)
    {
        holder.text.text = i.ToString();
    }

    public override GameObject OnCreateViewHolder(Transform parent)
    {
        GameObject row = new GameObject();
        row.AddComponent<CanvasRenderer>();
        GameObject text = new GameObject();
        text.AddComponent<Text>();
        text.GetComponent<Text>().color = Color.black;
        text.GetComponent<Text>().font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        text.transform.SetParent(row.transform);
        row.AddComponent<Image>();
        list.Add(row);
        return row;
    }


    // public void Update(){
    //     //yield return new WaitForSeconds(1);
    //     int i=0;
    //        //Debug.ClearDeveloperConsole();
    //    //  Debug.ClearDeveloperConsole();
    //     foreach(GameObject row in list){
    //         row.transform.GetChild(0).GetComponent<Text>().text = i.ToString() + " " +row.GetComponent<RectTransform>().IsVisibleFrom(Camera.main);
    //          Vector3[] objectCorners = new Vector3[4];
    //           Rect screenBounds = new Rect(0f, 0f, Camera.main.pixelWidth , Camera.main.pixelHeight  ); 

    //         row.GetComponent<RectTransform>().GetWorldCorners(objectCorners);
    //         string s  = "";
    //         bool c = false;
    //         foreach(Vector3 cor in objectCorners ){
    //             s += cor.ToString();
    //             if(!c){
    //                 c =screenBounds.Contains( cor);
    //               //  c = GetComponent<RectTransform>().rect.Contains( Camera.main.WorldToViewportPoint(cor));
    //             }
    //         }
    //    //     s+= "====" + c;
    //     //    Debug.Log(i + "    ------------------------------------");
    //   //      Debug.Log(s);
    //       //  Debug.Log("------------------------------------");
    //        // row.transform.GetChild(0).GetComponent<Text>().text = i.ToString() + " " +row.GetComponent<RectTransform>().IsFullyVisibleFrom(Camera.main);
    //     //   Rect rect = list[0].GetComponent<RectTransform>().rect;
    //     //   Rect rect2 = list[10].GetComponent<RectTransform>().rect;
    //    //     row.transform.GetChild(0).GetComponent<Text>().text = i.ToString() + " " +row.GetComponent<RectTransform>().rect.Overlaps(GetComponent<RectTransform>().rect);
    //     //    Rect.
    //       //  row.transform.GetChild(0).GetComponent<Text>().text = i.ToString()+ " " + is_rectTransformsOverlap(Camera.main, row.GetComponent<RectTransform>(), GetComponent<RectTransform>());
    //       //  row.transform.GetChild(0).GetComponent<Text>().text = i.ToString()+ " " + rectOverlaps(row.GetComponent<RectTransform>(), GetComponent<RectTransform>());
    //         i++;
    //     //   Debug.Log(row.transform.GetChild(0).GetComponent<Text>().text + " " +row.GetComponent<RectTransform>().IsVisibleFrom(Camera.main)) ;
    //     }
       
    // }

    public Vector3 GetGridPosition(GameObject obj){
        GameObject objAux = new GameObject();
        objAux.transform.position = obj.transform.position;
        return objAux.transform.position;
    }

    public class ViewHolder : RecyclerView.ViewHolder
    {
        public Text text;

        public ViewHolder(GameObject itemView) : base(itemView)
        {
            text = itemView.transform.GetChild(0).GetComponent<Text>();
        }
    }


    void OnGUI()
    {


        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        {
            ScrollTo(i);
           // ScrollTo(new Vector2(0, 0.5f));
        }
            
    }

    bool isRectInsideAnotherRect(){
        return false;
    }

 

}
