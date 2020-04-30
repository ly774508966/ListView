using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UGUI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class ScrollRectTest : MonoBehaviour
{
    void Start()
    {
        backButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(Init.initSceneName, LoadSceneMode.Single);
        });
        startBtn.onClick.AddListener(() =>
        {
            StartCoroutine(Scroll());
        });
        Application.targetFrameRate = 0;

        foreach (var rect in scrollList)
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
                    item.transform.Find("Icon").GetComponent<Image>().sprite = sprite;
                    item.transform.Find("Name").GetComponent<Text>().text = i.ToString();
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

    private IEnumerator Scroll()
    {
        moveComplete = false;
        yield return null;
        foreach (var scrollable in scrollList)
        {
            scrollable.FillCells();
            scrollable.ScrollToView(30, true, 3000);

            yield return new WaitForSeconds(0.05f);
        }
    }

    [SerializeField]
    private List<ScrollableRect> scrollList = null;
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private Button startBtn;
    private int index;
    private bool moveComplete = true;
}