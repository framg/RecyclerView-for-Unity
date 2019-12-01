# Recycler View for Unity

This is an untiy adaption for the current recycler view base on Android.
[Recycler View Official Documentation](https://developer.android.com/reference/android/support/v7/widget/RecyclerView)

## What is RecyclerView?

When we are working on a phone we usually don't want to keep too much memory in the heap. A large element list could cause severe errors and performance problems. In order to fix this isssue we using a Recycler View or a list with pooling.
During the scroll we are going to keep just a small list instead of the full data set, improving a lot the performance.


## How to use the ReyclerView?

We have two options:

###### Option 1

Right click on the hierarchy.
And press UI/RecyclerView

![Image 1](https://github.com/framg/RecyclerView/blob/master/Images/image1.PNG)

A new dialog box will display. Choose your name and press "Create".

![Image 1](https://github.com/framg/RecyclerView/blob/master/Images/image2.png)

Once you added it, you'll see a template file like this one:

```
using UnityEngine;
using System.Collections;

public class RecyclerView : UI.RecyclerView<TestRecyclerView.Holder>.Adapter {

    public override int GetItemCount()
    {
        throw new System.NotImplementedException();
    }

    public override void OnBindViewHolder(Holder holder, int i)
    {
        throw new System.NotImplementedException();
    }

    public override GameObject OnCreateViewHolder(Transform parent)
    {
        throw new System.NotImplementedException();
    }

    public class Holder : ViewHolder
    {
        public Holder(GameObject itemView) : base(itemView)
        {
        }
    }
}
```

