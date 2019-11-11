using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RecyclerView
{
    public abstract class RecyclerView<T> : MonoBehaviour, IAdapter<T>
        where T : ViewHolder
    {
        [Range(0, 1f)]
        public float decelerationRate = 0.5f;
        public Orientation orientation;
        public Vector2 Spacing;
        public bool IsReverse;
        private int POOL_SIZE = 10;
        private int CACHE_SIZE = 3;

        private Pool pool;
        private List<ViewHolder> attachedScrap = new List<ViewHolder>();
        private List<ViewHolder> cacheTop = new List<ViewHolder>();
        private List<ViewHolder> cacheBot = new List<ViewHolder>();


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

        private ViewHolder GetViewHolderFromScrap(int position)
        {
            foreach (ViewHolder vh in attachedScrap)
            {
                if (vh.current_index == position)
                {
                    return vh;
                }
            }
            return null;
        }

        private void AddToAttachedScrap(ViewHolder vh, bool attachTop)
        {
            layoutManager.AttachToGrid(vh, attachTop);         
            vh.itemView.SetActive(true);
            attachedScrap.Add(vh);
        }  

        private ViewHolder GetFromCache(int i, bool top)
        {
            if (top)
            {
                foreach (ViewHolder vh in cacheTop)
                {
                    if (vh.current_index == i)
                    {
                        cacheTop.Remove(vh);
                        return vh;
                    }
                }
            }
            else
            {
                foreach (ViewHolder vh in cacheBot)
                {
                    if (vh.current_index == i)
                    {
                        cacheBot.Remove(vh);
                        return vh;
                    }
                }
            }
            return null;
        }

        private ViewHolder TryGetViewHolderForPosition(int position)
        {
            if (position >= 0 && position < GetItemCount())
            {
                ViewHolder botCache = GetFromCache(position, false);
                if (botCache != null)
                {
                    botCache.status = ViewHolder.Status.CACHE;
                    botCache.current_index = position;
                    botCache.itemView.name = position.ToString();

                    return botCache;
                }
                ViewHolder topCache = GetFromCache(position, true);
                if (topCache != null)
                {
                    topCache.status = ViewHolder.Status.CACHE;
                    topCache.current_index = position;
                    topCache.itemView.name = position.ToString();

                    return topCache;
                }
                ViewHolder vhrecycled;
                vhrecycled = pool.GetFromPool(position);
                if (vhrecycled != null)
                {
                    vhrecycled.status = ViewHolder.Status.SCRAP;
                    vhrecycled.last_index = vhrecycled.current_index;
                    vhrecycled.current_index = position;
                    return vhrecycled;
                }

                if (pool.IsFull())
                {
                    vhrecycled = pool.GetFromPool(position, true);
                    vhrecycled.status = ViewHolder.Status.SCRAP;
                    vhrecycled.last_index = vhrecycled.current_index;
                    vhrecycled.current_index = position;
                    return vhrecycled;

                }
                else
                {
                    ViewHolder vh = (ViewHolder)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });

                    vh.current_index = position;
                    vh.last_index = position;
                    vh.status = ViewHolder.Status.SCRAP;

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
            foreach (ViewHolder scrap in attachedScrap)
            {
                if (scrap.current_index < lower)
                {
                    lower = scrap.current_index;
                }
            }
            return lower;
        }

        private int GetUpperPosition()
        {
            int upper = 0;
            foreach (ViewHolder scrap in attachedScrap)
            {
                if (scrap.current_index > upper)
                {
                    upper = scrap.current_index;
                }
            }
            return upper;
        }

        private int GetLowerChild()
        {
            int lower = int.MaxValue;
            foreach (ViewHolder scrap in attachedScrap)
            {
                if (scrap.itemView.transform.GetSiblingIndex() < lower)
                {
                    lower = scrap.itemView.transform.GetSiblingIndex();
                }
            }
            return lower;
        }


        private int GetUpperChild()
        {
            int upper = 0;
            foreach (ViewHolder scrap in attachedScrap)
            {
                if (scrap.itemView.transform.GetSiblingIndex() > upper)
                {
                    upper = scrap.itemView.transform.GetSiblingIndex();
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
            foreach (ViewHolder vh in attachedScrap)
            {
                str += vh.current_index + ",";
            }
            str += "} Cache Top: {";
            foreach (ViewHolder vh in cacheTop)
            {
                str += vh.current_index + ",";
            }
            str += "} Cache Bot: {";
            foreach (ViewHolder vh in cacheBot)
            {
                str += vh.current_index + ",";
            }
            str += "} Pool: {";
            foreach (ViewHolder vh in pool.Scrap)
            {
                str += vh.current_index + ",";
            }
            str += "}";
            return str;
        }

        private void AddNewViewHoldersToCache(bool top)
        {
            if (top)
            {
                int nTop = CACHE_SIZE - cacheTop.Count;
                for (int i = 0; i < nTop; i++)
                {
                    ViewHolder vh = TryGetViewHolderForPosition(Utils.GetUpperPosition(cacheTop.Count > 0 ? cacheTop : attachedScrap) + 1);
                    if (vh != null)
                    {
                        layoutManager.AttachToGrid(vh, top);
                        ThrowToCache(vh, true);
                        OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.current_index);
                    }
                }
            }
            else
            {
                int nBot = CACHE_SIZE - cacheBot.Count;
                for (int i = 0; i < nBot; i++)
                {
                    ViewHolder vh = TryGetViewHolderForPosition(Utils.GetLowerPosition(cacheBot.Count > 0 ? cacheBot : attachedScrap) - 1);
                    if (vh != null)
                    {
                        layoutManager.AttachToGrid(vh, top);
                        ThrowToCache(vh, false);
                        OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.current_index);
                    }
                }
            }
        }

        private void ThrowToPool(ViewHolder vh)
        {
            if (pool.IsFull())
            {
                vh.Destroy();
            }
            else
            {
                vh.status = ViewHolder.Status.RECYCLED;
                vh.itemView.SetActive(false);
                pool.Add(vh);
            }
        }



        private void ThrowToCache(ViewHolder viewHolder, bool top)
        {
            viewHolder.status = ViewHolder.Status.CACHE;
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
                if (cacheTop.Count > CACHE_SIZE)
                {
                    for (int i = cacheTop.Count - 1; i >= CACHE_SIZE; i--)
                    {
                        ThrowToPool(cacheTop[i]);
                        cacheTop.RemoveAt(i);
                    }
                }
            }
            else
            {
                Utils.Sort(cacheBot, false);
                if (cacheBot.Count > CACHE_SIZE)
                {
                    for (int i = cacheBot.Count - 1; i >= CACHE_SIZE; i--)
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

            pool = new Pool(POOL_SIZE, CACHE_SIZE);

            if (GetItemCount() > 0)
            {
                ViewHolder vh = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                vh.current_index = pos;
                vh.last_index = pos;
                vh.status = ViewHolder.Status.SCRAP;
                AddToAttachedScrap(vh, true);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), pos);

                int ATTACHED_SCRAP_SIZE = layoutManager.OnDataChange(vh.itemView, pos);
               
                for (int i = pos + 1; i < ATTACHED_SCRAP_SIZE + pos; i++)
                {
                    if (i < GetItemCount())
                    {
                        ViewHolder vh2 = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                        vh2.current_index = i;
                        vh2.last_index = i;
                        vh2.status = ViewHolder.Status.SCRAP;
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
            public List<ViewHolder> Scrap = new List<ViewHolder>();


            public bool IsFull()
            {
                return Scrap.Count >= poolSize;
            }

            public ViewHolder GetFromPool(int position, bool recycle = false)
            {
                foreach (ViewHolder vh in Scrap)
                {
                    if (vh.current_index == position)
                    {
                        Scrap.Remove(vh);
                        return vh;
                    }
                }
                if (recycle)
                {
                    ViewHolder vh2 = Scrap.Count > 0 ? Scrap[0] : null; //TODO coger por antiguedad
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


            public void Add(ViewHolder vh)
            {
                if (Scrap.Count < poolSize)
                {
                    vh.status = ViewHolder.Status.RECYCLED;
                    Scrap.Add(vh);
                }
                else
                {
                    vh.status = ViewHolder.Status.RECYCLED;
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
                List<ViewHolder> vhs = new List<ViewHolder>();
                vhs.AddRange(recyclerView.cacheBot);
                vhs.AddRange(recyclerView.cacheTop);
                vhs.AddRange(recyclerView.attachedScrap);
                foreach (ViewHolder vh in vhs)
                {
                    if (vh.status != ViewHolder.Status.RECYCLED)
                    {
                        if (IsVerticalOrientation())
                        {
                            if (recyclerView.IsReverse)
                            {
                                vh.rectTransform.localPosition = new Vector3(0, (vh.current_index * (RowDimension.y + recyclerView.Spacing.y)), 0);
                            }
                            else
                            {
                                vh.rectTransform.localPosition = new Vector3(0, (-vh.current_index * (RowDimension.y + recyclerView.Spacing.y)), 0);
                            }
                        }
                        else
                        {
                            if (recyclerView.IsReverse)
                            {
                                vh.rectTransform.localPosition = new Vector3((-vh.current_index * (RowDimension.x + recyclerView.Spacing.x)), 0, 0);
                            }
                            else
                            {
                                vh.rectTransform.localPosition = new Vector3((vh.current_index * (RowDimension.x + recyclerView.Spacing.x)), 0, 0);
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




            public void AttachToGrid(ViewHolder vh, bool attachTop)
            {
                vh.itemView.transform.SetParent(Grid.transform);
                if (attachTop)
                {
                    vh.itemView.transform.SetAsLastSibling();
                }
                else
                {
                    vh.itemView.transform.SetAsFirstSibling();
                }
                vh.itemView.name = vh.current_index.ToString();
                vh.itemView.SetActive(true);
                SetPivot(vh.rectTransform);
            }



            private bool IsStateValid()
            {
                foreach (ViewHolder vh in recyclerView.attachedScrap)
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

            public static void Sort(List<ViewHolder> list, bool upperFirst)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        if (upperFirst)
                        {
                            if (list[i].current_index > list[j].current_index)
                            {
                                ViewHolder aux = list[i];
                                list[i] = list[j];
                                list[j] = aux;
                            }
                        }
                        else
                        {
                            if (list[i].current_index < list[j].current_index)
                            {
                                ViewHolder aux = list[i];
                                list[i] = list[j];
                                list[j] = aux;
                            }
                        }
                    }
                }
            }
            public static int GetLowerPosition(List<ViewHolder> list)
            {
                int lower = int.MaxValue;
                foreach (ViewHolder scrap in list)
                {
                    if (scrap.current_index < lower)
                    {
                        lower = scrap.current_index;
                    }
                }
                return lower != int.MaxValue ? lower : -1;
            }

            public static int GetUpperPosition(List<ViewHolder> list)
            {
                int upper = -1;
                foreach (ViewHolder scrap in list)
                {
                    if (scrap.current_index > upper)
                    {
                        upper = scrap.current_index;
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
    }

    public abstract class Adapter<T> : RecyclerView<T>, IDataObservable
    where T : ViewHolder
    {
        public void NotifyDatasetChanged()
        {
            OnDataChange();
        }
    }

    public abstract class ViewHolder
    {
        public GameObject itemView;
        public RectTransform rectTransform;

        public int last_index, current_index;

        public Status status;



        public ViewHolder(GameObject itemView)
        {
            this.itemView = itemView;
            this.rectTransform = itemView.GetComponent<RectTransform>();

        }

        public void Destroy()
        {
            GameObject.Destroy(itemView);
        }

        public bool IsHidden()
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

        public int CompareTo(ViewHolder vh)
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

        public enum Status
        {
            SCRAP,
            CACHE,
            CACHE_TOP,
            CACHE_BOT,
            RECYCLED

        }
    }


    public interface IAdapter<in T> where T : ViewHolder
    {
        GameObject OnCreateViewHolder(Transform parent);
        void OnBindViewHolder(T holder, int i);
        int GetItemCount();
    }

    public interface IDataObservable
    {

        void NotifyDatasetChanged();

    }

    public interface IRecyclerView
    {
        void ScrollTo(Vector2 pos);
        void ScrollTo(int position);
        void SmothScrollTo(int position);

    }

}
