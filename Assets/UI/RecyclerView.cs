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
    public abstract class RecyclerView<T> : MonoBehaviour, RecyclerView<T>.IAdapter
        where T : RecyclerView<T>.ViewHolder
    {

        [Range(0, 1f)]
        [ReadOnlyWhenPlaying]
        public float decelerationRate = 0.5f;

        [ReadOnlyWhenPlaying]
        [Header("List orientation")]
        public Orientation orientation;

        [ReadOnlyWhenPlaying]
        [Header("Margin between rows")]
        public Vector2 Spacing;

        [ReadOnlyWhenPlaying]
        [Header("Set true to make the list reverse")]
        public bool IsReverse;

        [Space]
        [ReadOnlyWhenPlaying]
        [Header("Pool size and cache size (do not modify if you are not sure)")]
        public int PoolSize = 10;

        [ReadOnlyWhenPlaying]
        public int CacheSize = 3;

        private Pool pool;
        private List<IViewHolderInfo> attachedScrap = new List<IViewHolderInfo>();
        private List<IViewHolderInfo> cacheTop = new List<IViewHolderInfo>();
        private List<IViewHolderInfo> cacheBot = new List<IViewHolderInfo>();


        public abstract GameObject OnCreateViewHolder(Transform parent);
        public abstract void OnBindViewHolder(T holder, int i);   
        public abstract int GetItemCount();

        private LayoutManager layoutManager;

        public void Awake()
        {
            layoutManager = new LayoutManager(this, orientation);

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            layoutManager.Create();

            OnDataChange();
        }

        private IViewHolderInfo GetViewHolderFromScrap(int position)
        {
            foreach (IViewHolderInfo vh in attachedScrap)
            {
                if (vh.CurrentIndex == position)
                {
                    return vh;
                }
            }
            return null;
        }

        private void AddToAttachedScrap(IViewHolderInfo vh, bool attachTop)
        {
            layoutManager.AttachToGrid(vh, attachTop);         
            vh.ItemView.SetActive(true);
            attachedScrap.Add(vh);
        }  

        private IViewHolderInfo GetFromCache(int i, bool top)
        {
            if (top)
            {
                foreach (IViewHolderInfo vh in cacheTop)
                {
                    if (vh.CurrentIndex == i)
                    {
                        cacheTop.Remove(vh);
                        return vh;
                    }
                }
            }
            else
            {
                foreach (IViewHolderInfo vh in cacheBot)
                {
                    if (vh.CurrentIndex == i)
                    {
                        cacheBot.Remove(vh);
                        return vh;
                    }
                }
            }
            return null;
        }

        private IViewHolderInfo TryGetViewHolderForPosition(int position)
        {
            if (position >= 0 && position < GetItemCount())
            {
                IViewHolderInfo botCache = GetFromCache(position, false);
                if (botCache != null)
                {
                    botCache.Status = ViewHolder.Status.CACHE;
                    botCache.CurrentIndex = position;
                    botCache.ItemView.name = position.ToString();

                    return botCache;
                }
                IViewHolderInfo topCache = GetFromCache(position, true);
                if (topCache != null)
                {
                    topCache.Status = ViewHolder.Status.CACHE;
                    topCache.CurrentIndex = position;
                    topCache.ItemView.name = position.ToString();

                    return topCache;
                }
                IViewHolderInfo vhrecycled;
                vhrecycled = pool.GetFromPool(position);
                if (vhrecycled != null)
                {
                    vhrecycled.Status = ViewHolder.Status.SCRAP;
                    vhrecycled.LastIndex = vhrecycled.CurrentIndex;
                    vhrecycled.CurrentIndex = position;
                    return vhrecycled;
                }

                if (pool.IsFull())
                {
                    vhrecycled = pool.GetFromPool(position, true);
                    vhrecycled.Status = ViewHolder.Status.SCRAP;
                    vhrecycled.LastIndex = vhrecycled.CurrentIndex;
                    vhrecycled.CurrentIndex = position;
                    return vhrecycled;

                }
                else
                {
                    IViewHolderInfo vh = (ViewHolder)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });

                    vh.CurrentIndex = position;
                    vh.LastIndex = position;
                    vh.Status = ViewHolder.Status.SCRAP;

                    return vh;
                }

            }
            else
            {
                return null;
            }
        }


        private int GetLowerPosition()
        {
            int lower = int.MaxValue;
            foreach (IViewHolderInfo scrap in attachedScrap)
            {
                if (scrap.CurrentIndex < lower)
                {
                    lower = scrap.CurrentIndex;
                }
            }
            return lower;
        }

        private int GetUpperPosition()
        {
            int upper = 0;
            foreach (IViewHolderInfo scrap in attachedScrap)
            {
                if (scrap.CurrentIndex > upper)
                {
                    upper = scrap.CurrentIndex;
                }
            }
            return upper;
        }

        private int GetLowerChild()
        {
            int lower = int.MaxValue;
            foreach (IViewHolderInfo scrap in attachedScrap)
            {
                if (scrap.ItemView.transform.GetSiblingIndex() < lower)
                {
                    lower = scrap.ItemView.transform.GetSiblingIndex();
                }
            }
            return lower;
        }


        private int GetUpperChild()
        {
            int upper = 0;
            foreach (IViewHolderInfo scrap in attachedScrap)
            {
                if (scrap.ItemView.transform.GetSiblingIndex() > upper)
                {
                    upper = scrap.ItemView.transform.GetSiblingIndex();
                }
            }
            return upper;
        }

        private void OnScroll()
        {
            RemoveNotVisibleViewHolders();
            RemoveViewHoldersFromCache(true);
            RemoveViewHoldersFromCache(false);
            AddNewViewHoldersToCache(true);
            AddNewViewHoldersToCache(false);
        }


        public override string ToString()
        {
            string str = "";
            str += "Attached: {";
            foreach (IViewHolderInfo vh in attachedScrap)
            {
                str += vh.CurrentIndex + ",";
            }
            str += "} Cache Top: {";
            foreach (IViewHolderInfo vh in cacheTop)
            {
                str += vh.CurrentIndex + ",";
            }
            str += "} Cache Bot: {";
            foreach (IViewHolderInfo vh in cacheBot)
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

        private void AddNewViewHoldersToCache(bool top)
        {
            if (top)
            {
                int nTop = CacheSize - cacheTop.Count;
                for (int i = 0; i < nTop; i++)
                {
                    IViewHolderInfo vh = TryGetViewHolderForPosition(Utils.GetUpperPosition(cacheTop.Count > 0 ? cacheTop : attachedScrap) + 1);
                    if (vh != null)
                    {
                        layoutManager.AttachToGrid(vh, top);
                        ThrowToCache(vh, true);
                        OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.CurrentIndex);
                    }
                }
            }
            else
            {
                int nBot = CacheSize - cacheBot.Count;
                for (int i = 0; i < nBot; i++)
                {
                    IViewHolderInfo vh = TryGetViewHolderForPosition(Utils.GetLowerPosition(cacheBot.Count > 0 ? cacheBot : attachedScrap) - 1);
                    if (vh != null)
                    {
                        layoutManager.AttachToGrid(vh, top);
                        ThrowToCache(vh, false);
                        OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.CurrentIndex);
                    }
                }
            }
        }

        private void ThrowToPool(IViewHolderInfo vh)
        {
            if (pool.IsFull())
            {
                vh.Destroy();
            }
            else
            {
                vh.Status = ViewHolder.Status.RECYCLED;
                vh.ItemView.SetActive(false);
                pool.Add(vh);
            }
        }



        private void ThrowToCache(IViewHolderInfo viewHolder, bool top)
        {
            viewHolder.Status = ViewHolder.Status.CACHE;
            if (top)
            {
                cacheTop.Add(viewHolder);
            }
            else
            {
                cacheBot.Add(viewHolder);
            }
        }

        private void RemoveNotVisibleViewHolders()
        {
            attachedScrap.AddRange(cacheTop);
            attachedScrap.AddRange(cacheBot);
            cacheTop.Clear();
            cacheBot.Clear();

            Utils.Sort(attachedScrap, true);

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

            Utils.Sort(attachedScrap, false);

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

        private void RemoveViewHoldersFromCache(bool top)
        {
            if (top)
            {
                Utils.Sort(cacheTop, true);
                if (cacheTop.Count > CacheSize)
                {
                    for (int i = cacheTop.Count - 1; i >= CacheSize; i--)
                    {
                        ThrowToPool(cacheTop[i]);
                        cacheTop.RemoveAt(i);
                    }
                }
            }
            else
            {
                Utils.Sort(cacheBot, false);
                if (cacheBot.Count > CacheSize)
                {
                    for (int i = cacheBot.Count - 1; i >= CacheSize; i--)
                    {
                        ThrowToPool(cacheBot[i]);
                        cacheBot.RemoveAt(i);
                    }
                }
            }
        }
        
        private void Clear()
        {
            layoutManager.Clear();

            attachedScrap.Clear();
            pool = null;

            cacheBot.Clear();
            cacheTop.Clear();

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
                IViewHolderInfo vh = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                vh.CurrentIndex = pos;
                vh.LastIndex = pos;
                vh.Status = ViewHolder.Status.SCRAP;
                AddToAttachedScrap(vh, true);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), pos);

                int ATTACHED_SCRAP_SIZE = layoutManager.OnDataChange(vh.ItemView, pos);
               
                for (int i = pos + 1; i < ATTACHED_SCRAP_SIZE + pos; i++)
                {
                    if (i < GetItemCount())
                    {
                        IViewHolderInfo vh2 = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                        vh2.CurrentIndex = i;
                        vh2.LastIndex = i;
                        vh2.Status = ViewHolder.Status.SCRAP;
                        AddToAttachedScrap(vh2, true);
                        OnBindViewHolder((T)Convert.ChangeType(vh2, typeof(T)), i);
                    }
                }                     
                layoutManager.ReorderList();
                layoutManager.ClampList();
            }

            layoutManager.IsCreating = false;
        }


        public void ScrollTo(Vector2 pos)
        {
            layoutManager.ScrollTo(pos);
        }

        public void ScrollTo(int position)
        {
            layoutManager.ScrollTo(position);

        }

        public void SmothScrollTo(int position)
        {
            layoutManager.SmothScrollTo(position);
        }


        private class Pool
        {
            
            int poolSize, cacheSize;
            public Pool(int poolSize, int cacheSize)
            {
                this.poolSize = poolSize;
                this.cacheSize = cacheSize;
            }
            public List<IViewHolderInfo> Scrap = new List<IViewHolderInfo>();


            public bool IsFull()
            {
                return Scrap.Count >= poolSize;
            }

            public IViewHolderInfo GetFromPool(int position, bool recycle = false)
            {
                foreach (IViewHolderInfo vh in Scrap)
                {
                    if (vh.CurrentIndex == position)
                    {
                        Scrap.Remove(vh);
                        return vh;
                    }
                }
                if (recycle)
                {
                    IViewHolderInfo vh2 = Scrap.Count > 0 ? Scrap[0] : null; //TODO coger por antiguedad
                    if (vh2 != null)
                    {
                        Scrap.Remove(vh2);
                    }
                    return vh2;
                }
                else
                {
                    return null;
                }
            }


            public void Add(IViewHolderInfo vh)
            {
                if (Scrap.Count < poolSize)
                {
                    vh.Status = ViewHolder.Status.RECYCLED;
                    Scrap.Add(vh);
                }
                else
                {
                    vh.Status = ViewHolder.Status.RECYCLED;
                    Scrap.Add(vh);
                    Scrap.RemoveAt(0);
                }
            }


        }


        private class LayoutManager
        {
            private Orientation orientation;
            private float rowHeight;
            private Vector2 RowDimension;
            private ScrollRect ScrollRect;
            private RectTransform SelfRectTransform { get; set; }
            private RectTransform GridRectTransform { get; set; }
            private GameObject Grid;
            private float LIMIT_BOTTOM = 0;
            public bool IsCreating = false;
            private bool isDraging, isClickDown;
             
            private RecyclerView<T> recyclerView;

            public LayoutManager(RecyclerView<T> recyclerView, Orientation orientation)
            {
                this.recyclerView = recyclerView;
                this.orientation = orientation;
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

            public void ReorderList()
            {
                List<IViewHolderInfo> vhs = new List<IViewHolderInfo>();
                vhs.AddRange(recyclerView.cacheBot);
                vhs.AddRange(recyclerView.cacheTop);
                vhs.AddRange(recyclerView.attachedScrap);
                foreach (IViewHolderInfo vh in vhs)
                {
                    if (vh.Status != ViewHolder.Status.RECYCLED)
                    {
                        if (IsVerticalOrientation())
                        {
                            if (recyclerView.IsReverse)
                            {
                                vh.RectTransform.localPosition = new Vector3(0, (vh.CurrentIndex * (RowDimension.y + recyclerView.Spacing.y)), 0);
                            }
                            else
                            {
                                vh.RectTransform.localPosition = new Vector3(0, (-vh.CurrentIndex * (RowDimension.y + recyclerView.Spacing.y)), 0);
                            }
                        }
                        else
                        {
                            if (recyclerView.IsReverse)
                            {
                                vh.RectTransform.localPosition = new Vector3((-vh.CurrentIndex * (RowDimension.x + recyclerView.Spacing.x)), 0, 0);
                            }
                            else
                            {
                                vh.RectTransform.localPosition = new Vector3((vh.CurrentIndex * (RowDimension.x + recyclerView.Spacing.x)), 0, 0);
                            }
                        }
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
                            ScrollTo(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            ScrollTo(0);
                        }
                    }
                    else
                    {
                        if (GridRectTransform.offsetMax.y > LIMIT_BOTTOM)
                        {
                            ScrollTo(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            ScrollTo(0);
                        }
                    }
                }
                else
                {
                    if (recyclerView.IsReverse)
                    {
                        if (GridRectTransform.offsetMax.x > LIMIT_BOTTOM)
                        {
                            ScrollTo(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            ScrollTo(0);
                        }
                    }
                    else
                    {
                        if (GridRectTransform.offsetMax.x < -LIMIT_BOTTOM)
                        {
                            ScrollTo(recyclerView.GetItemCount() - 1);
                        }
                        else
                        {
                            ScrollTo(0);
                        }
                    }
                }
                Debug.Log("MODEL IS INVALID");
                Debug.Log(GridRectTransform.offsetMax);
            }

            private void OnScroll()
            {
                if (!IsCreating)
                {
                    if (IsStateValid())
                    {
                        ClampList();
                        recyclerView.OnScroll();
                        ReorderList();
                    }
                    else
                    {
                        Invalidate();
                    }

                }
            }

            public int OnDataChange(GameObject initialVH, int pos = 0)
            {
                RowDimension = new Vector2(initialVH.GetComponent<RectTransform>().rect.width, initialVH.GetComponent<RectTransform>().rect.height);
                int InitialSize = 0;
                if (IsVerticalOrientation())
                {
                    LIMIT_BOTTOM = ((recyclerView.GetItemCount() * (RowDimension.y + recyclerView.Spacing.y)) - SelfRectTransform.rect.height) - recyclerView.Spacing.y;
                    InitialSize = Mathf.FloorToInt(SelfRectTransform.rect.height / (RowDimension.y / 2)); //TODO calcular
                    if (recyclerView.IsReverse)
                    {
                 //       GridRectTransform.localPosition = new Vector2(GridRectTransform.localPosition.x, -(RowDimension.y + recyclerView.Spacing.y) * pos);
                        GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, -(RowDimension.y + recyclerView.Spacing.y) * pos);
                        GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                    }
                    else
                    {
                //       GridRectTransform.localPosition = new Vector2(GridRectTransform.localPosition.x, (RowDimension.y + recyclerView.Spacing.y) * pos);
                        GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, (RowDimension.y + recyclerView.Spacing.y) * pos);
                        GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                    }

                }
                else
                {
                    LIMIT_BOTTOM = ((recyclerView.GetItemCount() * (RowDimension.x + recyclerView.Spacing.x)) - SelfRectTransform.rect.width) - recyclerView.Spacing.x;
                    InitialSize = Mathf.FloorToInt(SelfRectTransform.rect.width / (RowDimension.x / 2)); //TODO calcular


                    if (recyclerView.IsReverse)
                    {
                        GridRectTransform.offsetMax = new Vector2((RowDimension.x + recyclerView.Spacing.x) * pos, GridRectTransform.offsetMax.y);
                        GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                    }
                    else
                    {
                        GridRectTransform.localPosition = new Vector2(-(RowDimension.x + recyclerView.Spacing.x) * pos, GridRectTransform.localPosition.y);
                        GridRectTransform.offsetMax = new Vector2(-(RowDimension.x + recyclerView.Spacing.x) * pos, GridRectTransform.offsetMax.y);
                        GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                    }

                }
                return InitialSize;
            }

            private IEnumerator IScrollTo(Vector2 dir, float speed = 100)
            {
                ScrollRect.inertia = false;
                if (IsVerticalOrientation())
                {
                    Vector2 v = new Vector2(0, dir.y * LIMIT_BOTTOM);
                    bool goUp = GridRectTransform.offsetMax.y > v.y;
                    float y = GridRectTransform.offsetMax.y;
                    while (goUp ? GridRectTransform.offsetMax.y > v.y : GridRectTransform.offsetMax.y < v.y)
                    {
                        if (isClickDown)
                        {
                            break;
                        }

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

                    }
                }
                else
                {
                    Vector2 v = new Vector2(dir.x * LIMIT_BOTTOM, 0);
                    bool goUp = GridRectTransform.offsetMax.x > v.x;
                    float y = GridRectTransform.offsetMax.x;
                    while (goUp ? GridRectTransform.offsetMax.x > v.x : GridRectTransform.offsetMax.x < v.x)
                    {
                        if (isClickDown)
                        {
                            break;
                        }

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
                    recyclerView.StartCoroutine(IScrollTo(new Vector2(0, ((RowDimension.y + recyclerView.Spacing.y) * position) / LIMIT_BOTTOM)));
                }
                else
                {
                    recyclerView.StartCoroutine(IScrollTo(new Vector2((((RowDimension.x + recyclerView.Spacing.x) * position) / LIMIT_BOTTOM), 0)));
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




            public void AttachToGrid(IViewHolderInfo vh, bool attachTop)
            {
                vh.ItemView.transform.SetParent(Grid.transform);
                if (attachTop)
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
            }



            private bool IsStateValid()
            {
                foreach (IViewHolderInfo vh in recyclerView.attachedScrap)
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

        private static class Utils
        {

            public static void Sort(List<IViewHolderInfo> list, bool upperFirst)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        if (upperFirst)
                        {
                            if (list[i].CurrentIndex > list[j].CurrentIndex)
                            {
                                IViewHolderInfo aux = list[i];
                                list[i] = list[j];
                                list[j] = aux;
                            }
                        }
                        else
                        {
                            if (list[i].CurrentIndex < list[j].CurrentIndex)
                            {
                                IViewHolderInfo aux = list[i];
                                list[i] = list[j];
                                list[j] = aux;
                            }
                        }
                    }
                }
            }
            public static int GetLowerPosition(List<IViewHolderInfo> list)
            {
                int lower = int.MaxValue;
                foreach (IViewHolderInfo scrap in list)
                {
                    if (scrap.CurrentIndex < lower)
                    {
                        lower = scrap.CurrentIndex;
                    }
                }
                return lower != int.MaxValue ? lower : -1;
            }

            public static int GetUpperPosition(List<IViewHolderInfo> list)
            {
                int upper = -1;
                foreach (IViewHolderInfo scrap in list)
                {
                    if (scrap.CurrentIndex > upper)
                    {
                        upper = scrap.CurrentIndex;
                    }
                }
                return upper;
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
                CACHE_TOP,
                CACHE_BOT,
                RECYCLED

            }

        }

        public abstract class Adapter : RecyclerView<T>, IDataObservable
        {
            public void NotifyDatasetChanged()
            {
                OnDataChange();
            }
        }

        private interface IAdapter
        {
            GameObject OnCreateViewHolder(Transform parent);
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
                        "    public override GameObject OnCreateViewHolder(Transform parent)\n" +
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
        // Necessary since some properties tend to collapse smaller than their content
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        // Draw a disabled property field
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = !Application.isPlaying;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
}
