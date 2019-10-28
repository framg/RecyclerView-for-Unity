using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace RecyclerView
{
    public interface IRecyclerView
    {
        void ScrollTo(Vector2 pos);
        void ScrollTo(int position);
        void SmothScrollTo(int position);

    }
}
