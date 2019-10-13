using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecyclerView{
    public abstract class ViewHolder{
        public GameObject itemView;

        public int last_index, current_index;

        public Status status;



        public ViewHolder(GameObject itemView){
            this.itemView = itemView;

        }

        public void Destroy(){
            GameObject.Destroy(itemView);
        }

        public bool IsHidden(){
            return !itemView.GetComponent<RectTransform>().IsVisibleFrom(Camera.main);
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