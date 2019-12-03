/*
    MIT License

    Copyright (c) 2019 Framg

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if (UNITY_EDITOR) 
using UnityEditor;
using System.IO;
#endif
namespace UI
{
    /// <summary>
    /// Recycler view for Unity.
    /// List of elements, it use pooling to display items in the screen. So it's going to keep a small list instead of the full elements list.
    /// </summary>
    /// <typeparam name="T">T must be an extension of ViewHolder from RecyclerView.</typeparam>
    public abstract class RecyclerView<T> : MonoBehaviour, RecyclerView<T>.IAdapter
        where T : RecyclerView<T>.ViewHolder
    {
        private const bool DEBUG = false;


        #if (UNITY_EDITOR)
        [Range(0, 1f)]
        [ReadOnlyWhenPlaying]
        #endif
        public float decelerationRate = 0.5f;

        #if (UNITY_EDITOR)
        [ReadOnlyWhenPlaying]
        [Header("List orientation")]
        #endif
        public Orientation orientation;

        #if (UNITY_EDITOR)
        [ReadOnlyWhenPlaying]
        [Header("Margin between rows")]
        #endif
        public Vector2 Spacing;

        #if (UNITY_EDITOR)
        [ReadOnlyWhenPlaying]
        [Header("Set true to make the list reverse")]
        #endif
        public bool IsReverse;

        #if (UNITY_EDITOR)
        [Space]
        [ReadOnlyWhenPlaying]
        [Header("Pool size and cache size (do not modify if you are not sure)")]
        #endif
        public int PoolSize = 3;

        #if (UNITY_EDITOR)
        [ReadOnlyWhenPlaying]
        #endif
        public int CacheSize = 3;

        private Pool pool;
        private readonly List<IViewHolderInfo> AttachedScrap = new List<IViewHolderInfo>();
        private readonly List<IViewHolderInfo> Cache = new List<IViewHolderInfo>();

        public abstract GameObject OnCreateViewHolder();
        public abstract void OnBindViewHolder(T holder, int i);   
        public abstract int GetItemCount();

        private LayoutManager layoutManager;


        public abstract class Adapter : RecyclerView<T>, IDataObservable
        {
            /// <summary>
            /// 
            /// </summary>
            public void NotifyDatasetChanged()
            {
                OnDataChange();
            }
            /// <summary>
            /// Scroll to a certain position from [0, 
            /// </summary>
            /// <param name="pos"></param>
            public void ScrollBy(Vector2 pos)
            {
                layoutManager.ScrollTo(pos);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="position"></param>
            public void ScrollTo(int position)
            {
                layoutManager.ScrollTo(position);

            }
            /// <summary>
            /// /
            /// </summary>
            /// <param name="position"></param>
            public void SmothScrollTo(int position)
            {
                layoutManager.SmothScrollTo(position);
            }
        }

        private void Awake()
        {
            layoutManager = new LayoutManager(this, orientation);

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            layoutManager.Create();

            OnDataChange();
        }

        private void AddToAttachedScrap(IViewHolderInfo vh, bool up)
        {
            layoutManager.AttachToGrid(vh, up);         
            vh.ItemView.SetActive(true);
            AttachedScrap.Add(vh);
        }  


        private IViewHolderInfo TryGetViewHolderForPosition(int position)
        {
            if (position >= 0 && position < GetItemCount())
            {
                for(int i=0; i<AttachedScrap.Count; i++)
                {
                    if(AttachedScrap[i].CurrentIndex == position)
                    {
                        IViewHolderInfo v = AttachedScrap[i];
                        AttachedScrap.RemoveAt(i);
                        return v;
                    }
                }

                for(int i=0; i<Cache.Count; i++)
                {
                    if(Cache[i].CurrentIndex == position)
                    {
                        IViewHolderInfo v = Cache[i];
                        Cache.RemoveAt(i);
                        return v;
                    }
                }

                IViewHolderInfo vhrecycled;
                vhrecycled = pool.GetFromPool();
                if (vhrecycled != null)
                {
                    vhrecycled.Status = ViewHolder.Status.SCRAP;
                    vhrecycled.LastIndex = vhrecycled.CurrentIndex;
                    vhrecycled.CurrentIndex = position;
                    layoutManager.AttachToGrid(vhrecycled, true);
                    OnBindViewHolder((T)Convert.ChangeType(vhrecycled, typeof(T)), vhrecycled.CurrentIndex);
                    return vhrecycled;
                }


                IViewHolderInfo vh = (ViewHolder)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder() });
                vh.CurrentIndex = position;
                vh.LastIndex = position;
                vh.Status = ViewHolder.Status.SCRAP;
                layoutManager.AttachToGrid(vh, true);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.CurrentIndex);
                return vh;


            }
            else
            {
                return null;
            }
        }

        private void ThrowAttachedScrapToCache()
        {
            foreach (IViewHolderInfo vh in AttachedScrap)
            {
                ThrowToCache(vh);
            }
        }

        private void UpdateScrap()
        {
            int firstPosition = layoutManager.GetFirstPosition();
            List<IViewHolderInfo> TmpScrap = new List<IViewHolderInfo>();
            
            for(int i=firstPosition - 1; i< firstPosition + layoutManager.GetScreenListSize()+1; i++)
            {
                IViewHolderInfo vh = TryGetViewHolderForPosition(i);
                if(vh != null)
                {
                    TmpScrap.Add(vh);
                }
            }

            ThrowAttachedScrapToCache();
            AttachedScrap.Clear();
            AttachedScrap.AddRange(TmpScrap);

            if (DEBUG)
            {
                Debug.Log(ToString());
            }

        }


        public override string ToString()
        {
            string str = "";
            str += "Attached: {";
            foreach (IViewHolderInfo vh in AttachedScrap)
            {
                str += vh.CurrentIndex + ",";
            }
            str += "} Cache: {";
            foreach (IViewHolderInfo vh in Cache)
            {
                str += vh.CurrentIndex + ",";
            }
            str += "} Pool: {";
            foreach (IViewHolderInfo vh in pool.Scrap)
            {
                str += vh.CurrentIndex + ",";
            }
            str += "}";
            return str;
        }

        private void ThrowToPool(IViewHolderInfo vh)
        {
 
            vh.Status = ViewHolder.Status.RECYCLED;
            vh.ItemView.SetActive(false);
            IViewHolderInfo recycled = pool.Throw(vh);
            if(recycled != null)
            {
                recycled.Destroy();
            }
            
        }



        private void ThrowToCache(IViewHolderInfo viewHolder)
        {
            viewHolder.Status = ViewHolder.Status.CACHE;
            Cache.Add(viewHolder);
            if (Cache.Count > CacheSize)
            {
                ThrowToPool(Cache[0]);
                Cache.RemoveAt(0);
            }
        }


        private void Clear()
        {
            layoutManager.Clear();

            AttachedScrap.Clear();
            pool = null;
            
            Cache.Clear();

        }

        protected void OnDataChange(int pos = 0)
        {
            layoutManager.IsCreating = true;

            if (pos < 0 || pos > GetItemCount())
            {
                return;
            }
            
            Clear();

            pool = new Pool(PoolSize, CacheSize);

            if (GetItemCount() > 0)
            {
                IViewHolderInfo vh = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder() });
                vh.CurrentIndex = pos;
                vh.LastIndex = pos;
                vh.Status = ViewHolder.Status.SCRAP;
                AddToAttachedScrap(vh, true);
                layoutManager.SetPositionViewHolder(vh);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), pos);

               
                    
                layoutManager.OnDataChange(vh.ItemView, pos);

                int ATTACHED_SCRAP_SIZE = layoutManager.GetScreenListSize() + 1;

                for (int i = pos + 1; i < ATTACHED_SCRAP_SIZE + pos; i++)
                {
                    if (i < GetItemCount())
                    {
                        IViewHolderInfo vh2 = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder() });
                        vh2.CurrentIndex = i;
                        vh2.LastIndex = i;
                        vh2.Status = ViewHolder.Status.SCRAP;
                        AddToAttachedScrap(vh2, true);
                        layoutManager.SetPositionViewHolder(vh2);
                        OnBindViewHolder((T)Convert.ChangeType(vh2, typeof(T)), i);
                    }
                }            
                layoutManager.ClampList();
            }

            layoutManager.IsCreating = false;
        }

        private class Pool
        {
            
            int poolSize, cacheSize;
            public Pool(int poolSize, int cacheSize)
            {
                this.poolSize = poolSize;
                this.cacheSize = cacheSize;
            }
            public Queue<IViewHolderInfo> Scrap = new Queue<IViewHolderInfo>();


            public bool IsFull()
            {
                return Scrap.Count >= poolSize;
            }

            public IViewHolderInfo GetFromPool()
            {
                if (Scrap.Count > 0)
                {
                    return Scrap.Dequeue();
                }
                else
                {
                    return null;
                }
            }


            public IViewHolderInfo Throw(IViewHolderInfo vh)
            {
                if (Scrap.Count < poolSize)
                {
                    vh.Status = ViewHolder.Status.RECYCLED;
                    Scrap.Enqueue(vh);
                }
                else
                {
                    vh.Status = ViewHolder.Status.RECYCLED;
                    IViewHolderInfo recycled = Scrap.Dequeue();
                    Scrap.Enqueue(vh);
                    return recycled;
                }
                return null;
            }


        }


        private class LayoutManager
        {
            private Orientation orientation;
            private Vector2 RowDimension;
            private Vector2 RowScale;
            private ScrollRect ScrollRect;
            private RectTransform SelfRectTransform { get; set; }
            private RectTransform GridRectTransform { get; set; }
            private GameObject Grid;
            public float LIMIT_BOTTOM = 0;
            public bool IsCreating = false;
            private bool isDraging, isClickDown;
             
            private RecyclerView<T> recyclerView;

            public LayoutManager(RecyclerView<T> recyclerView, Orientation orientation)
            {
                this.recyclerView = recyclerView;
                this.orientation = orientation;
            }

            public int GetFirstPosition()
            {
                if (IsVerticalOrientation())
                {
                    if (recyclerView.IsReverse)
                    {
                        return Mathf.Abs(Mathf.RoundToInt(Mathf.Clamp(Grid.transform.GetComponent<RectTransform>().offsetMin.y, -LIMIT_BOTTOM, 0) / (GetRowSize().y)));

                    }
                    else
                    {
                        return Mathf.RoundToInt(Mathf.Clamp(Grid.transform.GetComponent<RectTransform>().offsetMin.y, 0, LIMIT_BOTTOM) / (GetRowSize().y));

                    }
                }
                else
                {
                   

                    if (recyclerView.IsReverse)
                    {
                        return Mathf.RoundToInt(Mathf.Clamp(Grid.transform.GetComponent<RectTransform>().offsetMin.x, 0, LIMIT_BOTTOM) / (GetRowSize().x));

                    }
                    else
                    {
                        return Mathf.Abs(Mathf.RoundToInt(Mathf.Clamp(Grid.transform.GetComponent<RectTransform>().offsetMin.x, -LIMIT_BOTTOM, 0) / (GetRowSize().x)));

                    }
                }



            }

            public int GetScreenListSize()
            {
                if (!IsVerticalOrientation())
                {
                    return Mathf.FloorToInt(Screen.width / GetRowSize().x);
                }
                else
                {
                    return Mathf.FloorToInt(Screen.height / GetRowSize().y);
                }
              
            }

            public void Create()
            {

                SelfRectTransform = recyclerView.GetComponent<RectTransform>();
                Grid = new GameObject();
                Grid.name = "Grid";
                GridRectTransform = Grid.AddComponent<RectTransform>();
                GridRectTransform.sizeDelta = Vector2.zero;

                if (IsVerticalOrientation())
                {
                    if (recyclerView.IsReverse)
                    {
                        GridRectTransform.anchorMax = new Vector2(0.5f, 0f);
                        GridRectTransform.anchorMin = new Vector2(0.5f, 0f);
                        GridRectTransform.pivot = new Vector2(0.5f, 0f);
                    }
                    else
                    {
                        GridRectTransform.anchorMax = new Vector2(0.5f, 1f);
                        GridRectTransform.anchorMin = new Vector2(0.5f, 1f);
                        GridRectTransform.pivot = new Vector2(0.5f, 1f);
                    }

                }
                else
                {
                    if (recyclerView.IsReverse)
                    {
                        GridRectTransform.anchorMax = new Vector2(1f, 0.5f);
                        GridRectTransform.anchorMin = new Vector2(1f, 0.5f);
                        GridRectTransform.pivot = new Vector2(1f, 0.5f);
                    }
                    else
                    {
                        GridRectTransform.anchorMax = new Vector2(0f, 0.5f);
                        GridRectTransform.anchorMin = new Vector2(0f, 0.5f);
                        GridRectTransform.pivot = new Vector2(0f, 0.5f);
                    }
                }

                Grid.transform.SetParent(recyclerView.transform);
                GridRectTransform.anchoredPosition = Vector3.zero;


                ScrollRect = recyclerView.GetComponent<ScrollRect>();
                if (ScrollRect == null)
                {
                    ScrollRect = recyclerView.gameObject.AddComponent<ScrollRect>();
                }
                ScrollRect.content = GridRectTransform;
                ScrollRect.onValueChanged.AddListener(delegate { OnScroll(); });
                ScrollRect.viewport = SelfRectTransform;
                ScrollRect.content = GridRectTransform;
                ScrollRect.movementType = ScrollRect.MovementType.Unrestricted;
                ScrollRect.inertia = true;
                ScrollRect.decelerationRate = recyclerView.decelerationRate;
                ScrollRect.scrollSensitivity = 10f;
                ScrollRect.vertical = IsVerticalOrientation();
                ScrollRect.horizontal = !IsVerticalOrientation();

                if (recyclerView.GetComponent<Image>() == null)
                {
                    Image image = recyclerView.gameObject.AddComponent<Image>();
                    image.color = new Color(0, 0, 0, 0.01f);
                }
                if (recyclerView.GetComponent<Mask>() == null)
                {
                    recyclerView.gameObject.AddComponent<Mask>();
                }

                if (recyclerView.gameObject.GetComponent<EventTrigger>() == null)
                {
                    EventTrigger eventTrigger = recyclerView.gameObject.AddComponent<EventTrigger>();
                    EventTrigger.Entry pointup = new EventTrigger.Entry();
                    pointup.eventID = EventTriggerType.PointerUp;
                    pointup.callback.AddListener((data) => { OnClickUp(); });
                    eventTrigger.triggers.Add(pointup);

                    EventTrigger.Entry pointdown = new EventTrigger.Entry();
                    pointdown.eventID = EventTriggerType.PointerDown;
                    pointdown.callback.AddListener((data) => { OnClickDown(); });
                    eventTrigger.triggers.Add(pointdown);

                    EventTrigger.Entry drag = new EventTrigger.Entry();
                    drag.eventID = EventTriggerType.Drag;
                    drag.callback.AddListener((data) => { OnDrag(); });
                    eventTrigger.triggers.Add(drag);
                }
            }

            private void OnDrag()
            {
                isDraging = true;
            }

            private void OnClickDown()
            {
                isClickDown = true;
            }

            private void OnClickUp()
            {
                isDraging = false;
                isClickDown = false;
            }

            public Vector2 GetRowSize()
            {
                if (IsVerticalOrientation())
                {
                    return new Vector2(0, ((RowDimension.y * RowScale.y) + recyclerView.Spacing.y));
                }
                else
                {
                    return new Vector2(((RowDimension.x * RowScale.x) + recyclerView.Spacing.x), 0);
                }              
            }


            public void SetPositionViewHolder(IViewHolderInfo vh)
            {
                Vector2 size = GetRowSize();
    

                if (IsVerticalOrientation())
                {
                    if (recyclerView.IsReverse)
                    {
                        vh.RectTransform.localPosition = new Vector3(0, (vh.CurrentIndex * size.y), 0);
                    }
                    else
                    {
                        vh.RectTransform.localPosition = new Vector3(0, (-vh.CurrentIndex * size.y), 0);
                    }
                }
                else
                {
                    if (recyclerView.IsReverse)
                    {
                        vh.RectTransform.localPosition = new Vector3((-vh.CurrentIndex * size.x), 0, 0);
                    }
                    else
                    {
                        vh.RectTransform.localPosition = new Vector3((vh.CurrentIndex * size.x), 0, 0);
                    }
                }
            }

           

            private void Invalidate()
            {

                if (IsVerticalOrientation())
                {
                    if (recyclerView.IsReverse)
                    {
                        if (GridRectTransform.offsetMax.y < -LIMIT_BOTTOM)
                        {
                            recyclerView.OnDataChange(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            recyclerView.OnDataChange(0);
                        }
                    }
                    else
                    {
                        if (GridRectTransform.offsetMax.y > LIMIT_BOTTOM)
                        {
                            recyclerView.OnDataChange(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            recyclerView.OnDataChange(0);
                        }
                    }
                }
                else
                {
                    if (recyclerView.IsReverse)
                    {
                        if (GridRectTransform.offsetMax.x > LIMIT_BOTTOM)
                        {
                            recyclerView.OnDataChange(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            recyclerView.OnDataChange(0);
                        }
                    }
                    else
                    {
                        if (GridRectTransform.offsetMax.x < -LIMIT_BOTTOM)
                        {
                            recyclerView.OnDataChange(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            recyclerView.OnDataChange(0);
                        }
                    }
                }
                if (DEBUG)
                {
                    Debug.Log("MODEL IS INVALID");
                    Debug.Log(GridRectTransform.offsetMax);
                }
            }

            private void OnScroll()
            {
                if (!IsCreating)
                {
                    if (IsStateValid())
                    {             
                        recyclerView.UpdateScrap();
                        ClampList();
                    }
                    else
                    {
                        Invalidate();
                    }

                }
            }

            public void OnDataChange(GameObject initialVH, int pos = 0)
            {
                RowDimension = new Vector2(initialVH.GetComponent<RectTransform>().rect.width, initialVH.GetComponent<RectTransform>().rect.height);
                RowScale = initialVH.GetComponent<RectTransform>().localScale;
                Vector2 RowSize = GetRowSize();

                if (IsVerticalOrientation())
                {
                    LIMIT_BOTTOM = ((recyclerView.GetItemCount() * RowSize.y) - SelfRectTransform.rect.height) - recyclerView.Spacing.y;

                    if (recyclerView.IsReverse)
                    {
                        GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, -RowSize.y * pos);
                        GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                    }
                    else
                    {
                        GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, RowSize.y * pos);
                        GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                    }

                }
                else
                {
                    LIMIT_BOTTOM = ((recyclerView.GetItemCount() * RowSize.x) - SelfRectTransform.rect.width) - recyclerView.Spacing.x;


                    if (recyclerView.IsReverse)
                    {
                        GridRectTransform.offsetMax = new Vector2(RowSize.x * pos, GridRectTransform.offsetMax.y);
                        GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                    }
                    else
                    {
                        GridRectTransform.localPosition = new Vector2(-RowSize.x * pos, GridRectTransform.localPosition.y);
                        GridRectTransform.offsetMax = new Vector2(-RowSize.x * pos, GridRectTransform.offsetMax.y);
                        GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                    }

                }
            }

            private IEnumerator IScrollTo(Vector2 dir, float speed = 50)
            {
                ScrollRect.inertia = false;
                if (IsVerticalOrientation())
                {
                    Vector2 v = new Vector2(0, dir.y * LIMIT_BOTTOM);
                    bool goUp = GridRectTransform.offsetMax.y > v.y;
                    float y = GridRectTransform.offsetMax.y;
                    while (goUp ? GridRectTransform.offsetMax.y > v.y : GridRectTransform.offsetMax.y < v.y)
                    {
                        y += goUp ? -speed : speed;

                        if (y > LIMIT_BOTTOM)
                        {
                            y = LIMIT_BOTTOM;
                            GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, y);
                            GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                            OnScroll();
                            break;
                        }

                        GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, y);
                        GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                        OnScroll();
                        yield return new WaitForEndOfFrame();


                        if (isClickDown)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Vector2 v = new Vector2(dir.x * LIMIT_BOTTOM, 0);
                    bool goUp = GridRectTransform.offsetMax.x > v.x;
                    float y = GridRectTransform.offsetMax.x;
                    while (goUp ? GridRectTransform.offsetMax.x > v.x : GridRectTransform.offsetMax.x < v.x)
                    {
                       

                        y += goUp ? -speed : speed;

                        if (y > LIMIT_BOTTOM)
                        {
                            y = LIMIT_BOTTOM;
                            GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, y);
                            GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                            OnScroll();
                            break;
                        }

                        GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, y);
                        GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                        OnScroll();
                        yield return new WaitForEndOfFrame();

                        if (isClickDown)
                        {
                            break;
                        }

                    }
                }
                ScrollRect.inertia = true;
            }

            public void ScrollTo(Vector2 pos)
            {
                recyclerView.StartCoroutine(IScrollTo(pos));
            }

            public void ScrollTo(int position)
            {
                recyclerView.StartCoroutine(INotifyDatasetChanged(position));

            }

            public void SmothScrollTo(int position)
            {
                if (IsVerticalOrientation())
                {
                    recyclerView.StartCoroutine(IScrollTo(new Vector2(0, (GetRowSize().y * position) / LIMIT_BOTTOM)));
                }
                else
                {
                    recyclerView.StartCoroutine(IScrollTo(new Vector2(((GetRowSize().x * position) / LIMIT_BOTTOM), 0)));
                }
            }


            private IEnumerator INotifyDatasetChanged(int pos = 0)
            {
                ScrollRect.inertia = false;
                recyclerView.OnDataChange(pos);
                yield return new WaitForEndOfFrame();
                OnScroll();
                ScrollRect.inertia = true;
            }




            public void AttachToGrid(IViewHolderInfo vh, bool up)
            {
                vh.ItemView.transform.SetParent(Grid.transform);
                if (up)
                {
                    vh.ItemView.transform.SetAsLastSibling();
                }
                else
                {
                    vh.ItemView.transform.SetAsFirstSibling();
                }
                vh.ItemView.name = vh.CurrentIndex.ToString();
                vh.ItemView.SetActive(true);
                SetPivot(vh.RectTransform);
                SetPositionViewHolder(vh);
            }



            private bool IsStateValid()
            {
                if(recyclerView.GetItemCount() == 0)
                {
                    return true;
                }
                foreach (IViewHolderInfo vh in recyclerView.AttachedScrap)
                {
                    if (!vh.IsHidden())
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool IsVerticalOrientation()
            {
                return orientation == Orientation.VERTICAL;
            }

            public void Clear()
            {
                foreach (Transform row in Grid.transform)
                {
                    Destroy(row.gameObject);
                }
            }

            private void SetPivot(RectTransform rect)
            {
                if (IsVerticalOrientation())
                {
                    if (recyclerView.IsReverse)
                    {
                        rect.pivot = new Vector2(0.5f, 0f);
                    }
                    else
                    {
                        rect.pivot = new Vector2(0.5f, 1f);
                    }
                }
                else
                {
                    if (recyclerView.IsReverse)
                    {
                        rect.pivot = new Vector2(1f, 0.5f);
                    }
                    else
                    {
                        rect.pivot = new Vector2(0f, 0.5f);
                    }
                }
            }


            public void ClampList()
            {
                if (IsVerticalOrientation())
                {
                    if (recyclerView.IsReverse)
                    {
                        if (GridRectTransform.offsetMax.y > 0)
                        {
                            GridRectTransform.localPosition = new Vector2(GridRectTransform.localPosition.x, 0);
                            GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, 0);
                            GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                        }
                        else if (GridRectTransform.offsetMax.y < -LIMIT_BOTTOM)
                        {
                            GridRectTransform.localPosition = new Vector2(GridRectTransform.localPosition.x, -LIMIT_BOTTOM);
                            GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, -LIMIT_BOTTOM);
                            GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                        }
                    }
                    else
                    {
                        if (GridRectTransform.offsetMax.y < 0)
                        {
                            GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, 0);
                            GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                        }
                        else if (GridRectTransform.offsetMax.y > LIMIT_BOTTOM)
                        {
                            GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, LIMIT_BOTTOM);
                            GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                        }
                    }

                }
                else
                {

                    if (recyclerView.IsReverse)
                    {
                        if (GridRectTransform.offsetMax.x < 0)
                        {
                            GridRectTransform.offsetMax = new Vector2(0, GridRectTransform.offsetMax.y);
                            GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                        }
                        else if (GridRectTransform.offsetMax.x > LIMIT_BOTTOM)
                        {
                            GridRectTransform.offsetMax = new Vector2(LIMIT_BOTTOM, GridRectTransform.offsetMax.y);
                            GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                        }
                    }
                    else
                    {
                        if (GridRectTransform.offsetMax.x > 0)
                        {
                            GridRectTransform.localPosition = new Vector2(0, GridRectTransform.localPosition.y);
                            GridRectTransform.offsetMax = new Vector2(0, GridRectTransform.offsetMax.y);
                            GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                        }
                        else if (GridRectTransform.offsetMax.x < -LIMIT_BOTTOM)
                        {
                            GridRectTransform.localPosition = new Vector2(-LIMIT_BOTTOM, GridRectTransform.localPosition.y);
                            GridRectTransform.offsetMax = new Vector2(-LIMIT_BOTTOM, GridRectTransform.offsetMax.y);
                            GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                        }
                    }
                }
            }

        }


        public enum Orientation
        {
            VERTICAL,
            HORIZONTAL
        }

        private interface IViewHolderInfo
        {
            int LastIndex { get; set; }
            int CurrentIndex { get; set; }
            GameObject ItemView { get; set; }
            RectTransform RectTransform { get; set; }
            ViewHolder.Status Status { get; set; }
            void Destroy();
            bool IsHidden();
        }

        public abstract class ViewHolder : IViewHolderInfo
        {
            GameObject itemView;
            RectTransform rectTransform;
            int last_index, current_index;
            Status status;
            long timeStamp;

            int IViewHolderInfo.LastIndex { get => last_index;  set => last_index = value; }
            int IViewHolderInfo.CurrentIndex { get => current_index; set => current_index = value; }
            GameObject IViewHolderInfo.ItemView { get => itemView; set => itemView = value; }
            RectTransform IViewHolderInfo.RectTransform { get => rectTransform; set => rectTransform = value; }
            Status IViewHolderInfo.Status { get => status; set => status = value; }

            public ViewHolder(GameObject itemView)
            {
                this.itemView = itemView;
                this.rectTransform = itemView.GetComponent<RectTransform>();

            }

            public int GetAdapterPosition()
            {
                return current_index;
            }

            private void Destroy()
            {
                GameObject.Destroy(itemView);
            }

            private bool IsHidden()
            {
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

            private int CompareTo(ViewHolder vh)
            {
                if (vh.current_index > this.current_index)
                {
                    return -1;
                }
                else if (vh.current_index > this.current_index)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            void IViewHolderInfo.Destroy()
            {
                Destroy();
            }

            bool IViewHolderInfo.IsHidden()
            {
                return IsHidden();
            }

            public enum Status
            {
                SCRAP,
                CACHE,
                RECYCLED

            }

        }

        private interface IAdapter
        {
            GameObject OnCreateViewHolder();
            void OnBindViewHolder(T holder, int i);
            int GetItemCount();
        }


        private interface IDataObservable
        {

            void NotifyDatasetChanged();

        }

        private interface IRecyclerView
        {
            void ScrollTo(Vector2 pos);
            void ScrollTo(int position);
            void SmothScrollTo(int position);

        }
    }

#if (UNITY_EDITOR) 

    public class Menu : EditorWindow
    {
        string objNames = "";

        void OnGUI()
        {
            EditorGUI.DropShadowLabel(new Rect(0, 0, position.width, 20),
                "Choose a name for the adapter:");

            objNames = EditorGUI.TextField(new Rect(10, 25, position.width - 20, 20),
                "Name:",
                objNames);

            if (GUI.Button(new Rect(0, 50, position.width, 30), "Create"))
            {
                Selection.activeTransform = Create(objNames);
                Close();
            }
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        [MenuItem("GameObject/UI/RecyclerView", false, 0)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            var window = ScriptableObject.CreateInstance<Menu>();
            window.Show();
        }

        private static Transform Create(string name)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject();
                canvasObj.AddComponent<RectTransform>();
                canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject script = new GameObject();
            script.name = name;
            script.AddComponent<RectTransform>();
            script.transform.SetParent(canvas.transform);
            CreateScript(script);
            return script.transform;
        }

        private static void CreateScript(GameObject obj)
        {
            string name = obj.name;
            string copyPath;
            MonoScript script;
            int i = 1;
            copyPath = "Assets/" + name + ".cs";
            script = (MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript));
            if (script != null)
            {
                do
                {
                    name = obj.name + i;
                    copyPath = "Assets/" + name + ".cs";
                    script = (MonoScript)AssetDatabase.LoadAssetAtPath(copyPath, typeof(MonoScript));
                    i++;
                } while (script != null);
            }

            if (File.Exists(copyPath) == false)
            { // do not overwrite
                using (StreamWriter outfile =
                    new StreamWriter(copyPath))
                {
                    string file=
                        "using UnityEngine;\n" +
                        "using System.Collections;\n" +
                        "\n" +
                        "public class {{Name}} : UI.RecyclerView<{{Name}}.Holder>.Adapter {\n" +
                        "\n" +
                        "    public override int GetItemCount()\n" +
                        "    {\n" +
                        "        throw new System.NotImplementedException();\n" +
                        "    }\n" +
                        "\n" +
                        "    public override void OnBindViewHolder(Holder holder, int i)\n" +
                        "    {\n" +
                        "        throw new System.NotImplementedException();\n" +
                        "    }\n" +
                        "\n" +
                        "    public override GameObject OnCreateViewHolder()\n" +
                        "    {\n" +
                        "        throw new System.NotImplementedException();\n" +
                        "    }\n" +
                        "\n" +
                        "    public class Holder : ViewHolder\n" +
                        "    {\n" +
                        "        public Holder(GameObject itemView) : base(itemView)\n" +
                        "        {\n" +
                        "        }\n" +
                        "    }\n" +
                        "}\n" +
                        "\n";


                    outfile.WriteLine(file.Replace("{{Name}}", name));
                }
            }
            AssetDatabase.Refresh();
            CompileScript compileScript = obj.AddComponent<CompileScript>();
            compileScript.ScriptName = name;
        }


    }
    [ExecuteInEditMode]
    public class CompileScript : MonoBehaviour
    {
        public string ScriptName;

        void Update()
        {
            if (!EditorApplication.isCompiling)
            {
                gameObject.AddComponent(System.Type.GetType(ScriptName));
                DestroyImmediate(this);
            }

        }

    }

    public class ReadOnlyWhenPlayingAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ReadOnlyWhenPlayingAttribute))]
    public class ReadOnlyWhenPlayingAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
       
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = !Application.isPlaying;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}
