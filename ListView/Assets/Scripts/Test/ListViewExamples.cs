using UGUI;
using UnityEngine;
using UnityEngine.UI;

public class ListViewExamples : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 0;

        scrollable = GetComponent<ScrollableRect>();
        scrollable.FillItemData = (item, i) =>
        {
            int rand = Random.Range(1, 13);
            string iconName;
            if (rand < 10)
            {
                iconName = "fruit_0" + rand;
            }
            else
            {
                iconName = "fruit_" + rand;
            }

            Sprite sprite = Resources.Load<Sprite>(iconName);
            if (sprite)
            {
                item.transform.Find("Item").GetComponent<Image>().sprite = sprite;
            }
        };

        scrollable.FillCells();
    }

    private ScrollableRect scrollable;
}