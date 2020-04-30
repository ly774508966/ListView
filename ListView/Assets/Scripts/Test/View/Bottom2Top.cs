using System.Collections.Generic;
using UnityEngine.UI;

namespace UGUI.Test.View
{
    public class Bottom2Top : DemoBase
    {
        protected override void FillData(ScrollItem item, int index)
        {
            if (!cacheDict.TryGetValue(item, out var elements))
            {
                elements = item.GetComponent<UIElements>();
                cacheDict.Add(item, elements);
            }
            
            var image = elements.GetObject<Image>("Icon");
            var itemName = elements.GetObject<Text>("Name");
            var desc = elements.GetObject<Text>("Desc");
            var desc2 = elements.GetObject<Text>("Desc2");

            if (image != null) image.sprite = GetSprite();
            itemName.text = index.ToString();
        }

        private Dictionary<ScrollItem, UIElements> cacheDict = new Dictionary<ScrollItem, UIElements>();
    }
}