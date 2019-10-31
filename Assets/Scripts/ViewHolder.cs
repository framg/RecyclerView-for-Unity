using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecyclerView{
    public abstract class ViewHolder{
        public GameObject itemView;
        public RectTransform rectTransform;

        public int last_index, current_index;

        public Status status;



        public ViewHolder(GameObject itemView){
            this.itemView = itemView;
            this.rectTransform = itemView.GetComponent<RectTransform>();

        }

        public void Destroy(){
            GameObject.Destroy(itemView);
        }

        public bool IsHidden(){
            return !IsVisibleFrom(itemView.GetComponent<RectTransform>(), Camera.main);
        }

        private static bool IsVisibleFrom(RectTransform rectTransform, Camera camera)
        {
            return CountCornersVisibleFrom(rectTransform, camera) > 0; 
        }
        private static int CountCornersVisibleFrom(RectTransform rectTransform, Camera camera)
        {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height); 
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            for (var i = 0; i < objectCorners.Length; i++) 
            {
                
                if (screenBounds.Contains(objectCorners[i])) 
                {
                    visibleCorners++;
                }
            }
            return visibleCorners;
        }

        public int CompareTo(ViewHolder vh){
            if(vh.current_index > this.current_index){
                return -1;
            }else if(vh.current_index > this.current_index){
                return 1;
            }else{
                return 0;
            }
        }
    }


    public enum Status{
        SCRAP,
        CACHE,
        CACHE_TOP,
        CACHE_BOT,
        RECYCLED

    }
}