using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUI.ListView
{
    /// <summary>
    /// 页面管理器
    /// </summary>
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class PageDotManager : MonoBehaviour
    {
        #region Public Methods
        /// <summary>
        /// 刷新所有点
        /// </summary>
        public void RefreshDots()
        {
            UpdateAllDots();
        }
        #endregion

        #region Internal Methods
        private void Awake()
        {
            if (listView == null)
            {
                Debug.LogError("滑动列表不能为空");

                return;
            }

            if (template != null && template.activeInHierarchy)
            {
                template.SetActive(false);
            }

            for (int i = 0; i < initCount; i++)
            {
                CreateDot();
            }

            cacheItemCount = initCount;
            listView.OnSnapNearestChanged += OnSnapNearestChanged;
        }

        private void Update()
        {
            if (autoRefresh)
            {
                checkDotsDelta += Time.deltaTime;
                if (checkDotsDelta > 0.3f)
                {
                    checkDotsDelta = 0;
                    if (cacheItemCount != listView.TotalCount)
                    {
                        RefreshDots();
                    }
                }
            }
        }

        /// <summary>
        /// 创建点
        /// </summary>
        protected virtual void CreateDot()
        {
            if (template == null)
            {
                Debug.LogError("模板不能为空");
            }

            GameObject go = Instantiate(template, transform);
            PageDotEventTrigger trigger = go.AddComponent<PageDotEventTrigger>();
            trigger.PageDotManager = this;

            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(false);

            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                Debug.LogError("找不到图片组件");
            }
            else
            {
                dotList.Add(img);
            }
        }

        private void OnSnapNearestChanged(ScrollItem item)
        {
            UpdateAllDots();
        }

        protected virtual void UpdateAllDots()
        {
            var index = listView.SnapNearestItemIndex;
            if (index >= 0)
            {
                index %= listView.TotalCount;
            }
            else
            {
                index = listView.TotalCount +
                        ((index + 1) % listView.TotalCount) - 1;
            }

            if (cacheItemCount != listView.TotalCount)
            {
                cacheItemCount = listView.TotalCount;
                int offset = cacheItemCount - dotList.Count;
                for (int i = 0; i < offset; i++)
                {
                    CreateDot();
                }
            }

            for (int i = 0; i < dotList.Count; i++)
            {
                Image img = dotList[i];
                GameObject go = img.gameObject;
                go.name = i.ToString();
                if (i < cacheItemCount)
                {
                    
                    if (i == index)
                    {
                        img.sprite = highlightSprite;
                        img.color = highlightColor;
                    }
                    else
                    {
                        img.sprite = normalSprite;
                        img.color = normalColor;
                    }

                    go.SetActive(true);
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        internal void OnPointClicked(int index)
        {
            if (!clickNavigation)
            {
                return;
            }

            int curNearestItemIndex = listView.TotalCount;
            if (curNearestItemIndex < 0 || curNearestItemIndex >= listView.TotalCount)
            {
                return;
            }

            if (index == curNearestItemIndex)
            {
                return;
            }

            listView.ScrollGrid(index > curNearestItemIndex);
        }
        #endregion

        #region Internal Metghods
        [SerializeField]
        private ListView listView = null;
        [SerializeField]
        private int initCount = 0;
        [SerializeField]
        private bool clickNavigation = true;
        [SerializeField]
        private bool autoRefresh = false;
        [SerializeField]
        private GameObject template = null;
        [SerializeField]
        private Sprite normalSprite = null;
        [SerializeField]
        private Color normalColor = Color.white;
        [SerializeField]
        private Sprite highlightSprite = null;
        [SerializeField]
        private Color highlightColor = Color.white;

        private List<Image> dotList = new List<Image>();
        private int cacheItemCount;
        private float checkDotsDelta;

        #endregion
    }

    public class PageDotEventTrigger : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        #region Public Properties
        public PageDotManager PageDotManager { get; internal set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            int.TryParse(name, out var index);
            PageDotManager.OnPointClicked(index);
        }

        public void OnPointerDown(PointerEventData eventData)
        {

        }
        #endregion
    }
}
