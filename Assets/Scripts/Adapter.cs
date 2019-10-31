using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclerView{
    public abstract class Adapter<T> : RecyclerView<T>, IDataObservable
        where T : ViewHolder
    {
        public void NotifyDatasetChanged()
        {
            OnDataChange();
        }
    }
}
