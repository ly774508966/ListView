using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UGUI.Test.View
{
    public class Top2Bottom : DemoBase
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
            var starCount = elements.GetObject<Text>("StarCount");

            if (image != null) image.sprite = GetSprite();
            itemName.text = index.ToString();
            starCount.text = Random.Range(0, 5).ToString();
        }

        private Dictionary<ScrollItem, UIElements> cacheDict = new Dictionary<ScrollItem, UIElements>();
    }
}