using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclerView
{
    public class RecyclerView : MonoBehaviour
    {
        private Adapter<ViewHolder> Adapter;
        private ScrollRect ScrollRect;
        private RectTransform SelfRectTransform { get; set; }
        private RectTransform GridRectTransform { get; set; }
        private GameObject Grid;
        private GameObject poolObj;
        public int end_position;
        public int start_position;
        private int POOL_SIZE = 10;
        private int CACHE_SIZE = 3;
        private float LIMIT_BOTTOM = 0;

        private int ATTACHED_SCRAP_SIZE = 12;

        public Pool pool;
        public List<ViewHolder> attachedScrap = new List<ViewHolder>();
        public List<ViewHolder> cacheTop = new List<ViewHolder>();
        public List<ViewHolder> cacheBot = new List<ViewHolder>();




    }

}
