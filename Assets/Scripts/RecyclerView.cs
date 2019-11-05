using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RecyclerView
{
    public abstract class RecyclerView<T> : MonoBehaviour, IAdapter<T>, IRecyclerView
        where T : ViewHolder
    {

        public Orientation orientation;
        public Vector2 Spacing;
        public bool IsReverse;
        //  public float spacingY;
        // private float rowHeight;
        private Vector2 RowDimension;
        private ScrollRect ScrollRect;
        private RectTransform SelfRectTransform { get; set; }
        private RectTransform GridRectTransform { get; set; }
        private GameObject Grid;
        private int POOL_SIZE = 10;
        private int CACHE_SIZE = 3;
        private float LIMIT_BOTTOM = 0;
      
        private bool isDraging, isClickDown;

        private Pool pool;
        private List<ViewHolder> attachedScrap = new List<ViewHolder>();
        private List<ViewHolder> cacheTop = new List<ViewHolder>();
        private List<ViewHolder> cacheBot = new List<ViewHolder>();


        public abstract GameObject OnCreateViewHolder(Transform parent);
        public abstract void OnBindViewHolder(T holder, int i);   
        public abstract int GetItemCount();


        public void Awake()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            Create();

            OnDataChange();
        }

        private void Create()
        {

            SelfRectTransform = GetComponent<RectTransform>();
            Grid = new GameObject();
            Grid.name = "Grid";
            GridRectTransform = Grid.AddComponent<RectTransform>();
            GridRectTransform.sizeDelta = Vector2.zero;

            if (IsVerticalOrientation())
            {
                GridRectTransform.anchorMax = new Vector2(0.5f, 1f);
                GridRectTransform.anchorMin = new Vector2(0.5f, 1f);
                GridRectTransform.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                if (IsReverse)
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

            Grid.transform.SetParent(transform);
            GridRectTransform.anchoredPosition = Vector3.zero;


            ScrollRect = GetComponent<ScrollRect>();
            if (ScrollRect == null)
            {
                ScrollRect = gameObject.AddComponent<ScrollRect>();
            }       
            ScrollRect.content = GridRectTransform;
            ScrollRect.onValueChanged.AddListener(delegate { OnScroll(); });
            ScrollRect.viewport = SelfRectTransform;
            ScrollRect.content = GridRectTransform;
            ScrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            ScrollRect.inertia = true;
            ScrollRect.decelerationRate = 0.5f;
            ScrollRect.scrollSensitivity = 10f;
            ScrollRect.vertical = IsVerticalOrientation();
            ScrollRect.horizontal = !IsVerticalOrientation();

            if (GetComponent<Image>() == null)
            {
                Image image = gameObject.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0.01f);
            }
            if (GetComponent<Mask>() == null)
            {
                gameObject.AddComponent<Mask>();
            }

            if (gameObject.GetComponent<EventTrigger>() == null)
            {
                EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
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
            if (IsVerticalOrientation())
            {
                vh.rectTransform.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                if (IsReverse)
                {
                    vh.rectTransform.pivot = new Vector2(1f, 0.5f);
                }
                else
                {
                    vh.rectTransform.pivot = new Vector2(0f, 0.5f);
                }
            }
            vh.itemView.SetActive(true);
            attachedScrap.Add(vh);
        }

        private bool IsVerticalOrientation()
        {
            return orientation == Orientation.VERTICAL;
        }

        private void ReorderList()
        {
            List<ViewHolder> vhs = new List<ViewHolder>();
            vhs.AddRange(cacheBot);
            vhs.AddRange(cacheTop);
            vhs.AddRange(attachedScrap);
            foreach (ViewHolder vh in vhs)
            {
                if (vh.status != Status.RECYCLED)
                {
                    if (IsVerticalOrientation())
                    {
                        vh.rectTransform.localPosition = new Vector3(0, (-vh.current_index * (RowDimension.y + Spacing.y)), 0);
                    }
                    else
                    {
                        if (IsReverse)
                        {
                            vh.rectTransform.localPosition = new Vector3((-vh.current_index * (RowDimension.x + Spacing.x)), 0, 0);
                        }
                        else
                        {
                            vh.rectTransform.localPosition = new Vector3((vh.current_index * (RowDimension.x + Spacing.x)), 0, 0);
                        }
                    }
                }
            }

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
                    botCache.status = Status.CACHE;
                    botCache.current_index = position;
                    botCache.itemView.name = position.ToString();

                    return botCache;
                }
                ViewHolder topCache = GetFromCache(position, true);
                if (topCache != null)
                {
                    topCache.status = Status.CACHE;
                    topCache.current_index = position;
                    topCache.itemView.name = position.ToString();

                    return topCache;
                }
                ViewHolder vhrecycled;
                vhrecycled = pool.GetFromPool(position);
                if (vhrecycled != null)
                {
                    vhrecycled.status = Status.SCRAP;
                    vhrecycled.last_index = vhrecycled.current_index;
                    vhrecycled.current_index = position;
                    return vhrecycled;
                }

                if (pool.IsFull())
                {
                    vhrecycled = pool.GetFromPool(position, true);
                    vhrecycled.status = Status.SCRAP;
                    vhrecycled.last_index = vhrecycled.current_index;
                    vhrecycled.current_index = position;
                    return vhrecycled;

                }
                else
                {
                    ViewHolder vh = (ViewHolder)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });

                    vh.current_index = position;
                    vh.last_index = position;
                    vh.status = Status.SCRAP;

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


        private void ClampList()
        {
            if (IsVerticalOrientation())
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
            else
            {
                
                if (IsReverse)
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
                    //Debug.Log(GridRectTransform.offsetMax.ToString() + " AND " + GridRectTransform.sizeDelta.ToString());
                    if (GridRectTransform.offsetMax.x > 0)
                    {
                       //Debug.Log(GridRectTransform.localPosition);
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


        private void Snap()
        {
          //  int pos = Mathf.FloorToInt(GridRectTransform.offsetMax.y / (rowHeight + spacingY));
       //     GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, (rowHeight + spacingY) * pos);
          //  GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
        }



        private void OnDrag() {
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

        private void OnScroll()
        {
            if (IsStateValid())
            {
                ClampList();
                RemoveNotVisibleViewHolders();
                RemoveViewHoldersFromCache(true);
                RemoveViewHoldersFromCache(false);
                AddNewViewHoldersToCache(true);
                AddNewViewHoldersToCache(false);
                ReorderList();
            }
            else
            {
                Invalidate();
            }

            //if (Mathf.Abs(ScrollRect.velocity.y) < 100 && !isDraging)
            //{
            //    Snap();
            //}
            // Debug.Log(ScrollRect.velocity);
        }

        private void Invalidate()
        {
            Debug.Log("INVALIDATE");
            if (IsVerticalOrientation())
            {

            }
            else
            {
                if (IsReverse)
                {

                }
                else
                {
                    if (GridRectTransform.offsetMax.x < -LIMIT_BOTTOM)
                    {
                        ScrollTo(GetItemCount() - 1);
                    }
                    else
                    {
                        ScrollTo(0);
                    }
                }
            }
           // Debug.Log("MODEL IS INVALID");
          //  Debug.Log(GridRectTransform.offsetMax);
        }

        private bool IsStateValid()
        {
            foreach(ViewHolder vh in attachedScrap)
            {
                if (!vh.IsHidden())
                {
                    return true;
                }
            }

            return false;
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
                    ViewHolder vh = TryGetViewHolderForPosition(GetUpperPosition(cacheTop.Count > 0 ? cacheTop : attachedScrap) + 1);
                    if (vh != null)
                    {
                        vh.itemView.transform.SetParent(Grid.transform);
                        vh.itemView.transform.SetAsLastSibling();
                        vh.itemView.name = vh.current_index.ToString();
                        if (IsVerticalOrientation())
                        {
                            vh.rectTransform.pivot = new Vector2(0.5f, 1f);
                        }
                        else
                        {
                            if (IsReverse)
                            {
                                vh.rectTransform.pivot = new Vector2(1f, 0.5f);
                            }
                            else
                            {
                                vh.rectTransform.pivot = new Vector2(0f, 0.5f);
                            }
                        }
                        vh.itemView.SetActive(true);

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
                    ViewHolder vh = TryGetViewHolderForPosition(GetLowerPosition(cacheBot.Count > 0 ? cacheBot : attachedScrap) - 1);
                    if (vh != null)
                    {
                        vh.itemView.transform.SetParent(Grid.transform);
                        vh.itemView.transform.SetAsFirstSibling();
                        vh.itemView.name = vh.current_index.ToString();
                        if (IsVerticalOrientation())
                        {
                            vh.rectTransform.pivot = new Vector2(0.5f, 1f);
                        }
                        else
                        {
                            if (IsReverse)
                            {
                                vh.rectTransform.pivot = new Vector2(1f, 0.5f);
                            }
                            else
                            {
                                vh.rectTransform.pivot = new Vector2(0f, 0.5f);
                            }
                        }
                        vh.itemView.SetActive(true);
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
                vh.status = Status.RECYCLED;
                if (IsVerticalOrientation())
                {
                    vh.rectTransform.pivot = new Vector2(0.5f, 1f);
                }
                else
                {
                    if (IsReverse)
                    {
                        vh.rectTransform.pivot = new Vector2(1f, 0.5f);
                    }
                    else
                    {
                        vh.rectTransform.pivot = new Vector2(0f, 0.5f);
                    }
                }
                vh.itemView.SetActive(false);
                pool.Add(vh);
            }
        }



        private void ThrowToCache(ViewHolder viewHolder, bool top)
        {
            viewHolder.status = Status.CACHE;
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

            Sort(attachedScrap, true);

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

            Sort(attachedScrap, false);

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
                Sort(cacheTop, true);
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
                Sort(cacheBot, false);
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

        private void Sort(List<ViewHolder> list, bool upperFirst)
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
        private int GetLowerPosition(List<ViewHolder> list)
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

        private int GetUpperPosition(List<ViewHolder> list)
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
        private void Clear()
        {
            foreach (Transform row in Grid.transform)
            {
                Destroy(row.gameObject);
            }

            attachedScrap.Clear();
            pool = null;

            cacheBot.Clear();
            cacheTop.Clear();

        }

        private IEnumerator INotifyDatasetChanged(int pos = 0)
        {
            //float size;
            //if (IsVerticalOrientation())
            //{
            //    size = ((RowDimension.y + Spacing.y) * pos);
            //}
            //else
            //{
            //    size = ((RowDimension.x + Spacing.x) * pos);
            //}

            ScrollRect.inertia = false;
            OnDataChange(pos);
            yield return new WaitForEndOfFrame();
            OnScroll();
            ScrollRect.inertia = true;
        }

        private IEnumerator IScrollTo(Vector2 dir, float speed = 100)
        {
           // Debug.Log(dir + " " +speed);

            Vector2 v = new Vector2(0, dir.y * LIMIT_BOTTOM);
            ScrollRect.inertia = false;
            bool goUp = GridRectTransform.offsetMax.y > v.y;
            float y = GridRectTransform.offsetMax.y;
            while (goUp ? GridRectTransform.offsetMax.y > v.y : GridRectTransform.offsetMax.y < v.y)
            {
                if (isClickDown)
                {
                    break;
                }

                y += goUp ? -speed : speed;
                
                if(y > LIMIT_BOTTOM)
                {
                  //  break;
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
            ScrollRect.inertia = true;
        }

        public void ScrollTo(Vector2 pos)
        {
            StartCoroutine(IScrollTo(pos));
        }

        public void ScrollTo(int position)
        {
            StartCoroutine(INotifyDatasetChanged(position));

        }

        public void SmothScrollTo(int position)
        {
            if (IsVerticalOrientation())
            {
                StartCoroutine(IScrollTo(new Vector2(0, ((RowDimension.y + Spacing.y) * position) / LIMIT_BOTTOM)));
            }
            else
            {
                //TODO
            }
        }

        protected void OnDataChange(int pos = 0)
        {
            if (pos < 0 || pos > GetItemCount())
            {
                return;
            }
         //   Debug.Log("HEY");


            Clear();

            pool = new Pool(POOL_SIZE, CACHE_SIZE);

            if (GetItemCount() > 0)
            {
                int ATTACHED_SCRAP_SIZE = 0;
                ViewHolder vh = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                vh.current_index = pos;
                vh.last_index = pos;
                vh.status = Status.SCRAP;
                AddToAttachedScrap(vh, true);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), pos);
                RowDimension = new Vector2(vh.rectTransform.rect.width, vh.rectTransform.rect.height);

                if (IsVerticalOrientation())
                {                  
                    LIMIT_BOTTOM = ((GetItemCount() * (RowDimension.y + Spacing.y)) - SelfRectTransform.rect.height) - Spacing.y;

                    GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, (RowDimension.y + Spacing.y) * pos);
                    GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);

                    ATTACHED_SCRAP_SIZE = Mathf.FloorToInt(SelfRectTransform.rect.height / (RowDimension.y / 2)); //TODO calcular
                }
                else
                {
                    LIMIT_BOTTOM = ((GetItemCount() * (RowDimension.x + Spacing.x)) - SelfRectTransform.rect.width) - Spacing.x;
                    if (IsReverse)
                    {
                        GridRectTransform.offsetMax = new Vector2((RowDimension.x + Spacing.x) * pos, GridRectTransform.offsetMax.y);
                        GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                    }
                    else
                    {
                        GridRectTransform.localPosition = new Vector2(-(RowDimension.x + Spacing.x) * pos, GridRectTransform.localPosition.y);
                        GridRectTransform.offsetMax = new Vector2(-(RowDimension.x + Spacing.x) * pos, GridRectTransform.offsetMax.y);
                        GridRectTransform.sizeDelta = new Vector2(0, GridRectTransform.sizeDelta.y);
                    }
                    ATTACHED_SCRAP_SIZE = Mathf.FloorToInt(SelfRectTransform.rect.width / (RowDimension.x / 2)); //TODO calcular
                }

               
                for (int i = pos + 1; i < ATTACHED_SCRAP_SIZE + pos; i++)
                {
                    if (i < GetItemCount())
                    {
                        ViewHolder vh2 = (T)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder(transform) });
                        vh2.current_index = i;
                        vh2.last_index = i;
                        vh2.status = Status.SCRAP;
                        AddToAttachedScrap(vh2, true);
                        OnBindViewHolder((T)Convert.ChangeType(vh2, typeof(T)), i);
                    }
                }

               // OnScroll();

        
                ReorderList();
            }
        }

        public enum Orientation
        {
            VERTICAL,
            HORIZONTAL
        }

        public class Pool
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
                    vh.status = Status.RECYCLED;
                    Scrap.Add(vh);
                }
                else
                {
                    vh.status = Status.RECYCLED;
                    Scrap.Add(vh);
                    Scrap.RemoveAt(0);
                }
            }


        }

    }

}
