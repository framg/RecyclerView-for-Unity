using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclerView
{
    public interface IAdapter<in T> where T : ViewHolder
    {
        //void SetDataObserver(AdapterDataObserver adapterDataObserver);
        //GameObject OnCreateViewHolder(Transform parent);
        //void OnBindViewHolder(T holder,  int i);
        //int GetItemCount();
    }
}
