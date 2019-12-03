using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExampleAdapter : UI.RecyclerView<ExampleAdapter.Holder>.Adapter
{
    public List<Sprite> sprites;
    public int scrollTo = 15;
    public int smoothScrollTo = 10;
    public Vector2 scrollyBy = new Vector2(0, 0.5f);

    private List<Item> list = new List<Item>();
    public GameObject row;

    public void Start()
    {

        foreach (Sprite sprite in sprites)
        {
            Item item = new Item();
            item.Image = sprite;
            item.Name = sprite.name;
            list.Add(item);
        }
        

        NotifyDatasetChanged();
    }


    public override int GetItemCount()
    {
        
        return list.Count;
    }

    public override void OnBindViewHolder(Holder holder, int i)
    {
        holder.text.text = list[i].Name;
        holder.image.sprite = list[i].Image;
        holder.button.onClick.RemoveAllListeners();
        holder.button.onClick.AddListener(delegate ()
        {
            Debug.Log(list[i].Name);
        });
    }

    public override GameObject OnCreateViewHolder()
    {
        return Instantiate(row);
    }

    private class Item
    {
        public string Name;
        public Sprite Image;
    }

    public Vector3 GetGridPosition(GameObject obj){
        GameObject objAux = new GameObject();
        objAux.transform.position = obj.transform.position;
        return objAux.transform.position;
    }

    public class Holder : ViewHolder
    {
        public Text text;
        public Image image;
        public Button button;

        public Holder(GameObject itemView) : base(itemView)
        {
            text = itemView.transform.Find("Text").GetComponent<Text>();
            image = itemView.transform.Find("Image").GetComponent<Image>();
            button = itemView.transform.Find("Button").GetComponent<Button>();           
        }
    }


    void OnGUI()
    {


        if (GUI.Button(new Rect(10, 70, 150, 30), "Smoth Scroll To " + smoothScrollTo))
        {
           SmothScrollTo(smoothScrollTo);
        }

        if (GUI.Button(new Rect(10, 110, 150, 30), "Scroll To " + scrollTo))
        {
            ScrollTo(scrollTo);
        }

        if (GUI.Button(new Rect(10, 150, 150, 30), "Scroll By " + scrollyBy))
        {
            ScrollBy(scrollyBy);
        }
    }


}
