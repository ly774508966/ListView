using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UGUI;

namespace UGUI
{
    /// <summary>
    /// 可循环的活动列表列表，复用Item
    /// </summary>
    public class ListView : ScrollableRect,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IScrollHandler, 
        ILayoutGroup, 
        IInitializePotentialDragHandler
    {
        #region Properties
        /// <summary>
        /// 下拉过顶或者上拉过底时允许超过的距离占显示区域的比例
        /// </summary>
        public float RubberScale
        {
            get => rubberScale;
            set => rubberScale = value;
        }

        /// <summary>
        /// 弹力,只是针对于Movement == MovementType.Elastic有效
        /// </summary>
        public float Elasticity
        {
            get => elasticity;
            set => elasticity = value;
        }

        /// <summary>
        /// 是否开启惯性拖动 开启时 当拖拽结束时会继续移动直到速度减为0为止,
        /// 减速大小由DecelerationRate和SlowDownCoefficient来决定 否则当松开手指时即停止移动
        /// </summary>
        public bool Inertia
        {
            get => inertia;
            set => inertia = value;
        }

        /// <summary>
        /// 移动类型
        /// </summary>
        public MovementType Movement
        {
            get => movementType;
            set => movementType = value;
        }

        /// <summary>
        /// 滚动条
        /// </summary>
        public Scrollbar Scrollbar
        {
            get => Horizontal ? horizontalScrollbar : verticalScrollbar;

            set
            {
                //如果是横向
                if (Horizontal)
                {
                    if (horizontalScrollbar != null)
                    {
                        var sEvent = horizontalScrollbar.onValueChanged;
                        sEvent.RemoveListener(SetHorizontalNormalizedPosition);
                    }

                    horizontalScrollbar = value;
                    if (horizontalScrollbar != null)
                    {
                        var sEvent = horizontalScrollbar.onValueChanged;
                        sEvent.AddListener(SetHorizontalNormalizedPosition);
                    }
                }
                //如果是纵向
                else
                {
                    if (verticalScrollbar != null)
                    {
                        var sEvent = verticalScrollbar.onValueChanged;
                        sEvent.RemoveListener(SetVerticalNormalizedPosition);
                    }

                    verticalScrollbar = value;
                    if (verticalScrollbar != null)
                    {
                        var sEvent = verticalScrollbar.onValueChanged;
                        sEvent.AddListener(SetVerticalNormalizedPosition);
                    }
                }

                SetDirtyCaching();
            }
        }

        /// <summary>
        /// 滚动条可见性
        /// </summary>
        public ScrollbarVisibility ScrollbarVisible
        {
            get=> Horizontal ? horizontalScrollbarVisibility : 
                verticalScrollbarVisibility;

            set
            {
                if (Horizontal)
                {
                    horizontalScrollbarVisibility = value;
                }
                else
                {
                    verticalScrollbarVisibility = value;
                }

                SetDirtyCaching();
            }
        }

        /// <summary>
        /// 当前拖动位置比例
        /// </summary>
        public float NormalizedPosition
        {
            get
            {
                EnsureLayoutHasRebuilt();
                UpdateBounds();
                if (Horizontal)
                {
                    if (!Loop && ItemEnd > ItemStart)
                    {
                        var elementSize = contentBounds.size.x / (ItemEnd - ItemStart);
                        var totalSize = elementSize * TotalCount;
                        var offset = contentBounds.min.x - elementSize * ItemStart;

                        if (totalSize <= viewBounds.size.x)
                        {
                            return viewBounds.min.x > offset ? 1 : 0;
                        }

                        return (viewBounds.min.x - offset) / (totalSize - viewBounds.size.x);
                    }

                    return 0.5f;
                }

                if (!Loop && ItemEnd > ItemStart)
                {
                    float elementSize = contentBounds.size.y / (ItemEnd - ItemStart);
                    float totalSize = elementSize * TotalCount;
                    float offset = contentBounds.max.y + elementSize * ItemStart;

                    if (totalSize <= viewBounds.size.y)
                    {
                        return offset > viewBounds.max.y ? 1 : 0;
                    }

                    return (offset - viewBounds.max.y) / (totalSize - viewBounds.size.y);
                }

                return 0.5f;
            }

            set => SetNormalizedPosition(value, Horizontal ? 0 : 1);
        }

        /// <summary>
        /// 当前快速移动索引
        /// </summary>
        public int SnapNearestItemIndex { get; private set; }

        /// <summary>
        /// 是否开启拖拽移动 当处于禁用状态时 拖拽事件也会触发但Content并不会移动
        /// </summary>
        public bool EnableDragMove
        {
            get => enableDragMove;
            set => enableDragMove = value;
        }
        #endregion

        #region Events
        /// <summary>
        /// 开始拖拽事件
        /// </summary>
        public Action<PointerEventData> OnBeginDrag;

        /// <summary>
        /// 拖拽中事件
        /// </summary>
        public Action<PointerEventData> OnDragging;

        /// <summary>
        /// 结束拖拽事件
        /// </summary>
        public Action<PointerEventData> OnEndDrag;

        /// <summary>
        /// 滚动进度发生改变事件
        /// </summary>
        public ScrollRectEvent OnValueChanged => onValueChanged;

        /// <summary>
        /// 当快速动画完成时事件
        /// </summary>
        public Action<ScrollItem> OnSnapFinished;

        /// <summary>
        /// 当快速移动索引改变时
        /// </summary>
        public Action<ScrollItem> OnSnapNearestChanged;

        /// <summary>
        /// 自定义减速过程
        /// <para>T1: 当前速度</para>
        /// <para>T2: 减速速率</para>
        /// <para>T3: 减速倍数</para>
        /// <para>T4: 减速后的值</para>
        /// </summary>
        public Func<float, float, float, float> CustomDeceleration;
        #endregion

        #region Methods
        /// <summary>
        /// 移动一个单元格
        /// </summary>
        /// <param name="add">增加或者减小</param>
        /// <param name="immediately">是否立即完成</param>
        public void ScrollGrid(bool add, bool immediately = false)
        {
            if (SnapNearestItemIndex == TotalCount && !Loop)
            {
                return;
            }

            //更新当前移动索引
            currentSnapData.CurSnapValue = 0;
            if (add)
            {
                ++SnapNearestItemIndex;
            }
            else
            {
                --SnapNearestItemIndex;
            }

            //获取需要移动的大小
            float value;
            if (GetWidthOrHeight != null)
            {
                value = GetWidthOrHeight.Invoke(SnapNearestItemIndex) + Spacing;
            }
            else
            { 
                value = GetFirstShow().ItemSizeWithSpacing;
            }

            //开始移动
            currentSnapData.TargetSnapValue = add ? -value - 1 : value + 1;
            currentSnapData.IsMoving = true;
            currentSnapData.IsNearestChange = true;
            if (immediately)
            {
                UpdateSnapMove(true);
            }
        }

        public override void Rebuild(CanvasUpdate executing)
        {
            base.Rebuild(executing);

            //在网格计算之前
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateSliderData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();
            }
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// 更新开始和上一次的内容区域位置
        /// </summary>
        private void UpdateCachePos(float size, bool add)
        {
            Vector2 offset = GetVector(size);
            if (add)
            {
                contentStartPosition += offset;
                prevPosition += offset;
            }
            else
            {
                contentStartPosition -= offset;
                prevPosition -= offset;
            }
        }
           
        /// <summary>
        /// 在开始处添加一个新项
        /// </summary>
        /// <returns>新项的大小</returns>
        protected override float NewItemAtStart()
        {
            if (!Loop && ItemStart - CellCount < 0)
            {
                return 0;
            }

            var size = base.NewItemAtStart();
            if (!ReverseDirection)
            {
                UpdateCachePos(size, true);
            }

            return size;
        }
        
        /// <summary>
        /// 在开始初删除一个项
        /// </summary>
        /// <returns>删除项的大小</returns>
        protected override float DeleteItemAtStart()
        {
            if (((dragging || velocity != Vector2.zero) && ItemEnd >= TotalCount - 1 && !Loop) || childrenList.Count == 0)
            {
                return 0;
            }

            var size = base.DeleteItemAtStart();
            if (!ReverseDirection)
            {
                UpdateCachePos(size, false);
            }

            return size;
        }

        /// <summary>
        /// 在结尾处添加一个新项
        /// </summary>
        /// <returns>新项的大小</returns>
        protected override float NewItemAtEnd()
        {
            if (!Loop && ItemEnd >= TotalCount)
            {
                return 0;
            }

            var size = base.NewItemAtEnd();
            if (ReverseDirection)
            {
                UpdateCachePos(size, false);
            }

            return size;
        }

        /// <summary>
        /// 在结尾处删除一个项
        /// </summary>
        /// <returns>删除项的大小</returns>
        protected override float DeleteItemAtEnd()
        {
            if (((dragging || velocity != Vector2.zero) && !Loop && ItemStart < CellCount) || childrenList.Count == 0)
            {
                return 0;
            }

            var size = base.DeleteItemAtEnd();
            if (ReverseDirection)
            {
                UpdateCachePos(size, true);
            }

            return size;
        }

        protected override void Awake()
        {
            base.Awake();

            if (Scrollbar)
            {
                switch (ScrollDirection)
                {
                    case Direction.LeftToRight:
                        Scrollbar.direction = Scrollbar.Direction.LeftToRight;

                        break;
                    case Direction.RightToLeft:
                        Scrollbar.direction = Scrollbar.Direction.RightToLeft;

                        break;
                    case Direction.TopToBottom:
                        Scrollbar.direction = Scrollbar.Direction.TopToBottom;

                        break;
                    case Direction.BottomToTop:
                        Scrollbar.direction = Scrollbar.Direction.BottomToTop;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (horizontalScrollbar)
            {
                horizontalScrollbar.onValueChanged.AddListener(
                    SetHorizontalNormalizedPosition);
            }

            if (verticalScrollbar)
            {
                verticalScrollbar.onValueChanged.AddListener(
                    SetVerticalNormalizedPosition);
            }

            SetDirty();
        }

        protected override void OnDisable()
        {
            StopAllCoroutines();
            if (horizontalScrollbar)
            {
                horizontalScrollbar.onValueChanged.RemoveListener(
                    SetHorizontalNormalizedPosition);
            }

            if (verticalScrollbar)
            {
                verticalScrollbar.onValueChanged.RemoveListener(
                    SetVerticalNormalizedPosition);
            }

            tracker.Clear();
            velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);

            base.OnDisable();
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            velocity = Vector2.zero;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!IsActive())
            {
                return;
            }

            if (!enableDragMove)
            {
                OnBeginDrag?.Invoke(eventData);

                return;
            }

            CustomMove = false;
            UpdateBounds();
            pointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ViewRect,
                eventData.position,
                eventData.pressEventCamera,
                out pointerStartLocalCursor);
            contentStartPosition = Content.anchoredPosition;

            dragging = true;
            updateSnap = false;
            currentSnapData.Stop();
            OnBeginDrag?.Invoke(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!IsActive())
            {
                return;
            }

            if (!enableDragMove)
            {
                OnDragging?.Invoke(eventData);

                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                ViewRect,
                eventData.position,
                eventData.pressEventCamera,
                out var localCursor))
            {
                return;
            }

            UpdateBounds();
            var pointerDelta = localCursor - pointerStartLocalCursor;
            Vector2 position = contentStartPosition + pointerDelta;

            // 根据鼠标拖动的距离计算需要运动的偏移值
            Vector2 offset = CalculateOffset(position - Content.anchoredPosition);
            position += offset;
            if (movementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                {
                    position.x -= RubberDelta(
                                     offset.x, viewBounds.size.x) * rubberScale;
                }

                if (offset.y != 0)
                {
                    position.y -= RubberDelta(
                                     offset.y, viewBounds.size.y) * rubberScale;
                }
            }
            else if (movementType == MovementType.Clamped)
            {
                var axis = Horizontal ? pointerDelta[0] : pointerDelta[1];
                if ((ItemStart == 0 && axis < 0) || (ItemEnd == TotalCount && axis > 0))
                {
                    return;
                }

                if (offset.x != 0)
                {
                    position.x -= RubberDelta(
                                      offset.x, viewBounds.size.x) * rubberScale;
                }

                if (offset.y != 0)
                {
                    position.y -= RubberDelta(
                                      offset.y, viewBounds.size.y) * rubberScale;
                }
            }
            
            //设置位置
            SetContentPosition(position);
            OnDragging?.Invoke(eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!enableDragMove)
            {
                OnEndDrag?.Invoke(eventData);

                return;
            }

            var speed = Mathf.Abs(Speed);
            if (speed < snapVelocityThreshold)
            {
                updateSnap = true;
            }

            dragging = false;
            OnEndDrag?.Invoke(eventData);
        }

        /// <summary>
        /// 当鼠标中键滚动时
        /// </summary>
        /// <param name="data">滑动数据</param>
        void IScrollHandler.OnScroll(PointerEventData data)
        {
            if (!IsActive())
            {
                return;
            }

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            //计算偏移
            Vector2 delta = data.scrollDelta;
            delta.y *= -1;
            if (Horizontal)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                {
                    delta.x = delta.y;
                }

                delta.y = 0;
                
            }
            else
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    delta.y = delta.x;
                }

                delta.x = 0;
            }

            //计算需要滚动的距离
            Vector2 position = Content.anchoredPosition;
            position += delta * scrollSensitivity;
            if (movementType == MovementType.Clamped)
            {
                position += CalculateOffset(position - Content.anchoredPosition);
            }

            //设置位置
            SetContentPosition(position);
        }

        void ILayoutController.SetLayoutHorizontal()
        {
            tracker.Clear();
            if (hSliderExpand || vSliderExpand)
            {
                //添加禁止在Inspector中修改ViewRect的Anchors, SizeDelta, AnchoredPosition属性
                tracker.Add(this, ViewRect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                // 设置视图大小为最大
                ViewRect.anchorMin = Vector2.zero;
                ViewRect.anchorMax = Vector2.one;
                ViewRect.sizeDelta = Vector2.zero;
                ViewRect.anchoredPosition = Vector2.zero;

                // 使用此大小重新计算内容布局，以查看在没有滚动条时它是否适合。
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
                viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                contentBounds = GetBounds();
            }

            //如果垂直方向需要伸展，启用垂直滚动条并计算其位置。
            if (vSliderExpand && VScrollingNeeded)
            {
                ViewRect.sizeDelta = new Vector2(-vSliderWidth, ViewRect.sizeDelta.y);

                // 重新计算内容布局，确定是否显示垂直滚动条
                // 当有一个垂直滚动条（可以重新转换内容以使其更高）。
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
                viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                contentBounds = GetBounds();
            }

            // 如果水平方向需要伸展，启用水平滚动条并计算其位置。
            if (hSliderExpand && HScrollingNeeded)
            {
                ViewRect.sizeDelta = new Vector2(ViewRect.sizeDelta.x, -hSliderHeight);
                viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                contentBounds = GetBounds();
            }

            //如果垂直和水平都没有显示，需要再次检查垂直滑块是否需要显示。
            //如果需要显示，启用垂直滚动条并计算其位置。
            if (vSliderExpand &&
                VScrollingNeeded &&
                ViewRect.sizeDelta.x == 0 && ViewRect.sizeDelta.y < 0)
            {
                ViewRect.sizeDelta = new Vector2(-vSliderWidth, ViewRect.sizeDelta.y);
            }
        }

        void ILayoutController.SetLayoutVertical()
        {
            UpdateScrollbarLayout();
            viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
            contentBounds = GetBounds();
        }

        private float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - 1 / (Mathf.Abs(overStretching) * 0.55f / viewSize + 1))
                   * viewSize * Mathf.Sign(overStretching);
        }

        /// <summary>
        /// 固定更新事件
        /// </summary>
        protected override void LateUpdate()
        {
            if (CustomMove)
            {
                base.LateUpdate();

                return;
            }

            if (!EnableDragMove)
            {
                UpdateSnapMove();

                return;
            }

            EnsureLayoutHasRebuilt();
            UpdateSnapMove();
            UpdateScrollbarVisibility();
            UpdateBounds();
            var offset = CalculateOffset(Vector2.zero);
            var deltaTime = Time.smoothDeltaTime;
            if (!dragging && (offset != Vector2.zero || velocity != Vector2.zero))
            {
                var position = Content.anchoredPosition;
                int axis = Horizontal ? 0 : 1;
                //当运动类型是弹性的并且内容在视图中有偏移量，则应用弹性移动
                if (movementType == MovementType.Elastic && 
                    Mathf.Abs(offset[axis]) > 1 && 
                    (ItemStart == 0 || ItemEnd == TotalCount))
                {
                    float speed = 0;
                    position[axis] = Mathf.SmoothDamp(
                        Content.anchoredPosition[axis],
                        Content.anchoredPosition[axis] + offset[axis],
                        ref speed,
                        elasticity,
                        Mathf.Infinity, deltaTime);

                    if (Mathf.Abs(speed) < 10)
                    {
                        speed = 0;
                    }

                    velocity[axis] = speed;
                }
                // 根据移动速度大小做减速运动
                else if (inertia)
                {
                    var speed = velocity[axis];
                    if (CustomDeceleration != null)
                    {
                        lastVelocity[axis] = speed;
                        velocity[axis] = CustomDeceleration.Invoke(
                            speed, DecelerationRate, SlowDownCoefficient);
                    }
                    else
                    {
                        lastVelocity[axis] = speed;
                        if (speed > 20000)
                        {
                            velocity[axis] *= Mathf.Pow(DecelerationRate, deltaTime);
                        }
                        else if (speed > 5000)
                        {
                            velocity[axis] *= Mathf.Pow(DecelerationRate, deltaTime * 2);
                        }
                        else
                        {
                            velocity[axis] *= Mathf.Pow(DecelerationRate, deltaTime * 4);
                        }
                    }

                    if (Mathf.Abs(velocity[axis]) < 100)
                    {
                        lastVelocity[axis] = 0;
                        velocity[axis] = 0;
                    }

                    position[axis] += velocity[axis] * deltaTime;
                }
                else  //如果没有弹力也没有摩擦力, 则速度为0
                {
                    lastVelocity[axis] = 0;
                    velocity[axis] = 0;
                }

                if (movementType == MovementType.Clamped)
                {
                    offset = CalculateOffset(position - Content.anchoredPosition);
                    position += offset;
                }

                SetContentPosition(position);
            }

            if (dragging && inertia)
            {
                Vector3 newVelocity = (Content.anchoredPosition - prevPosition) / deltaTime;
                velocity = Vector3.Lerp(velocity, newVelocity, 0.8f);
                lastVelocity = velocity;
            }

            if (viewBounds != prevViewBounds ||
                contentBounds != prevContentBounds ||
                Content.anchoredPosition != prevPosition)
            {
                UpdateScrollbars(offset);
                onValueChanged.Invoke(NormalizedPosition);
                UpdatePrevData();
            }
        }

        private void UpdatePrevData()
        {
            prevPosition = Content.anchoredPosition;
            prevViewBounds = viewBounds;
            prevContentBounds = contentBounds;
        }

        /// <summary>
        /// 设置水平位置比例
        /// </summary>
        /// <param name="value">比例值, 在0-1之间</param>
        private void SetHorizontalNormalizedPosition(float value)
        {
            SetNormalizedPosition(value, 0);
        }

        /// <summary>
        /// 设置垂直位置比例
        /// </summary>
        /// <param name="value">比例值, 在0-1之间</param>
        private void SetVerticalNormalizedPosition(float value)
        {
            SetNormalizedPosition(value, 1);
        }

        /// <summary>
        /// 设置位置比例
        /// </summary>
        /// <param name="value">比例值, 在0-1之间</param>
        /// <param name="axis">轴向 0代表X轴 1代表Y轴</param>
        private void SetNormalizedPosition(float value, int axis)
        {
            if (Loop || ItemEnd <= ItemStart)
            {
                return;
            }

            value = !ReverseDirection ? value : 1 - value;
            //更新区域大小
            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector3 localPosition = Content.localPosition;
            var newLocalPosition = localPosition[axis];
            var elementSize = 1f;
            //如果是横向位置
            if (axis == 0)
            {
                elementSize = contentBounds.size.x / (ItemEnd - ItemStart);
                var totalSize = elementSize * TotalCount;
                var offset = contentBounds.min.x - elementSize * ItemStart;

                newLocalPosition += viewBounds.min.x -
                                    value * (totalSize - viewBounds.size[axis]) - offset;
            }
            //如果是纵向位置
            else if (axis == 1)
            {
                elementSize = contentBounds.size.y / (ItemEnd - ItemStart);
                var totalSize = elementSize * TotalCount;
                var offset = contentBounds.max.y + elementSize * ItemStart;

                newLocalPosition -= offset - value
                                    * (totalSize - viewBounds.size.y) - viewBounds.max.y;
            }

            //更新位置
            var offsetPos = Mathf.Abs(localPosition[axis] - newLocalPosition);
            var moveDir = Horizontal ? localPosition[axis] - newLocalPosition > 0 : newLocalPosition - localPosition[axis] > 0;
            if (offsetPos > 0.05f && (value > 0 && value < 1))
            {
                // 在这里 直接估计出当前位置 进行刷新不进行 移动操作
                velocity = Vector2.zero;
                if (offsetPos > 1000)
                {
                    int count = (int)(offsetPos / elementSize);
                    count = moveDir ? count + 1 : -count - 1;
                    var countValue = ReverseDirection ? TotalCount - (ItemEnd + count) : ItemStart + count;

                    FillCells(Mathf.Clamp(countValue, 0, TotalCount));
                }
                else
                {
                    localPosition[axis] = newLocalPosition;
                    SetContentPosition(localPosition);
                }
            }
        }

        /// <summary>
        /// 计算视图区域与内容区域的间隔
        /// </summary>
        /// <param name="delta">偏移值</param>
        /// <returns></returns>
        protected internal override Vector2 CalculateOffset(Vector2 delta)
        {
            if (movementType == MovementType.Unrestricted)
            {
                return Vector2.zero;
            }

            return base.CalculateOffset(delta);
        }

        protected void SetDirtyCaching()
        {
            if (!IsActive())
            {
                return;
            }

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Override to alter or add to the code that keeps the appearance of the scroll rect synced with its data.
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        /// <summary>
        /// 更新滚动条数据
        /// </summary>
        private void UpdateSliderData()
        {
            Transform trans = transform;
            hBarRect = horizontalScrollbar == null ? null :
                horizontalScrollbar.transform as RectTransform;
            vBarRect = verticalScrollbar == null ? null :
                verticalScrollbar.transform as RectTransform;

            //确定水平和垂直滚动条是否为子元素
            bool viewIsChild = ViewRect.parent == trans;
            bool hScrollbarIsChild =
                !hBarRect || hBarRect.parent == trans;
            bool vScrollbarIsChild =
                !vBarRect || vBarRect.parent == trans;
            bool allAreChildren = viewIsChild && hScrollbarIsChild && vScrollbarIsChild;

            //滚动条是否需要展开
            hSliderExpand = allAreChildren &&
                            hBarRect &&
                            ScrollbarVisible ==
                            ScrollbarVisibility.AutoHideAndExpandViewport;
            vSliderExpand = allAreChildren &&
                            vBarRect &&
                            ScrollbarVisible ==
                            ScrollbarVisibility.AutoHideAndExpandViewport;
            hSliderHeight = hBarRect == null ? 0 :
                hBarRect.rect.height;
            vSliderWidth = vBarRect == null ? 0 :
                vBarRect.rect.width;
        }

        /// <summary>
        /// 更新滑动条size和value
        /// </summary>
        /// <param name="offset"></param>
        private void UpdateScrollbars(Vector2 offset)
        {
            if (Loop)
            {
                if (Horizontal && horizontalScrollbar)
                {
                    horizontalScrollbar.size = 1;

                    return;
                }

                if (!Horizontal && verticalScrollbar)
                {
                    verticalScrollbar.size = 1;

                    return;
                }

                return;
            }

            if (Horizontal && horizontalScrollbar)
            {
                //更新水平滑动条size和value
                if (contentBounds.size.x > 0)
                {
                    var value = (viewBounds.size.x - Mathf.Abs(offset.x)) /
                                contentBounds.size.x *
                                (ItemEnd - ItemStart) / TotalCount;
                    horizontalScrollbar.size = Mathf.Clamp01(value);
                }
                else
                {
                    horizontalScrollbar.size = 1;
                }

                var position = NormalizedPosition;
                horizontalScrollbar.value = !ReverseDirection ? position : 1 - position;

                return;
            }

            if (!Horizontal && verticalScrollbar)
            {
                //更新垂直滑动条size和value
                if (contentBounds.size.y > 0)
                {
                    var value = (viewBounds.size.y - Mathf.Abs(offset.y)) /
                                contentBounds.size.y *
                                (ItemEnd - ItemStart) / TotalCount;
                    verticalScrollbar.size = Mathf.Clamp01(value);
                }
                else
                {
                    verticalScrollbar.size = 1;
                }

                var position = NormalizedPosition;
                verticalScrollbar.value = !ReverseDirection ? position : 1 - position;
            }
        }

        /// <summary>
        /// 更新滑动条可见性
        /// </summary>
        private void UpdateScrollbarVisibility()
        {
            if (Loop)
            {
                return;
            }

            if (verticalScrollbar &&
                verticalScrollbarVisibility != ScrollbarVisibility.Permanent &&
                verticalScrollbar.gameObject.activeSelf != VScrollingNeeded)
                verticalScrollbar.gameObject.SetActive(VScrollingNeeded);

            if (horizontalScrollbar &&
                horizontalScrollbarVisibility != ScrollbarVisibility.Permanent &&
                horizontalScrollbar.gameObject.activeSelf != HScrollingNeeded)
                horizontalScrollbar.gameObject.SetActive(HScrollingNeeded);
        }

        /// <summary>
        /// 更新滚动条锚点和位置
        /// </summary>
        private void UpdateScrollbarLayout()
        {
            if (Loop)
            {
                return;
            }

            if (vSliderExpand && horizontalScrollbar)
            {
                //添加禁止在Inspector中修改水平滚动条的Anchors, SizeDelta, AnchoredPosition属性
                tracker.Add(this, 
                    hBarRect,
                    DrivenTransformProperties.AnchorMinX |
                    DrivenTransformProperties.AnchorMaxX |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.AnchoredPositionX);

                //更新水平滚动条的锚点,位置和大小
                hBarRect.anchorMin = new Vector2(0, hBarRect.anchorMin.y);
                hBarRect.anchorMax = new Vector2(1, hBarRect.anchorMax.y);
                hBarRect.anchoredPosition = new Vector2(0,hBarRect.anchoredPosition.y);
                hBarRect.sizeDelta = VScrollingNeeded ?
                    new Vector2(
                        -vSliderWidth,
                        hBarRect.sizeDelta.y) :
                    new Vector2(0, hBarRect.sizeDelta.y);
            }

            if (hSliderExpand && verticalScrollbar)
            {
                //添加禁止在Inspector中修改垂直滚动条的Anchors, SizeDelta, AnchoredPosition属性
                tracker.Add(this, 
                    vBarRect,
                    DrivenTransformProperties.AnchorMinY |
                    DrivenTransformProperties.AnchorMaxY |
                    DrivenTransformProperties.SizeDeltaY |
                    DrivenTransformProperties.AnchoredPositionY);

                //更新垂直滚动条的锚点,位置和大小
                vBarRect.anchorMin = new Vector2(vBarRect.anchorMin.x, 0);
                vBarRect.anchorMax = new Vector2(vBarRect.anchorMax.x, 1);
                vBarRect.anchoredPosition = new Vector2(vBarRect.anchoredPosition.x, 0);
                vBarRect.sizeDelta = HScrollingNeeded ?
                    new Vector2(
                        vBarRect.sizeDelta.x,
                        -hSliderHeight) :
                    new Vector2(vBarRect.sizeDelta.x, 0);
            }
        }

        /// <summary>
        /// 初始化快速移动数据
        /// </summary>
        private void InitSnapData()
        {
            lastVelocity = Vector2.zero;
            velocity = Vector2.zero;
            UpdateBounds();
            var index = GetFirstShow().Index;
            var num = 0;
            for (var i = 0; i < childrenList.Count; i++)
            {
                var item = childrenList[i];
                if (item.Index == index)
                {
                    num = i;

                    break;
                }
            }

            var itemBounds = GetBounds4Item(num);
            bool add;
            if (Horizontal)
            {
                add = itemBounds.center.x > viewBounds.min.x;
                var pos = add ? viewBounds.min.x - itemBounds.min.x :
                    viewBounds.min.x - itemBounds.max.x;
                currentSnapData.TargetSnapValue = pos + Spacing;
                currentSnapData.CurSnapValue = 0;
            }
            else
            {
                add = itemBounds.center.y > viewBounds.min.y;
                var pos = add ? viewBounds.min.y - itemBounds.min.y :
                    viewBounds.min.y - itemBounds.max.y;
                currentSnapData.TargetSnapValue = pos + Spacing;
                currentSnapData.CurSnapValue = 0;
            }

            SnapNearestItemIndex = add ? index : index + 1;
            currentSnapData.IsMoving = true;
            currentSnapData.IsNearestChange = true;
        }

        /// <summary>
        /// 更新快速移动
        /// </summary>
        /// <param name="immediately">是否立即完成</param>
        private void UpdateSnapMove(bool immediately = false)
        {
            if (!enableSnap)
            {
                return;
            }

            if (CanSnap())
            {
                //开始移动
                var position = Content.anchoredPosition;
                float speed = 0;
                var old = currentSnapData.CurSnapValue;
                currentSnapData.CurSnapValue = Mathf.SmoothDamp(
                    currentSnapData.CurSnapValue,
                    currentSnapData.TargetSnapValue,
                    ref speed,
                    smoothDumpRate,
                    Mathf.Infinity,
                    Time.smoothDeltaTime);

                //取得需要移动到的索引项
                var targetItem = GetShowItemByIndex(SnapNearestItemIndex);
                if (targetItem)
                {
                    var num = 0;
                    for (var i = 0; i < childrenList.Count; i++)
                    {
                        var item = childrenList[i];
                        if (item.Index == targetItem.Index)
                        {
                            num = i;

                            break;
                        }
                    }

                    //判断索引是否改变
                    var itemBounds = GetBounds4Item(num);
                    if (Horizontal)
                    {
                        if (currentSnapData.IsNearestChange &&
                            (itemBounds.center.x > viewBounds.min.x ||
                             itemBounds.center.x < viewBounds.max.x))
                        {
                            OnSnapNearestChanged?.Invoke(targetItem);
                            currentSnapData.IsNearestChange = false;
                        }
                    }
                    else
                    {
                        if (currentSnapData.IsNearestChange &&
                            (itemBounds.center.y > viewBounds.min.y ||
                             itemBounds.center.y < viewBounds.max.y))
                        {
                            OnSnapNearestChanged?.Invoke(targetItem);
                            currentSnapData.IsNearestChange = false;
                        }
                    }
                }

                var distance = currentSnapData.CurSnapValue - old;
                //如果需要立即完成或完成了移动
                if (immediately || currentSnapData.RemainDistance < snapFinishThreshold)
                {
                    distance = position.x + currentSnapData.TargetSnapValue - old;
                    position.x = Mathf.CeilToInt(distance);
                    currentSnapData.IsMoving = false;
                    currentSnapData.IsNearestChange = false;
                    if (targetItem)
                    {
                        OnSnapFinished?.Invoke(targetItem);
                    }
                }
                else
                {
                    if (Horizontal)
                    {
                        position.x += distance;
                    }
                    else
                    {
                        position.y += distance;
                    }
                }

                //设置位置
                SetContentPosition(position);
            }

            if (!updateSnap || !enableDragMove)
            {
                return;
            }

            updateSnap = false;
            InitSnapData();
        }

        /// <summary>
        /// 能否快速移动
        /// </summary>
        /// <returns></returns>
        private bool CanSnap()
        {
            if (dragging || childrenList.Count == 0)
            {
                return false;
            }

            if (Horizontal)
            {
                if (Content.rect.width < ViewRect.rect.width)
                {
                    return false;
                }
            }
            else
            {
                if (Content.rect.height < ViewRect.rect.height)
                {
                    return false;
                }
            }

            var speed = Mathf.Abs(Speed);
            var lastSpeed = Mathf.Abs(!Horizontal ? lastVelocity.y : lastVelocity.x);
            if (speed > snapVelocityThreshold)
            {
                return false;
            }

            if (lastSpeed > snapVelocityThreshold)
            {
                updateSnap = true;
            }

            if (!currentSnapData.IsMoving)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 滚动中效果
        /// </summary>
        /// <param name="speed">滚动速度</param>
        protected override void DoMovingEffect(float speed)
        {
            StopAllCoroutines();
            velocity = Vector2.zero;
            StartCoroutine(ScrollToCell(speed));
        }

        private IEnumerator ScrollToCell(float speed)
        {
            var moveMore = ReverseDirection ? moveIndex > GetLastShow().Index : moveIndex < GetFirstShow().Index;
            bool needMoving = true;
            var move = 0f;
            while (needMoving)
            {
                yield return 0;
                if (!dragging)
                {
                    if (moveIndex < ItemStart)
                    {
                        move = -Time.deltaTime * speed;
                    }
                    else if (moveIndex >= ItemEnd)
                    {
                        move = Time.deltaTime * speed;
                    }
                    else
                    {
                        viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                        var index = 0;
                        for (var i = 0; i < childrenList.Count; i++)
                        {
                            var item = childrenList[i];
                            if (item.Index == moveIndex)
                            {
                                index = i;

                                break;
                            }
                        }

                        var itemBounds = GetBounds4Item(index);
                        float offset;
                        if (Horizontal)
                        {
                            offset = ReverseDirection ? itemBounds.max.x - viewBounds.max.x : viewBounds.max.y - itemBounds.max.y;
                        }
                        else
                        {
                            offset = ReverseDirection ? viewBounds.min.y - itemBounds.min.y : itemBounds.min.x - viewBounds.min.x;
                        }

                        // check if we cannot move on
                        if (TotalCount >= 0)
                        {
                            if (offset > 0 && ItemEnd == TotalCount && !ReverseDirection)
                            {
                                itemBounds = GetBounds4Item(childrenList.Count - 1);
                                // 到达底部
                                if ((!Horizontal && itemBounds.min.y >= viewBounds.min.y) ||
                                    (Horizontal && itemBounds.max.x <= viewBounds.max.x))
                                {
                                    break;
                                }
                            }
                            else if (offset < 0 && ItemStart == 0 && ReverseDirection)
                            {
                                itemBounds = GetBounds4Item(0);
                                if ((!Horizontal && itemBounds.max.y <= viewBounds.max.y) ||
                                    (Horizontal && itemBounds.min.x >= viewBounds.min.x))
                                {
                                    break;
                                }
                            }
                        }

                        float maxMove = Time.deltaTime * speed;
                        if (Mathf.Abs(offset) < maxMove)
                        {
                            needMoving = false;
                            move = offset;
                        }
                        else
                        {
                            move = Mathf.Sign(offset) * maxMove;
                        }
                    }

                    if (move != 0)
                    {
                        Vector2 offset = GetVector(move);
                        SetContentPosition(offset + Content.anchoredPosition);
                        if (((moveIndex == ItemStart || ItemEnd == TotalCount) && !ReverseDirection ) || 
                            ((moveIndex == ItemEnd || ItemStart == 0) && ReverseDirection))
                        {
                            if (moveMore)
                            {
                                break;
                            }

                            SetContentPosition(Vector2.zero, false);

                            break;
                        }
                    }
                }
                else
                {
                    needMoving = false;
                }
            }

            if (moveMore)
            {
                var total = Horizontal ? GetFirstShow().Width : GetFirstShow().Height;
                var current = 0f;
                while (current <= total)
                {
                    yield return 0;
                    if (!dragging)
                    {
                        current += Mathf.Abs(move);
                        Vector2 offset = GetVector(move);
                        SetContentPosition(offset + Content.anchoredPosition, false);
                    }
                    else
                    {
                        break;
                    }
                }

                SetContentPosition(Vector2.zero);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 当该脚本被加载或检视面板的值被修改时，此函数被调用（仅在编辑器被调用）。
        /// </summary>
        protected override void OnValidate()
        {
            SetDirtyCaching();
        }
#endif
        #endregion

        #region Internal Properties
        /// <summary>
        /// 水平方向是否需要滚动
        /// </summary>
        private bool HScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                {
                    return contentBounds.size.x > viewBounds.size.x + 0.01f;
                }

                return true;
            }
        }

        /// <summary>
        /// 垂直方向是否需要伸展
        /// </summary>
        private bool VScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                {
                    return contentBounds.size.y > viewBounds.size.y + 0.01f;
                }

                return true;
            }
        }
        #endregion

        #region Internal Fields
        /// <summary>
        /// 只是针对于Movement == MovementType.Elastic有效
        /// </summary>
        [SerializeField]
        [Range(0, 0.5f)]
        private float elasticity = 0.05f;

        /// <summary>
        /// 拖拽比例
        /// </summary>
        [SerializeField]
        [Range(0, 2)]
        private float rubberScale = 1;

        /// <summary>
        /// 是否开启惯性
        /// </summary>
        [SerializeField]
        private bool inertia = true;

        /// <summary>
        /// 移动类型
        /// </summary>
        [SerializeField]
        private MovementType movementType = MovementType.Elastic;

        /// <summary>
        /// 水平滚动条可见性
        /// </summary>
        [SerializeField]
        private ScrollbarVisibility horizontalScrollbarVisibility;

        /// <summary>
        /// 水平滚动条
        /// </summary>
        [SerializeField]
        private Scrollbar horizontalScrollbar;

        /// <summary>
        /// 垂直滚动条可见性
        /// </summary>
        [SerializeField]
        private ScrollbarVisibility verticalScrollbarVisibility;

        /// <summary>
        /// 垂直滚动条
        /// </summary>
        [SerializeField]
        private Scrollbar verticalScrollbar;

        /// <summary>
        /// 当拖动值改变时
        /// </summary>
        [SerializeField]
        private ScrollRectEvent onValueChanged = null;

        [SerializeField]
        private bool enableSnap = false;

        [SerializeField]
        [Range(0, 1)]
        private float smoothDumpRate = 0.1f;

        /// <summary>
        /// 是否开启拖拽移动
        /// </summary>
        [SerializeField]
        private bool enableDragMove = true;

        //拖拽相关
        private bool dragging;

        //滚动条相关
        private bool hSliderExpand;
        private bool vSliderExpand;
        private float hSliderHeight;
        private float vSliderWidth;
        private RectTransform hBarRect;
        private RectTransform vBarRect;

        //snap相关
        private Vector2 lastVelocity;
        private bool updateSnap;
        private readonly SnapData currentSnapData = new SnapData();
        private float snapFinishThreshold = 0.1f;
        private float snapVelocityThreshold = 150;

        //鼠标中键滚动速率
        private float scrollSensitivity = 5;
        private DrivenRectTransformTracker tracker;
        private Bounds prevContentBounds;
        private Bounds prevViewBounds;
        private Vector2 prevPosition = Vector2.zero;
        private Vector2 pointerStartLocalCursor;
        private Vector2 contentStartPosition;
        #endregion

        #region Public Declarations
        /// <summary>
        /// 列表移动类型
        /// </summary>
        public enum MovementType
        {
            /// <summary>
            /// 自由的, 无限制的拖动
            /// </summary>
            Unrestricted,

            /// <summary>
            /// 带弹力
            /// </summary>
            Elastic,

            /// <summary>
            /// 限制边缘拖动
            /// </summary>
            Clamped,
        }

        /// <summary>
        /// 滚动条显示
        /// </summary>
        public enum ScrollbarVisibility
        {
            /// <summary>
            /// 永久显示
            /// </summary>
            Permanent,

            /// <summary>
            /// 自动隐藏
            /// </summary>
            AutoHide,

            /// <summary>
            /// 自动隐藏展开
            /// </summary>
            AutoHideAndExpandViewport,
        }

        /// <summary>
        /// 滑动区域事件
        /// </summary>
        [Serializable]
        public class ScrollRectEvent : UnityEvent<float>
        {

        }

        /// <summary>
        /// 快速移动数据
        /// </summary>
        private class SnapData
        {
            #region Properties
            /// <summary>
            /// 是否在移动中
            /// </summary>
            public bool IsMoving { get; set; }

            /// <summary>
            /// 索引改变是否改变
            /// </summary>
            public bool IsNearestChange { get; set; }

            /// <summary>
            /// 移动目标值
            /// </summary>
            public float TargetSnapValue { get; set; }

            /// <summary>
            /// 当前值
            /// </summary>
            public float CurSnapValue { get; set; }

            /// <summary>
            /// 剩余距离
            /// </summary>
            public float RemainDistance => Mathf.Abs(TargetSnapValue - CurSnapValue);
            #endregion

            #region Methods
            /// <summary>
            /// 停止移动
            /// </summary>
            public void Stop()
            {
                IsMoving = false;
                IsNearestChange = false;
                CurSnapValue = 0;
            }
            #endregion
        }
        #endregion
    }
}