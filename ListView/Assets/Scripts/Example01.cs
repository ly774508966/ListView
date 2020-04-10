using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UGUI;
using Random = UnityEngine.Random;

public class Example01 : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 0;

        foreach (var rect in scrollables)
        {
            rect.FillItemData = (item, i) =>
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

            rect.MoveStateChanged += StateChanged;
            rect.GetWidthOrHeight = i => 220;
            rect.FillCells();
        }
    }

    private void StateChanged(ScrollableRect.MoveState state)
    {
        if (state == ScrollableRect.MoveState.MoveComplete)
        {
            index++;
            if (index == 5)
            {
                index = 0;
                moveComplete = true;
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && moveComplete)
        {
            StartCoroutine(Scroll());
        }
    }

    private IEnumerator Scroll()
    {
        moveComplete = false;
        yield return null;
        foreach (var scrollable in scrollables)
        {
            scrollable.FillCells();
            scrollable.ScrollToView(30, true, 3000);

            yield return new WaitForSeconds(0.05f);
        }
    }

    [SerializeField]
    private List<ScrollableRect> scrollables;
    private int index;
    private bool moveComplete = true;
}