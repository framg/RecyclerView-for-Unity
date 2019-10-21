using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExampleAdapter : RecyclerView.Adapter<ExampleAdapter.ViewHolder>
{



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


    bool isRectInsideAnotherRect(){
        return false;
    }
bool rectOverlaps(RectTransform rectTrans1, RectTransform rectTrans2)
{
    Rect rect1 = new Rect(rectTrans1.localPosition.x, rectTrans1.localPosition.y, rectTrans1.rect.width, rectTrans1.rect.height);
    Rect rect2 = new Rect(rectTrans2.localPosition.x, rectTrans2.localPosition.y, rectTrans2.rect.width, rectTrans2.rect.height);

    return rect1.Overlaps(rect2);
}
  //if your viewport is screen, then keep it as 'null'
    // NOTICE - doesn't consider if the rectangles are rotating,
    //
    // but shoudl work even if canvas's camera ISN'T aligned with world axis :)
    public static bool is_rectTransformsOverlap( Camera cam,
                                                 RectTransform elem,
                                                 RectTransform viewport = null ){
        Vector2 viewportMinCorner;
        Vector2 viewportMaxCorner;
 
        if(viewport != null) {
            //so that we don't have to traverse the entire parent hierarchy (just to get screen coords relative to screen),
            //ask the camera to do it for us.
            //first get world corners of our rect:
            Vector3[] v_wcorners = new Vector3[4];
            viewport.GetWorldCorners(v_wcorners); //bot left, top left, top right, bot right
 
            //+ow shove it back into screen space. Now the rect is relative to the bottom left corner of screen:
            viewportMinCorner = cam.WorldToScreenPoint(v_wcorners[0]);
            viewportMaxCorner = cam.WorldToScreenPoint(v_wcorners[2]);
        }
        else {
            //just use the scren as the viewport
            viewportMinCorner = new Vector2( 0, 0 );
            viewportMaxCorner = new Vector2( Screen.width, Screen.height);
        }
 
        //give 1 pixel border to avoid numeric issues:
        viewportMinCorner += Vector2.one;
        viewportMaxCorner -= Vector2.one;
 
        //do a similar procedure, to get the "element's" corners relative to screen:
        Vector3[] e_wcorners = new Vector3[4];
        elem.GetWorldCorners(e_wcorners);
 
        Vector2 elem_minCorner = cam.WorldToScreenPoint(e_wcorners[0]);
        Vector2 elem_maxCorner = cam.WorldToScreenPoint(e_wcorners[2]);
 
        //perform comparison:
        if(elem_minCorner.x > viewportMaxCorner.x) { return false; }//completelly outside (to the right)
        if(elem_minCorner.y > viewportMaxCorner.y) { return false; }//completelly outside (is above)
 
        if(elem_maxCorner.x < viewportMinCorner.x) {  return false;  }//completelly outside (to the left)
        if(elem_maxCorner.y < viewportMinCorner.y) {  return false;  }//completelly outside (is below)
 
        /*
             commented out, but use it if need to check if element is completely inside:
            Vector2 minDif = viewportMinCorner - elem_minCorner;
            Vector2 maxDif = viewportMaxCorner - elem_maxCorner;
            if(minDif.x < 0  &&  minDif.y < 0  &&  maxDif.x > 0  &&maxDif.y > 0) { //return "is completely inside" }
        */
     
        return true;//passed all checks, is inside (at least partially)
    }
 

}
