# Recycler View for Unity

This is an untiy adaption for the current recycler view base on Android.
[Recycler View Official Documentation](https://developer.android.com/reference/android/support/v7/widget/RecyclerView)

## What is RecyclerView?

When we are working on a phone we usually don't want to keep too much memory in the heap. A large element list could cause severe errors and performance problems. In order to fix this isssue we using a Recycler View or a list with pooling.
During the scroll we are going to keep just a small list instead of the full data set, improving a lot the performance.


## How to create the ReyclerView?

We have two options:

###### Option 1

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

###### Option 2

Just create the class and extend it to UI.RecyclerView<ViewHolder>.Adapter. 
ViewHolder needs to extend from the class ViewHolder insde of RecyclerView.
    
    
## How to use the ReyclerView?

In order to use it we have to complete following the methods:


GetItemCount needs to return the number of items in your list.
```
public override int GetItemCount()
```

OnCreateViewHolder must return a copy from your row. (You could use Instantiate(GameObject) for expample)
```
public override GameObject OnCreateViewHolder(Transform parent)
```

OnBindViewHolder is going to bind our holder. So in here you need to populate the row.
```
public override void OnBindViewHolder(Holder holder, int i)
```

## Unity inspector.

![Image 3](https://github.com/framg/RecyclerView/blob/master/Images/image3.PNG)

 - We can modify the list orientation. (Vertical or Horizontal)
 - Space between rows.
 - Set the list in reverse order.
 - Change the deceletarion rate of the scrolling.
 - And at last change the cache size or pool size. If you are not sure about it don't do it.







