using Pool;
using UnityEngine;
using UnityEngine.UI;

namespace UGUI.ListView
{
    /// <summary>
    /// 滚动槽元素
    /// </summary>
    public class ScrollItem : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// 当前索引
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// 自定义数据
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// 所属滑动列表
        /// </summary>
        public ScrollableRect Parent { get; internal set; }

        /// <summary>
        /// RectTransform
        /// </summary>
        public RectTransform RectTrans
        {
            get
            {
                if (rect == null && gameObject)
                {
                    rect = transform as RectTransform;
                }

                return rect;
            }
        }

        /// <summary>
        /// 当前项高度
        /// </summary>
        public float Height
        {
            get
            {
                if (element == null && gameObject)
                {
                    element = GetComponent<LayoutElement>();
                }

                return element.preferredHeight;
            }

            set
            {
                if (element == null && gameObject)
                {
                    element = GetComponent<LayoutElement>();
                }

                element.preferredHeight = value;
            }
        }

        /// <summary>
        /// 当前项宽度
        /// </summary>
        public float Width
        {
            get
            {
                if (gameObject && element == null)
                {
                    element = GetComponent<LayoutElement>();
                }

                return element.preferredWidth;
            }

            set
            {
                if (element == null)
                {
                    element = GetComponent<LayoutElement>();
                }

                element.preferredWidth = value;
            }
        }

        /// <summary>
        /// 所属对象池
        /// </summary>
        public ObjectPool<GameObject> Pool { get; internal set; }

        /// <summary>
        /// 格子大小
        /// </summary>
        public float ItemSize
        {
            get
            {
                if (Parent.ScrollDirection == ScrollableRect.Direction.TopToBottom ||
                    Parent.ScrollDirection == ScrollableRect.Direction.BottomToTop)
                {
                    return RectTrans.rect.height;
                }

                return RectTrans.rect.width;
            }
        }

        /// <summary>
        /// 格子本身加上间隔大小
        /// </summary>
        public float ItemSizeWithSpacing => ItemSize + Spacing;

        /// <summary>
        /// 间隔大小
        /// </summary>
        public float Spacing { get; internal set; }
        #endregion

        #region Internal Methods
        /// <summary>
        /// 回收自己
        /// </summary>
        internal void Recycle()
        {
            if (gameObject)
            {
                Pool?.Recycle(gameObject);
            }
        }
        #endregion

        #region Internal Fields
        private RectTransform rect;
        private LayoutElement element;
        #endregion
    }
}