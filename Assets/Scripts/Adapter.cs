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
            Clear();

            pool = new Pool(POOL_SIZE, CACHE_SIZE);

            if (GetItemCount() > 0)
            {
                for (int i = pos; i < ATTACHED_SCRAP_SIZE + pos; i++)
                {
                    ViewHolder vh = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });


                    vh.current_index = i;
                    vh.last_index = i;
                    vh.status = Status.SCRAP;


                    AddToAttachedScrap(vh, true);

                    OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), i);
                }
                rowHeight = attachedScrap[0].rectTransform.rect.height;
                LIMIT_BOTTOM = ((GetItemCount() * (attachedScrap[0].rectTransform.rect.height + spacingY)) - SelfRectTransform.rect.height) - spacingY;
                GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, (attachedScrap[0].rectTransform.rect.height + spacingY) * pos);
                GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);

                ReorderList();                
            }
        }

    }
}
