using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclerView{
    public abstract class Adapter<T> : RecyclerView<T>
        where T : ViewHolder
    {
        public override void NotifyDatasetChanged(int pos  = 0)
        {
            if(pos < 0 || pos > GetItemCount())
            {
                return;
            }


            Clear();

            pool = new Pool(POOL_SIZE, CACHE_SIZE);

            if (GetItemCount() > 0)
            {
                ViewHolder vh = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });   
                vh.current_index = pos;
                vh.last_index = pos;
                vh.status = Status.SCRAP;
                AddToAttachedScrap(vh, true);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), pos);

                rowHeight = vh.rectTransform.rect.height;
                LIMIT_BOTTOM = ((GetItemCount() * (rowHeight + spacingY)) - SelfRectTransform.rect.height) - spacingY;
                GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, (rowHeight + spacingY) * pos);
                GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                int ATTACHED_SCRAP_SIZE = Mathf.FloorToInt(SelfRectTransform.rect.height / (rowHeight / 2) ); //TODO calcular
                Debug.Log(ATTACHED_SCRAP_SIZE);
                for (int i = pos + 1; i < ATTACHED_SCRAP_SIZE + pos; i++)
                {
                    if(i < GetItemCount())
                    {
                        ViewHolder vh2 = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                        vh2.current_index = i;
                        vh2.last_index = i;
                        vh2.status = Status.SCRAP;
                        AddToAttachedScrap(vh2, true);
                        OnBindViewHolder((T)Convert.ChangeType(vh2, typeof(T)), i);
                    }
                }
               
               


                ReorderList();                
            }
        }

    }
}
