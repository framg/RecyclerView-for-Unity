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
        private RectTransform SelfRectTransform { get; set; }
        private GameObject poolObj; 
        public ScrollRect scroll;
        public void Awake(){
            SelfRectTransform = GetComponent<RectTransform>();
            poolObj = new GameObject();
            poolObj.transform.SetParent(transform.parent);
            poolObj.name = "Pool";
        }

        public int end_position;
        public int start_position;
        private int POOL_SIZE = 10;
        private int CACHE_SIZE = 3;
        private float LIMIT_BOTTOM = 0;

        private int ATTACHED_SCRAP_SIZE = 12 ;
        
        public Pool pool;
        public List<ViewHolder> attachedScrap = new List<ViewHolder>();
        public List<ViewHolder> cacheTop = new List<ViewHolder>();
        public List<ViewHolder> cacheBot = new List<ViewHolder>();
        


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


        private void ClampList()
        {
            if (SelfRectTransform.offsetMax.y < 0)
            {
                SelfRectTransform.offsetMax = new Vector2(SelfRectTransform.offsetMax.x, 0);
                SelfRectTransform.sizeDelta = new Vector2(SelfRectTransform.sizeDelta.x, 0);
            }
            else if (SelfRectTransform.offsetMax.y > LIMIT_BOTTOM)
            {
                SelfRectTransform.offsetMax = new Vector2(SelfRectTransform.offsetMax.x, LIMIT_BOTTOM);
                SelfRectTransform.sizeDelta = new Vector2(SelfRectTransform.sizeDelta.x, 0);
            }
        }


        public void OnScroll(Vector2 pos){

            ClampList();
            RemoveNotVisibleViewHolders();
            RemoveViewHoldersFromCache(true);
            RemoveViewHoldersFromCache(false);
            AddNewViewHoldersToCache(true);
            AddNewViewHoldersToCache(false);
            ReorderList();
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

        private void AddNewViewHoldersToCache(bool top){
            if(top){
                int nTop =  CACHE_SIZE - cacheTop.Count;
                for(int i=0; i < nTop; i++ ){
                    ViewHolder vh = TryGetViewHolderForPosition(GetUpperPosition(cacheTop.Count > 0 ? cacheTop : attachedScrap) + 1);
                    if(vh != null ){
                        vh.itemView.transform.SetParent(transform);              
                        vh.itemView.transform.SetAsLastSibling();                     
                        vh.itemView.name = vh.current_index.ToString();
                        vh.itemView.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                        vh.itemView.SetActive(true);
                            
                        ThrowToCache(vh, true);
                        OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.current_index);    
                    }
                }
            }else{
                int nBot = CACHE_SIZE - cacheBot.Count;
                for(int i=0; i < nBot; i++ ){
                    ViewHolder vh = TryGetViewHolderForPosition(GetLowerPosition(cacheBot.Count > 0 ? cacheBot : attachedScrap) - 1);
                    if(vh != null){
                        vh.itemView.transform.SetParent(transform);                               
                        vh.itemView.transform.SetAsFirstSibling();               
                        vh.itemView.name = vh.current_index.ToString();
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
            attachedScrap.AddRange(cacheBot);
            cacheTop.Clear();
            cacheBot.Clear();

            Sort(attachedScrap, true);

            for (int i = attachedScrap.Count - 1; i >= 0; i--)
            {
                if (attachedScrap[i].IsHidden())
                {
                    ThrowToCache(attachedScrap[i], true);
                    attachedScrap.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }

            Sort(attachedScrap, false);

            for (int i = attachedScrap.Count - 1; i >= 0; i--)
            {
                if (attachedScrap[i].IsHidden())
                {
                    ThrowToCache(attachedScrap[i], false);
                    attachedScrap.RemoveAt(i);
                }
                else
                {
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
                }
            }
        }
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
