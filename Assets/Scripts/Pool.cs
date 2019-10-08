using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecyclerView{
    public  class Pool{
        int poolSize, cacheSize;
        public Pool(int poolSize,int cacheSize){
            this.poolSize = poolSize;
            this.cacheSize = cacheSize;
        }
       // public List<ViewHolder> Cache = new List<ViewHolder>();
        public List<ViewHolder> Scrap = new List<ViewHolder>();


        public bool IsFull(){
            return Scrap.Count >= poolSize;
        }

        public ViewHolder GetFromPool(int position, bool recycle = false){
            foreach(ViewHolder vh in Scrap){
                if(vh.current_index == position){
                    Scrap.Remove(vh);
                    return vh;
                }
            }
            if(recycle){
                ViewHolder vh2 = Scrap.Count > 0 ? Scrap[0] : null; //TODO coger por antiguedad
                    if(vh2 != null){
                        Scrap.Remove(vh2);
                    }
                    return vh2;
            }else{
                return null;
            }
        }

        // public ViewHolder GetFromCache(int position){
        //     foreach(ViewHolder vh in Cache){
        //         if(vh.current_index == position){
        //             Cache.Remove(vh);
        //             return vh;
        //         }
        //     }
        //     return null;
        // }



        public void Add(ViewHolder vh){
             if(Scrap.Count < poolSize){
                    vh.status = Status.RECYCLED;
                    Scrap.Add(vh);
                }else{
                     vh.status = Status.RECYCLED;
                    Scrap.Add(vh);
                    Scrap.RemoveAt(0);
                }
            //  if(Scrap.Count < poolSize){
            //         vh2.status = Status.RECYCLED;
            //         Scrap.Add(vh2);
            //     }else{
            //          vh2.status = Status.RECYCLED;
            //         Scrap.Add(vh2);
            //         Scrap.RemoveAt(0);
            //     }
            // if(Cache.Count < cacheSize){
            //     vh.status = Status.CACHE;
            //     Cache.Add(vh);
            // }else{
            //     ViewHolder vh2 = Cache[0];           
            //     Cache.RemoveAt(0);
            //      vh.status = Status.CACHE;
            //     Cache.Add(vh);
            //     if(Scrap.Count < poolSize){
            //         vh2.status = Status.RECYCLED;
            //         Scrap.Add(vh2);
            //     }else{
            //          vh2.status = Status.RECYCLED;
            //         Scrap.Add(vh2);
            //         Scrap.RemoveAt(0);
            //     }
            // }
        }


    }
}