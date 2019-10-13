using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclerView{
    public abstract class Adapter<T> : MonoBehaviour
        where T : ViewHolder
    {
            
        private GameObject poolObj; 
        public ScrollRect scroll;
        public void Awake(){
            poolObj = new GameObject();
            poolObj.transform.SetParent(transform.parent);
            poolObj.name = "Pool";

            // if(T is ViewHolder){

            // }
        }

        public int end_position;
        public int start_position;
        private int POOL_SIZE = 10;
        private int CACHE_SIZE = 3;
        public float LIMIT_BOTTOM = 0;

        private int ATTACHED_SCRAP_SIZE = 12 ;

   //     public float cell_screen_size;
        public Pool pool;
        public List<ViewHolder> attachedScrap = new List<ViewHolder>();
        public List<ViewHolder> cacheTop = new List<ViewHolder>();
        public List<ViewHolder> cacheBot = new List<ViewHolder>();

        private float last_y_scroll_position;


        public abstract GameObject OnCreateViewHolder(Transform parent);
        public abstract void OnBindViewHolder(T holder, int i);
        
        public abstract int GetItemCount();


        private ViewHolder GetViewHolderFromScrap(int position){
            foreach(ViewHolder vh in attachedScrap){
                if(vh.current_index == position){
                    return vh;
                }
            }
            return null;
        }


        public void NotifyDatasetChanged(){
            Clear();
            pool = new Pool(POOL_SIZE, CACHE_SIZE);
             
            if(GetItemCount() > 0){
          //      ViewHolder vh = GetViewHolder(0);
          //        attachedScrap.Add(vh);
           //     OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), 0);
            //     float row_height = vh.itemView.GetComponent<RectTransform>().rect.height;
             //   cell_screen_size = Mathf.Ceil(1/(row_height / transform.parent.GetComponent<RectTransform>().rect.height));
                // for(int i = 1; i<cell_screen_size + 1 ; i++){
                //         vh = GetViewHolder(i);
                //         attachedScrap.Add(vh);
                //          OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), i);
                // }
                for(int i = 0; i<ATTACHED_SCRAP_SIZE ; i++){
                        ViewHolder vh = (ViewHolder) Activator.CreateInstance(typeof(T),new object[] { OnCreateViewHolder(transform) } );
                        

                        vh.current_index = i;
                        vh.last_index = i;
                        vh.status = Status.SCRAP;


                       AddToAttachedScrap(vh, true);

                        OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), i);
                }
                ReorderList();
                LIMIT_BOTTOM = (GetItemCount() * attachedScrap[0].itemView.GetComponent<RectTransform>().rect.height) - transform.parent.GetComponent<RectTransform>().rect.height;
            }

            last_y_scroll_position = 1;
            // current_start_position = 0; 
            // current_end_position = Mathf.CeilToInt(cell_screen_size) + 1;
        }     

        private void AddToAttachedScrap(ViewHolder vh, bool attachTop){
            vh.itemView.transform.SetParent(transform);
            if(attachTop){
                vh.itemView.transform.SetAsLastSibling();
            }else{
                vh.itemView.transform.SetAsFirstSibling();
            }
            vh.itemView.name = vh.current_index.ToString();
            vh.itemView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            vh.itemView.SetActive(true);
            attachedScrap.Add(vh);
        }


        private void ReorderList()
        {
            List<ViewHolder> vhs = new List<ViewHolder>();
            vhs.AddRange(cacheBot);
            vhs.AddRange(cacheTop);
            vhs.AddRange(attachedScrap);
            foreach(ViewHolder vh in vhs)
            {
                vh.itemView.GetComponent<RectTransform>().localPosition = new Vector3(0, -vh.current_index * 100, 0);
            }
            
        }

        private ViewHolder GetFromCache(int i, bool top){
            if (top)
            {
                foreach (ViewHolder vh in cacheTop)
                {
                    if (vh.current_index == i)
                    {
                        cacheTop.Remove(vh);
                        return vh;
                    }
                }
            }
            else
            {
                foreach (ViewHolder vh in cacheBot)
                {
                    if (vh.current_index == i)
                    {
                        cacheBot.Remove(vh);
                        return vh;
                    }
                }
            }
            return null;
        }

        private ViewHolder TryGetViewHolderForPosition(int position){
            if(position >= 0 && position < GetItemCount()){
                    ViewHolder botCache = GetFromCache(position, false);
                    if(botCache != null){
                        botCache.status = Status.CACHE;
                        botCache.current_index = position;
                        botCache.itemView.name = position.ToString();
   
                        return botCache;
                    }
                    ViewHolder topCache = GetFromCache(position, true);
                    if(topCache != null){
                        topCache.status = Status.CACHE;
                        topCache.current_index = position;
                        topCache.itemView.name = position.ToString();
   
                        return topCache;
                    }
                ViewHolder vhrecycled;
                    vhrecycled = pool.GetFromPool(position);
                    if(vhrecycled != null){
                        vhrecycled.status = Status.RECYCLED;
                        vhrecycled.last_index = vhrecycled.current_index;
                        vhrecycled.current_index = position;
                        return vhrecycled;
                    }

                    if(pool.IsFull()){
                        vhrecycled = pool.GetFromPool(position, true);
                        vhrecycled.status = Status.RECYCLED;
                        vhrecycled.last_index = vhrecycled.current_index;
                        vhrecycled.current_index = position;
                        return vhrecycled;
                        
                    }else{
                        ViewHolder vh = (ViewHolder) Activator.CreateInstance(typeof(T),new object[] { OnCreateViewHolder(transform) } );
                    
                        vh.current_index = position;
                        vh.last_index = position;
                        vh.status = Status.SCRAP;
                        
                        return vh;
                    }
                
            }else{
                return null;
            }
        }
        private void OnScrollDown(){
          //  for(int i=0; i<ATTACHED_SCRAP_SIZE - attachedScrap.Count; i++){
                // int new_position = GetUpperPosition() + 1;
                // if(new_position < GetItemCount()){
                //     ViewHolder vh = TryGetViewHolderForPosition(new_position);
                //     Debug.Log("ADDED " + vh.itemView.name);
                //     OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), new_position);
                // //    attachedScrap.Add(vh);
                // }
          //  }

            // if(attachedScrap.Count < ATTACHED_SCRAP_SIZE){

            // }
            //Debug.Log("DOWN");
        }

        private void OnScrollUp(){
            // //for(int i=0; i<ATTACHED_SCRAP_SIZE - attachedScrap.Count; i++){
            //     int new_position = GetLowerPosition() - 1;
            //     if(new_position > 0){
            //         ViewHolder vh = TryGetViewHolderForPosition(new_position);
            //         vh.itemView.transform.SetSiblingIndex(GetUpperChild() + 1);
            //         OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), new_position);
            //     //    attachedScrap.Add(vh);
            //     }
       //     }
           // Debug.Log("UP");
        }
        // private void OnScrollDown(int new_position){
        //     pool.Add(attachedScrap[0]);
        //     attachedScrap.RemoveAt(0);
        //     ViewHolder vh = GetViewHolder(new_position);
        //     vh.itemView.transform.SetSiblingIndex(GetLowerChild() + 1);
        //      OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), new_position);
        //     attachedScrap.Add(vh);
        // }

        // private void OnScrollUp(int new_position){
        //     pool.Add(attachedScrap[attachedScrap.Count - 1]);
        //     attachedScrap.RemoveAt(attachedScrap.Count - 1);
        //     ViewHolder vh = GetViewHolder(new_position);
        //     vh.itemView.transform.SetSiblingIndex(GetUpperChild() - 1);
        //      OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), new_position);
        //     attachedScrap.Insert(0, vh);
        // }

        private int GetLowerPosition(){
            int lower = int.MaxValue;
            foreach(ViewHolder scrap in attachedScrap){
                if(scrap.current_index< lower){
                    lower = scrap.current_index;
                }
            }
            return lower;
        }

        private int GetUpperPosition(){
            int upper = 0;
            foreach(ViewHolder scrap in attachedScrap){
                if(scrap.current_index > upper){
                    upper = scrap.current_index;
                }
            }
            return upper;
        }

        private int GetLowerChild(){
            int lower = int.MaxValue;
            foreach(ViewHolder scrap in attachedScrap){
                if(scrap.itemView.transform.GetSiblingIndex() < lower){
                    lower = scrap.itemView.transform.GetSiblingIndex();
                }
            }
            return lower;
        }

        
        private int GetUpperChild(){
            int upper = 0;
            foreach(ViewHolder scrap in attachedScrap){
                if(scrap.itemView.transform.GetSiblingIndex() > upper){
                    upper = scrap.itemView.transform.GetSiblingIndex();
                }
            }
            return upper;
        }

        // private int GetCurrentEndPosition(){
        //     return GetCurrentStartPosition() + Mathf.CeilToInt(cell_screen_size);
        // }

        // private int GetCurrentStartPosition(){

        //    float scroll_pos_inv = (1 - scroll.verticalNormalizedPosition)  ; 
        //    // float items_size = GetItemCount() - cell_screen_size;
        //     float items_size = Mathf.CeilToInt(cell_screen_size) + 1;
        //     return Mathf.RoundToInt(scroll_pos_inv * items_size);
        // //    float scroll_pos_inv = (1 - scroll.verticalNormalizedPosition)  ; 
        // //    // float items_size = GetItemCount() - cell_screen_size;
        // //     float items_size = Mathf.CeilToInt(cell_screen_size) + 1;
        // //     return Mathf.RoundToInt(scroll_pos_inv * items_size);
        // }

        //public override void OnBeginDrag(PointerEventData eventData)
        //{

        //}
        //public override void OnDrag(PointerEventData eventData)
        //{

        //}
        //public override void OnEndDrag(PointerEventData eventData)
        //{

        //}
        //public override void OnInitializePotentialDrag(PointerEventData eventData)
        //{

        //}
        //public override void OnScroll(PointerEventData data)
        //{

        //}

        public void OnScroll(Vector2 pos){
            

            if(pos.y > 1)
            {
               // transform.parent.GetComponent<ScrollRect>().
            }

            if(GetComponent<RectTransform>().offsetMax.y < 0)
            {
                GetComponent<RectTransform>().offsetMax = new Vector2(GetComponent<RectTransform>().offsetMax.x, 0);
                GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, 0);
            }
            else if(GetComponent<RectTransform>().offsetMax.y > LIMIT_BOTTOM)
            {
                GetComponent<RectTransform>().offsetMax = new Vector2(GetComponent<RectTransform>().offsetMax.x, LIMIT_BOTTOM);
                GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, 0);
            }

            //if (GetComponent<RectTransform>().offsetMax.y < 0)
            //{
            //    GetComponent<RectTransform>().offsetMax = new Vector2(GetComponent<RectTransform>().offsetMax.x, 0);
            //}

          
      //      Vector2 velocity = transform.parent.GetComponent<ScrollRect>().velocity;
       //     transform.parent.GetComponent<ScrollRect>().velocity = new Vector2(0, Mathf.Clamp(velocity.y, -1000, 1000));
          //  Debug.Log(transform.parent.GetComponent<ScrollRect>().velocity);
            // Debug.ClearDeveloperConsole();
            // string str = "";
            // foreach(ViewHolder vh in attachedScrap){
            //    str += vh.current_index + " " + vh.IsHidden() + "_____";
            // }
            // Debug.Log(str);

            //  Sort(cacheBot, false);
            //    Sort(cacheTop, true);

            if (last_y_scroll_position < scroll.verticalNormalizedPosition){
              //  OnScrollUp();
            //   GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            }else if(last_y_scroll_position > scroll.verticalNormalizedPosition){
               // OnScrollDown();
           //    GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            }
            last_y_scroll_position = scroll.verticalNormalizedPosition;

          //  RemoveNotHiddenViesFromCache(true);
            CleanCache();
            RemoveNotVisibleViewHolders();
            RemoveViewHoldersFromCache(true);
            RemoveViewHoldersFromCache(false);
            AddNewViewHoldersToCache(true);
            AddNewViewHoldersToCache(false);
            ReorderList();







          //  Debug.Log(ToString());

        //     int start_new_position = GetCurrentSWtartPosition();
        //     int end_new_position = GetCurrentEndPosition();

        //     if(start_new_position < current_start_position){
        //      //   OnScrollUp(start_new_position);
        //     }else if(end_new_position > current_end_position){
        //    //     OnScrollDown(current_end_position);
        //     }

        //     current_end_position = end_new_position;
        //     current_start_position = start_new_position;
        }


        public override string ToString(){
            string str  = "";
            str+= "Attached: {";
            foreach(ViewHolder vh in attachedScrap){
                str+= vh.current_index + ",";
            }
            str += "} Cache Top: {";
            foreach(ViewHolder vh in cacheTop){
                str+= vh.current_index + ",";
            }
            str += "} Cache Bot: {";
            foreach(ViewHolder vh in cacheBot){
                str+= vh.current_index + ",";
            }
            str += "} Pool: {";
            foreach(ViewHolder vh in pool.Scrap){
                str+= vh.current_index + ",";
            }
            str += "}";
            return str;
        }

    //    private void RemoveNotHiddenViesFromCache(bool top){
    //         if(top){
    //             for(int i= cacheTop.Count - 1; i>=0; i--){
    //                 if(!cacheTop[i].IsHidden()){
    //                         attachedScrap.Add(cacheTop[i]);
    //                         cacheTop[i].itemView.name = cacheTop[i].current_index.ToString() + " SCRAP";
    //                     //AddToAttachedScrap(cacheTop[i], true);
    //                     cacheTop.RemoveAt(i);
    //                 }
    //             }
    //         }else{ 
                
    //         // for(int i= cacheBot.Count - 1; i>=0; i--){
    //         //     if(!cacheBot[i].IsHidden()){
    //         //         attachedScrap.Add(cacheBot[i]);
    //         //         cacheBot[i].itemView.name = cacheBot[i].current_index.ToString() + " SCRAP";
    //         //        // AddToAttachedScrap(cacheBot[i], false);
    //         //         cacheBot.RemoveAt(i);
    //         //     }
    //         // }
    //         }
    //     }

        private void AddNewViewHoldersToCache(bool top){
            if(top){
                int nTop =  CACHE_SIZE -cacheTop.Count;
                for(int i=0; i < nTop; i++ ){
                    //int upper = GetUpperPosition(cacheTop);
                    //if(upper >= 0){
                        ViewHolder vh = TryGetViewHolderForPosition(GetUpperPosition(cacheTop.Count > 0 ? cacheTop : attachedScrap) + 1);
                        if(vh != null ){
                            vh.itemView.transform.SetParent(transform);              
                            vh.itemView.transform.SetAsLastSibling();                     
                            vh.itemView.name = vh.current_index.ToString() + " "  + " CACHE";
                            vh.itemView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                            vh.itemView.SetActive(true);
                            
                            ThrowToCache(vh, true);
                            OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.current_index);
                     //   }
                    }
                }
            }else{
                int nBot = CACHE_SIZE -cacheBot.Count;
                for(int i=0; i < nBot; i++ ){
                        ViewHolder vh = TryGetViewHolderForPosition(GetLowerPosition(cacheBot.Count > 0 ? cacheBot : attachedScrap) - 1);
                        if(vh != null){
                            vh.itemView.transform.SetParent(transform);                               
                            vh.itemView.transform.SetAsFirstSibling();               
                            vh.itemView.name = vh.current_index.ToString() + " "  + " CACHE";
                        vh.itemView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                        vh.itemView.SetActive(true);
                            

                            ThrowToCache(vh, false);
                            OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.current_index);
                        }
                }
            }
        }

        private void ThrowToPool(ViewHolder vh){
            if(pool.IsFull()){
                vh.Destroy();
            }else{
                vh.status = Status.RECYCLED;
                vh.itemView.transform.SetParent(poolObj.transform);
                vh.itemView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                vh.itemView.SetActive(false);
                pool.Add(vh);
            }
        }

        private void CleanCache(){
            for(int i=cacheBot.Count - 1; i>=0; i--){
                if(!cacheBot[i].IsHidden()){
                    attachedScrap.Add(cacheBot[i]);
                    cacheBot.RemoveAt(i);
                }
            }
            for(int i=cacheTop.Count - 1; i>=0; i--){
                if(!cacheTop[i].IsHidden()){
                    attachedScrap.Add(cacheTop[i]);
                    cacheTop.RemoveAt(i);
                }
            }

        }

        private void ThrowToCache(ViewHolder viewHolder, bool top){
            viewHolder.status = Status.CACHE;
            if(top){
                cacheTop .Add(viewHolder);
            }else{
                cacheBot.Add(viewHolder);
            } 
        }

        private void RemoveNotVisibleViewHolders(){
            attachedScrap.AddRange(cacheTop);
            attachedScrap.InsertRange(0, cacheBot);
            cacheTop.Clear();
            cacheBot.Clear();

            Sort(attachedScrap, true);

            for(int i= attachedScrap.Count - 1; i>=0; i--){
                if(attachedScrap[i].IsHidden() ){
                    ThrowToCache(attachedScrap[i], true);
                    attachedScrap.RemoveAt(i);
                }else{
                    break;
                }
            }

            Sort(attachedScrap, false);

            for(int i= attachedScrap.Count - 1; i>=0; i--){
                if(attachedScrap[i].IsHidden() ){
                    ThrowToCache(attachedScrap[i], false);
                    attachedScrap.RemoveAt(i);
                }else{
                    break;
                }
            }
        }

    private void RemoveViewHoldersFromCache(bool top){
        if(top){
            Sort(cacheTop, true);
            if(cacheTop.Count > CACHE_SIZE){
                for(int i=cacheTop.Count - 1; i>=CACHE_SIZE; i--){
                    ThrowToPool(cacheTop[i]);
                    cacheTop.RemoveAt(i);
                }
            }
        }else{
            Sort(cacheBot, false);
            if(cacheBot.Count > CACHE_SIZE){
                for(int i=cacheBot.Count - 1; i>=CACHE_SIZE; i--){
                        ThrowToPool(cacheBot[i]);
                        cacheBot.RemoveAt(i);
                        //   StartCoroutine(test(i));
                        // GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
                        // ThrowToPool(cacheBot[i]);
                        // GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
                        // Vector2 vec = GetComponent<RectTransform>().sizeDelta;
                        //   GetComponent<RectTransform>().sizeDelta = new Vector2( GetComponent<RectTransform>().sizeDelta.x,  GetComponent<RectTransform>().sizeDelta.y - 100);
                        //   Vector2 vec2 = GetComponent<RectTransform>().sizeDelta;
                        //  cacheBot.RemoveAt(i);
                    }
            }
        }
    }

    private IEnumerator test(int i){
         SetPivot(GetComponent<RectTransform>(), new Vector2(0.5f, 0));
          ThrowToPool(cacheBot[i]);
                       cacheBot.RemoveAt(i);
         yield return new WaitForEndOfFrame();
                     
                       SetPivot(GetComponent<RectTransform>(), new Vector2(0.5f, 1));

    }
    void OnGUI()
        {

            if (GUI.Button(new Rect(10, 70, 50, 30), "Click")){
                SetPivot(GetComponent<RectTransform>(), new Vector2(0.5f, 0));
               //  GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
               // StartCoroutine(test(0));
//GetComponent<RectTransform>().sizeDelta = new Vector2( GetComponent<RectTransform>().sizeDelta.x,  GetComponent<RectTransform>().sizeDelta.y - 100);
                  //   Vector2 vec2 = GetComponent<RectTransform>().sizeDelta;
                  //  cacheBot.RemoveAt(i);

            }
             //   Debug.Log("Clicked the button with text");
        }

            public  void SetPivot(RectTransform rectTransform, Vector2 pivot)
     {
         if (rectTransform == null) return;
 
         Vector2 size = rectTransform.rect.size;
         Vector2 deltaPivot = rectTransform.pivot - pivot;
         Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
         rectTransform.pivot = pivot;
         rectTransform.localPosition -= deltaPosition;
     }

    private void Sort(List<ViewHolder> list, bool upperFirst){
        for(int i=0; i<list.Count; i++){
            for(int j=i+1; j<list.Count; j++){
                if(upperFirst){
                    if(list[i].current_index > list[j].current_index){
                        ViewHolder aux = list[i];
                        list[i] = list[j];
                        list[j] = aux;
                    }
                }else{
                    if(list[i].current_index < list[j].current_index){
                        ViewHolder aux = list[i];
                        list[i] = list[j];
                        list[j] = aux;
                    }
                }
            }
        }
    }
     private int GetLowerPosition(List<ViewHolder> list){
            int lower = int.MaxValue;
            foreach(ViewHolder scrap in list){
                if(scrap.current_index< lower){
                    lower = scrap.current_index;
                }
            }
            return lower != int.MaxValue ? lower : -1;
        }

        private int GetUpperPosition(List<ViewHolder> list){
            int upper = -1;
            foreach(ViewHolder scrap in list){
                if(scrap.current_index > upper){
                    upper = scrap.current_index;
                }
            }
            return upper;
        }
        private void Clear(){
            foreach(Transform row in transform){
                Destroy(row.gameObject);
            }

            attachedScrap.Clear();

        }
    }
}
