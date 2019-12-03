# Recycler View for Unity

This is an untiy adaption for the current recycler view base on Android.
[Recycler View Official Documentation](https://developer.android.com/reference/android/support/v7/widget/RecyclerView)

## What is RecyclerView?

When we are working on a phone we usually don't want to keep too much memory in the heap. A large element list could cause severe errors and performance problems. In order to fix this isssue we using a Recycler View or a list with pooling.
During the scroll we are going to keep just a small list instead of the full data set, improving a lot the performance.


## How to create the ReyclerView?

We have two options:

### Option 1

Right click on the hierarchy.
And press UI/RecyclerView

![Image 1](https://github.com/framg/RecyclerView/blob/master/Images/image1.PNG)

A new dialog box will display. Choose your name and press "Create".

![Image 2](https://github.com/framg/RecyclerView/blob/master/Images/image2.png)

Once you added it, you'll see a template file like this one:

```
using UnityEngine;
using System.Collections;

public class RecyclerView : UI.RecyclerView<RecyclerView.Holder>.Adapter {

    public override int GetItemCount()
    {
        throw new System.NotImplementedException();
    }

    public override void OnBindViewHolder(Holder holder, int i)
    {
        throw new System.NotImplementedException();
    }

    public override GameObject OnCreateViewHolder()
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

### Option 2

Just create the class and extend it to UI.RecyclerView<ViewHolder>.Adapter. 
ViewHolder needs to extend from the class ViewHolder insde of RecyclerView.
    
    
## How to use the ReyclerView?

You can check the ExampleAdapter I've created for this tutorial.
In order to use it we have to complete following the methods:


GetItemCount needs to return the number of items in your list.
```
public override int GetItemCount()
```

OnCreateViewHolder must return a copy from your row. (You could use Instantiate(GameObject) for expample)
```
public override GameObject OnCreateViewHolder()
```

OnBindViewHolder is going to bind our holder. So in here you need to populate the row.
```
public override void OnBindViewHolder(Holder holder, int i)
```

## Properties

 - **Orientation**: We can modify the list orientation. (Vertical or Horizontal)
 - **Spacing**: Space between rows.
 - **IsReverse**: Set the list in reverse order.
 - **decelerationRate**: Change the deceletarion rate of the scrolling.
 - **PoolSize**: The size of the pool. (If you are not sure about it don't overwrite it)
 - **CacheSize**: The size of the cache. (If you are not sure about it don't overwrite it)

## Public Methods

### Adapter
 
 - **NotifyDatasetChanged**: You must call this method after the data set was modified.
 - **ScrollBy**: Scroll by a given float value from 0 to 1 where 0 is the beggining and 1 is the end.
 - **ScrollTo**: Scroll to the given position.
 - **SmothScrollTo**: Smoth scroll to the given position.

### ViewHolder

 - **GetAdapterPosition**: Get the current position for that view holder.

## Unity inspector

![Image 3](https://github.com/framg/RecyclerView/blob/master/Images/image3.PNG)


 
 










