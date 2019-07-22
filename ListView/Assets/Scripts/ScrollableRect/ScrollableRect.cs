using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DG.Tweening;
using Pool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUI.ListView
{
    /// <summary>
    /// 可滑动的区域  使用前需先为content赋值 可以通过FillItemData指定的回调函数设置列表数据。
    /// </summary>
    public class ScrollableRect : UIBehaviour, ICanvasElement, ILayoutElement
    {
        #region Properties
        /// <summary>
        /// 所有预制数据
        /// </summary>
        public List<ListViewPrefabSource> PrefabSources => prefabSources;

        /// <summary>
        /// 所有子项只读列表
        /// </summary>
        public ReadOnlyCollection<ScrollItem> SlotItems => 
            items ?? (items = new ReadOnlyCollection<ScrollItem>(childrenList));

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount
        {
            get => totalCount;
            set
            {
                totalCount = value;
                moveIndex = totalCount;
            }
        }

        /// <summary>
        /// 是否是循环列表, 当为True时 则总数量为-1(会自动填充格子不用手动调用)
        /// </summary>
        public bool Loop
        {
            get => isLoop;
            set => isLoop = value;
        }

        /// <summary>
        /// 滚动内容
        /// </summary>
        public RectTransform Content => content;

        /// <summary>
        /// 当前视口Transform
        /// </summary>
        public RectTransform ViewRect
        {
            get
            {
                if (viewRect == null)
                {
                    viewRect = transform as RectTransform;
                }

                return viewRect;
            }

            set => viewRect = value;
        }

        /// <summary>
        /// 滑动方向
        /// 当处于循环模式中, 每次切换模式请重新填充格子, 否则会出现错误
        /// </summary>
        public Direction ScrollDirection
        {
            get => scrollDirection;
            set
            {
                scrollDirection = value;
                SetAnchorPivot();
            }
        }

        /// <summary>
        /// 回弹效果, 仅在调用方法移动时有效, 在拖拽时无效
        /// </summary>
        public BounceType Bounce
        {
            get => bounceType;
            set => bounceType = value;
        }

        /// <summary>
        /// 当前移动速度 值越大则移动越快 大于0表示正向移动 小于0表示反向移动
        /// </summary>
        public Vector2 Velocity
        {
            get => velocity;
            set => velocity = value;
        } 

        /// <summary>
        /// 当处于惯性滚动时减速的速率。
        /// 越接近1，减速越慢，意味着滑动的时间和距离更长。
        /// </summary>
        public float DecelerationRate
        {
            get => decelerationRate;
            set => decelerationRate = Mathf.Clamp(value, 0.05f, 0.9f);
        }

        /// <summary>
        /// 减速倍数, 越大减速距离越小
        /// </summary>
        public float SlowDownCoefficient
        {
            get => slowDownCoefficient;
            set => slowDownCoefficient = Mathf.Clamp(value, 0.1f, 10);
        }

        /// <summary>
        /// 是否暂停动画播放
        /// </summary>
        public bool Pause
        {
            get => isPause;
            set
            {
                if (isPause != value)
                {
                    isPause = value;
                    if (isPause)
                    {
                        foreach (var tween in tempTweener)
                        {
                            tween.Pause();
                        }
                    }
                    else
                    {
                        foreach (var tween in tempTweener)
                        {
                            tween.Play();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 是否翻转方向
        /// </summary>
        public bool ReverseDirection => ScrollDirection == Direction.BottomToTop ||
                                           ScrollDirection == Direction.RightToLeft;

        /// <summary>
        /// 是否是水平
        /// </summary>
        public bool Horizontal => ScrollDirection == Direction.LeftToRight ||
                                     ScrollDirection == Direction.RightToLeft;
        #endregion

        #region Events
        /// <summary>
        /// 根据索引获取高度或宽度
        /// </summary>
        public Func<int, float> GetWidthOrHeight;

        /// <summary>
        /// 自定义格子
        /// <para>填充的索引</para>
        /// <para>返回当前项的预制体的路径</para>
        /// </summary>
        public Func<int, string> GetCustomItem;

        /// <summary>
        /// 填充格子数据
        /// <para>要填充的项</para>
        /// <para>填充的索引</para>
        /// </summary>
        public Action<ScrollItem, int> FillItemData;

        /// <summary>
        /// 滚动完成
        /// </summary>
        public Action<MoveState> MoveStateChanged;

        public Action<ScrollableRect, ScrollItem> OnItemInstantiated;

        public Action<ScrollableRect, ScrollItem> OnItemRecycled;

        /// <summary>
        /// 自定义开始回弹效果
        /// <para>可滑动区域</para>
        /// <para>开始速度值</para>
        /// <para>回弹结束事件</para>
        /// <para>返回修改后<see cref="ScrollableRect.Content"/> 的值</para>
        /// </summary>
        public Func<ScrollableRect, float, Action, Vector2> StartBounce;

        /// <summary>
        /// 自定义移动效果
        /// <para>可滑动区域</para>
        /// <para>开始速度值</para>
        /// <para>移动结束事件</para>
        /// <para>返回修改后<see cref="ScrollableRect.Content"/> 的值</para>
        /// </summary>
        public Func<ScrollableRect, float, Action, Vector2> MoveBounce;

        /// <summary>
        /// 自定义结束回弹效果
        /// <para>可滑动区域</para>
        /// <para>结束速度值</para>
        /// <para>回弹结束事件</para>
        /// <para>返回修改后<see cref="ScrollableRect.Content"/> 的值</para>
        /// </summary>
        public Func<ScrollableRect, float, Action, Vector2> EndBounce;
        #endregion

        #region Methods
        /// <summary>
        /// 清空所有的格子
        /// </summary>
        /// <param name="excludePool">是否清理池子</param>
        public void ClearCells(bool excludePool)
        {
            ItemStart = 0;
            ItemEnd = 0;
            for (int i = childrenList.Count - 1; i >= 0; i--)
            {
                var child = childrenList[i];
                child.Recycle();
            }

            childrenList.Clear();

            if (!excludePool)
            {
                return;
            }
            
            if (poolManager)
            {
                poolManager.RemoveAll();
            }
            else
            {
                var hashSet = new HashSet<string>();
                for (int i = 0; i < totalCount; i++)
                {
                    var poolName = GetCustomItem.Invoke(i);
                    hashSet.Add(poolName);
                }

                foreach (var set in hashSet)
                {
                    PoolManager.Instance.RemovePool(set);
                }
            }
        }

        /// <summary>
        /// 滚动到指定索引的位置
        /// </summary>
        /// <param name="index">索引位置</param>
        public void ScrollToView(int index)
        {
            ScrollToView(index, false);
        }

        /// <summary>
        /// 滚动到指定索引的位置
        /// </summary>
        /// <param name="index">索引位置</param>
        /// <param name="ani">如果为True 则缓动到指定位置 否则立即到达 </param>
        /// <param name="speed">滚动速度</param>
        public void ScrollToView(int index, bool ani, float speed = 1000)
        {
            if (!ani)
            {
                FillCells(index);

                return;
            }

            if (isLoop && index <= 0)
            {
                Debug.LogWarning("循环列表没有最大值, 必须指定大于0的值");

                return;
            }

            CustomMove = true;
            if (!isLoop)
            {
                moveIndex = index == -1 ?
                    totalCount : Mathf.Clamp(index, 0, totalCount);
            }
            else
            {
                moveIndex += index;
            }

            //重置锚点 清理动画
            SetAnchorPivot();
            ClearTweener();

            try
            {
                StartEffect(speed, 
                    () => MovingEffect(speed, () => EndEffect(MoveComplete)));
            }
            catch (Exception e)
            {
                StopMovement(3, true);
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 刷新正在显示的对象数据
        /// </summary>
        public void RefreshCells()
        {
            ItemEnd = ItemStart;
            for (int i = 0; i < childrenList.Count; i++)
            {
                var item = childrenList[i];
                if (ItemEnd < totalCount)
                {
                    //填充数据
                    try
                    {
                        FillItemData?.Invoke(item, ItemEnd);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }

                    ItemEnd++;
                }
                else
                {
                    //回收无用项
                    item.Recycle();
                    childrenList.Remove(item);
                    i--;
                }
            }
        }

        /// <summary>
        /// 重新填充所有格子, 会首先清除当前所有显示格子在重新填充
        /// </summary>
        /// <param name="offset">偏移</param>
        public void FillCells(int offset = 0)
        {
            //停止移动
            velocity = Vector2.zero;
            ItemStart = ReverseDirection ? totalCount - offset : offset;
            ItemEnd = ItemStart;

            //清空所有
            for (int i = childrenList.Count - 1; i >= 0; i--)
            {
                var item = childrenList[i];
                item.Recycle();
                childrenList.Remove(item);
            }

            if (!Loop && ItemStart % CellCount != 0)
            {
                Debug.LogWarning("当在最后一行填充网格时, 网格会变得很奇怪");
            }

            //开始填充
            float sizeFilled = 0;
            var sizeToFill = Horizontal ? ViewRect.rect.size.x : ViewRect.rect.size.y;
            while (sizeToFill > sizeFilled)
            {
                float size = ReverseDirection ? NewItemAtStart() : NewItemAtEnd();
                if (size <= 0)
                {
                    break;
                }

                sizeFilled += size;
            }

            //重置位置
            SetContentPosition(Vector2.zero, false);
            OnFilled();
        }

        /// <summary>
        /// 设置对象池, 如果已经初始化完成 则会重新填充所有格子
        /// </summary>
        public void SetPool(PoolManager pool)
        {
            if (!pool)
            {
                throw new Exception("对象池不能为空");
            }

            if (poolManager != null)
            {
                poolManager.RemoveAll();   
            }

            poolManager = pool;
            FillCells();
        }

        /// <summary>
        /// 全部停止移动
        /// </summary>
        /// <param name="offset">停止移动时的偏移</param>
        /// <param name="playEndEffect">是否播放停止回弹效果</param>
        public void StopMovement(int offset, bool playEndEffect = false)
        {
            if (playEndEffect && 
                (bounceType == BounceType.Both || bounceType == BounceType.OnlyEnd))
            {
                tempVelocity = velocity;
                velocity = Vector2.zero;
                FillCells(totalCount - offset);
                EndEffect(MoveComplete);

                return;
            }

            startEnd = false;
            CustomMove = false;
            velocity = Vector2.zero;
            SetContentPosition(Vector2.zero);
            moveComplete = null;
            startComplete = null;
            endComplete = null;

            ClearTweener(false);
            moveState = MoveState.Stop;
            MoveStateChanged?.Invoke(moveState);
        }

        /// <summary>
        /// 获取显示中的项
        /// </summary>
        /// <param name="index">列表索引</param>
        /// <returns></returns>
        public ScrollItem GetShowItem(int index)
        {
            if (index < 0 || index >= childrenList.Count)
            {
                return null;
            }

            return childrenList[index];
        }

        /// <summary>
        /// 获取显示中的项
        /// </summary>
        /// <param name="index">列表索引</param>
        /// <returns></returns>
        public ScrollItem GetShowItemByIndex(int index)
        {
            if (childrenList.Count <= 0)
            {
                return null;
            }

            foreach (var item in childrenList)
            {
                if (item.Index == index)
                {
                    return item;
                }
            }

            return childrenList[0];
        }

        /// <summary>
        /// 获取显示视图中第一个显示的元素
        /// </summary>
        /// <returns></returns>
        public ScrollItem GetFirstShow()
        {
            if (childrenList.Count <= 0)
            {
                return null;
            }

            for (var i = 0; i < childrenList.Count; i++)
            {
                var item = childrenList[i];
                var bounds = GetBounds4Item(i);
                if (Horizontal)
                {
                    if (bounds.max.x > viewBounds.min.x && 
                        bounds.min.x <= viewBounds.min.x)
                    {
                        return item;
                    }
                }
                else
                {
                    if (bounds.max.y > viewBounds.min.y &&
                        bounds.min.y <= viewBounds.min.y)
                    {
                        return item;
                    }
                }
            }

            return childrenList[0];
        }

        /// <summary>
        /// 设置内容区域位置 这个方法不会对元素进行增加删除 仅进行位置修正
        /// </summary>
        /// <param name="position">要设置的位置</param>
        public void SetPosition(Vector2 position)
        {
            SetContentPosition(position, false);
        }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            //在网格计算以后
            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();

                hasRebuiltLayout = true;
            }
        }
        #endregion

        #region Internal Methods
        protected override void Awake()
        {
            base.Awake();
            if (!content)
            {
                throw new Exception("Content节点为空");
            }

            //缓存布局器
            contentLayout = content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            gridLayout = content.GetComponent<GridLayoutGroup>();

            if (localPool)
            {
                poolManager = GetComponent<PoolManager>();
                if (poolManager == null)
                {
                    poolManager = gameObject.AddComponent<PoolManager>();
                }

                //创建本地池管理
                GameObject poolsRoot = new GameObject("Pools");
                poolsRoot.transform.SetParent(transform);
                poolsRoot.transform.localScale = Vector3.one;
                poolsRoot.transform.localPosition = Vector3.zero;
                poolManager.PoolsRoot = poolsRoot.transform;
            }

            SetAnchorPivot();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        protected virtual void LateUpdate()
        {
            if (!content || isPause)
            {
                return;
            }

            EnsureLayoutHasRebuilt();
            Vector2 position;
            switch (moveState)
            {
                case MoveState.StartMove when StartBounce != null:

                    position = StartBounce.Invoke(this, Speed, startComplete);
                    SetContentPosition(position, false);

                    break;
                case MoveState.Moving when MoveBounce != null:

                    position = MoveBounce.Invoke(this, Speed, () =>
                    {
                        moveComplete();
                        tempVelocity = velocity;
                    });
                    SetContentPosition(position);

                    break;
                case MoveState.EndMove when EndBounce != null:

                    var speed = Horizontal ? tempVelocity.x : tempVelocity.y;
                    position = EndBounce.Invoke(this, speed, endComplete);
                    SetContentPosition(position, false);

                    break;
                case MoveState.EndMove when EndBounce == null:

                    EndEffectTick();

                    break;
                default:

                    MovingEffectTick();

                    break;
            }
        }

        /// <summary>
        /// 当自身尺寸改变时调用
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            ClearTweener(false);
            velocity = Vector2.zero;
            hasRebuiltLayout = false;

            base.OnDisable();
        }

        /// <summary>
        /// 当格子填充完成
        /// </summary>
        protected virtual void OnFilled()
        {

        }

        /// <summary>
        /// 在开始处添加一个新项
        /// </summary>
        /// <returns>新项的大小</returns>
        protected virtual float NewItemAtStart()
        {
            if (!isLoop && ItemStart - CellCount < 0)
            {
                return 0;
            }

            float size = 0;
            //实例化一行或一列的元素
            for (int i = 0; i < CellCount; i++)
            {
                ItemStart--;
                var newItem = InstantiateItem(ItemStart);
                childrenList.Insert(0, newItem);
                newItem.RectTrans.SetAsFirstSibling();
                size = Mathf.Max(GetSize(newItem), size);
                OnItemInstantiated?.Invoke(this, newItem);
            }

            //设置项删除阀值 和内容区域位置
            threshold = Mathf.Max(threshold, size);
            if (!ReverseDirection)
            {
                Vector2 offset = GetVector(size);
                content.anchoredPosition += offset;
            }

            return size;
        }

        /// <summary>
        /// 在开始初删除一个项
        /// </summary>
        /// <returns>删除项的大小</returns>
        protected virtual float DeleteItemAtStart()
        {
            if (velocity != Vector2.zero &&
                !isLoop &&
                ItemEnd >= totalCount + 1 ||
                content.childCount == 0)
            {
                return 0;
            }

            float size = 0;
            //删除一行或一列的元素
            for (int i = 0; i < CellCount; i++)
            {
                var oldItem = childrenList[0];
                size = Mathf.Max(GetSize(oldItem), size);
                oldItem.Recycle();
                childrenList.Remove(oldItem);
                ItemStart++;
                OnItemRecycled?.Invoke(this, oldItem);

                if (childrenList.Count == 0)
                {
                    break;
                }
            }

            if (!ReverseDirection)
            {
                Vector2 offset = GetVector(size);
                content.anchoredPosition -= offset;
            }

            return size;
        }

        /// <summary>
        /// 在结尾处添加一个新项
        /// </summary>
        /// <returns>新项的大小</returns>
        protected virtual float NewItemAtEnd()
        {
            if (!isLoop && ItemEnd >= totalCount)
            {
                return 0;
            }

            float size = 0;
            int count = CellCount - childrenList.Count % CellCount;
            //实例化一行或一列的元素
            for (int i = 0; i < count; i++)
            {
                var newItem = InstantiateItem(ItemEnd);
                childrenList.Add(newItem);
                size = Mathf.Max(GetSize(newItem), size);
                ItemEnd++;
                OnItemInstantiated?.Invoke(this, newItem);

                if (!isLoop && ItemEnd >= totalCount)
                {
                    break;
                }
            }

            //设置项删除阀值 和内容区域位置
            threshold = Mathf.Max(threshold, size);
            if (ReverseDirection)
            {
                Vector2 offset = GetVector(size);
                content.anchoredPosition -= offset;
            }

            return size;
        }

        /// <summary>
        /// 在结尾处删除一个项
        /// </summary>
        /// <returns>删除项的大小</returns>
        protected virtual float DeleteItemAtEnd()
        {
            if (velocity != Vector2.zero &&
                !isLoop &&
                ItemStart < CellCount ||
                content.childCount == 0)
            {
                return 0;
            }

            float size = 0;
            //删除一行或一列的元素
            for (int i = 0; i < CellCount; i++)
            {
                var oldItem = childrenList[childrenList.Count - 1];
                size = Mathf.Max(GetSize(oldItem), size);
                oldItem.Recycle();
                childrenList.Remove(oldItem);
                ItemEnd--;
                OnItemRecycled?.Invoke(this, oldItem);

                if (ItemEnd % CellCount == 0 || childrenList.Count == 0)
                {
                    break;
                }
            }

            if (ReverseDirection)
            {
                Vector2 offset = GetVector(size);
                content.anchoredPosition += offset;
            }

            return size;
        }

        /// <summary>
        /// 实例化新项
        /// </summary>
        /// <param name="itemIndex">索引</param>
        /// <returns></returns>
        protected ScrollItem InstantiateItem(int itemIndex)
        {
            //查找当前索引对应的项数据
            ListViewPrefabSource prefabSource = null;
            if (GetCustomItem != null)
            {
                var customItem = GetCustomItem(itemIndex);
                if (string.IsNullOrEmpty(customItem))
                {
                    prefabSource = prefabSources[0];
                }
                else
                {
                    foreach (var item in prefabSources)
                    {
                        if (item.prefabName == customItem)
                        {
                            prefabSource = item;

                            break;
                        }
                    }

                    if (prefabSource == null)
                    {
                        Debug.LogError("未找到Prefab:" + customItem);
                    }
                }
            }
            else
            {
                prefabSource = prefabSources[0];
            }

            if (prefabSource == null)
            {
                return null;
            }

            //获取池子并生成项
            var pool = GetPoolByName(prefabSource.PrefabName, prefabSource.Template);
            var spawn = pool.Spawn();
            var slotItem = spawn.GetComponent<ScrollItem>();
            if (slotItem == null)
            {
                slotItem = spawn.AddComponent<ScrollItem>();
            }

            //初始化参数
            slotItem.Parent = this;
            slotItem.Index = itemIndex;
            slotItem.Pool = pool;
            slotItem.name = itemIndex.ToString();
            slotItem.Spacing = Spacing;
            slotItem.RectTrans.SetParent(Content, false);
            slotItem.gameObject.SetActive(true);

            //填充数据
            try
            {
                FillItemData?.Invoke(slotItem, itemIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            return slotItem;
        }

        /// <summary>
        /// 通过名字获取对象池
        /// </summary>
        /// <param name="poolName">池名字也可以是路径</param>
        /// <param name="template">模板</param>
        /// <returns></returns>
        protected ObjectPool<GameObject> GetPoolByName(
            string poolName, 
            GameObject template = null)
        {
            ObjectPool<GameObject> pool;
            //如果是本地池
            if (poolManager != null)
            {
                if (poolManager.TryGetPool(poolName, out pool))
                {

                }
                else
                {
                    if (template == null)
                    {
                        template = Resources.Load<GameObject>(poolName);
                    }

                    pool = poolManager.GetOrCreatePool(
                        poolName + GetHashCode(), template, 1);
                }
            }
            else
            {
                //获取池子
                if (PoolManager.Instance.TryGetPool(poolName, out pool))
                {

                }
                else
                {
                    if (template == null)
                    {
                        //这里可以使用自己的资源加载方式
                        template = Resources.Load<GameObject>(poolName);
                    }

                    pool = PoolManager.Instance.GetOrCreatePool(
                        poolName + GetHashCode(), template, 1);
                }
            }

            return pool;
        }

        /// <summary>
        /// 开始滚动效果
        /// </summary>
        /// <param name="speed">初始移动速度</param>
        /// <param name="complete">开始效果播放完成回调</param>
        private void StartEffect(float speed, Action complete)
        {
            if (moveState == MoveState.StartMove || moveState == MoveState.Moving)
            {
                return;
            }

            moveState = MoveState.StartMove;
            MoveStateChanged?.Invoke(moveState);
            startComplete = complete;
            if (bounceType == BounceType.Both || bounceType == BounceType.OnlyStart)
            {
                if (StartBounce == null)
                {
                    DoStartEffect(speed, complete);
                }

                return;
            }

            complete?.Invoke();
        }

        /// <summary>
        /// 滚动中效果
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="complete">开始效果播放完成回调</param>
        private void MovingEffect(float speed, Action complete)
        {
            moveState = MoveState.Moving;
            MoveStateChanged?.Invoke(moveState);

            moveComplete = complete;
            if (MoveBounce == null)
            {
                DoMovingEffect(speed, complete);
            }
        }

        /// <summary>
        /// 结束滚动效果
        /// </summary>
        /// <param name="complete">开始效果播放完成回调</param>
        private void EndEffect(Action complete)
        {
            moveState = MoveState.EndMove;
            MoveStateChanged?.Invoke(moveState);
            endComplete = complete;
            if (bounceType == BounceType.Both || bounceType == BounceType.OnlyEnd)
            {
                if (EndBounce == null)
                {
                    DoEndEffect(complete);
                }

                return;
            }

            complete?.Invoke();
        }

        /// <summary>
        /// 开始滚动效果
        /// </summary>
        /// <param name="speed">滚动速度</param>
        /// <param name="complete">开始滚动完成回调</param>
        protected virtual void DoStartEffect(float speed, Action complete)
        {
            speed = Mathf.Abs(speed);
            float current = 0;
            var tween = DOTween.To(
                    () => current,
                    value =>
                    {
                        current = value;
                        var realSpeed = ReverseDirection ? -current : current;
                        velocity = Horizontal ? new Vector2(realSpeed, 0) :
                            new Vector2(0, realSpeed);
                    },
                    speed,
                    0.5f).SetEase(Ease.InExpo)
                .OnComplete(() =>
                {
                    complete();
                    startComplete = null;
                });

            tempTweener.Add(tween);
        }

        /// <summary>
        /// 滚动中效果
        /// </summary>
        /// <param name="speed">滚动速度</param>
        /// <param name="complete">滚动完成回调</param>
        protected virtual void DoMovingEffect(float speed, Action complete)
        {
            speed = ReverseDirection ? -speed : speed;
            velocity = GetVector(speed);
            slowDistance = GetSlowDownDistance(velocity);
            totalDistance = GetTotalSize();
            moveDistance = 0;
        }

        /// <summary>
        /// 移动中循环事件
        /// </summary>
        private void MovingEffectTick()
        {
            var position = content.anchoredPosition;
            var deltaTime = Time.smoothDeltaTime;
            var axis = Horizontal ? 0 : 1;
            if (velocity != Vector2.zero)
            {
                //判断能否移动
                if (!CanMove())
                {
                    if (!velocity.Equals(Vector2.zero))
                    {
                        //移动完成
                        tempVelocity = velocity;
                        velocity = Vector2.zero;
                        moveComplete?.Invoke();
                        moveComplete = null;
                    }
                }

                if (velocity != Vector2.zero)
                {
                    //如果速度不为零, 则进行移动
                    var distance = Vector2.zero;
                    distance[axis] = velocity[axis] * deltaTime;

                    if (moveState == MoveState.Moving)
                    {
                        moveDistance += Horizontal ? distance[0] : distance[1];
                    }

                    //移动一定距离时开始减速
                    if (Mathf.Abs(moveDistance) > totalDistance - slowDistance + 10)
                    {
                        velocity[axis] *= Mathf.Pow(
                            DecelerationRate, deltaTime * SlowDownCoefficient);
                        if (Mathf.Abs(velocity[axis]) < 10)
                        {
                            velocity[axis] = 0;
                        }

                        position[axis] += distance[axis];
                    }
                    else
                    {
                        position += distance;
                    }

                    SetContentPosition(position);
                }
            }
        }

        /// <summary>
        /// 结束滚动效果
        /// </summary>
        /// <param name="complete">结束滚动完成回调</param>
        protected virtual void DoEndEffect(Action complete)
        {
            
        }

        /// <summary>
        /// 结束效果回弹事件
        /// </summary>
        private void EndEffectTick()
        {
            //移动结束减速回弹效果
            var position = content.anchoredPosition;
            var deltaTime = Time.unscaledDeltaTime;
            var axis = Horizontal ? 0 : 1;
            if (tempVelocity[axis] != 0)
            {
                tempVelocity[axis] *= Mathf.Pow(0.1f, deltaTime * 15);
                if (Mathf.Abs(tempVelocity[axis]) < 100)
                {
                    tempVelocity[axis] = 0;
                    startEnd = true;
                }

                position[axis] += tempVelocity[axis] * deltaTime;
                SetContentPosition(position, false);
            }

            //移动结束加速弹回效果
            if (startEnd)
            {
                var offset = CalculateOffset(Vector2.zero);
                float v = 0;
                position[axis] = Mathf.SmoothDamp(
                    Content.anchoredPosition[axis],
                    Content.anchoredPosition[axis] + offset[axis],
                    ref v,
                    0.04f,
                    Mathf.Infinity, deltaTime);

                if (Mathf.Abs(v) < 10)
                {
                    startEnd = false;
                    endComplete?.Invoke();
                    endComplete = null;
                }
                else
                {
                    SetContentPosition(position, false);
                }
            }
        }

        /// <summary>
        /// 移动完成事件
        /// </summary>
        private void MoveComplete()
        {
            CustomMove = false;
            var pos = content.anchoredPosition;
            pos.x = Mathf.CeilToInt(pos.x);
            pos.y = Mathf.CeilToInt(pos.y);
            SetContentPosition(pos, false);

            moveState = MoveState.MoveComplete;
            MoveStateChanged?.Invoke(moveState);
        }

        /// <summary>
        /// 清理缓存的动画
        /// </summary>
        private void ClearTweener(bool complete = true)
        {
            if (tempTweener.Count == 0)
            {
                return;
            }

            foreach (var tween in tempTweener)
            {
                tween.Kill(complete);
            }

            tempTweener.Clear();
        }

        /// <summary>
        /// 计算视图区域与内容区域的间隔
        /// </summary>
        /// <param name="delta">偏移值</param>
        /// <returns></returns>
        protected internal virtual Vector2 CalculateOffset(Vector2 delta)
        {
            var offset = Vector2.zero;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;
            if (Horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;
                if (min.x > viewBounds.min.x)
                {
                    offset.x = viewBounds.min.x - min.x;
                }
                else if (max.x < viewBounds.max.x)
                {
                    offset.x = viewBounds.max.x - max.x;
                }
            }
            else
            {
                min.y += delta.y;
                max.y += delta.y;
                if (max.y < viewBounds.max.y)
                {
                    offset.y = viewBounds.max.y - max.y;
                }
                else if (min.y > viewBounds.min.y)
                {
                    offset.y = viewBounds.min.y - min.y;
                }
            }

            return offset;
        }

        /// <summary>
        /// 设置锚点和坐标
        /// </summary>
        private void SetAnchorPivot()
        {
            switch (ScrollDirection)
            {
                case Direction.LeftToRight:
                    content.anchorMin = new Vector2(0, 0.5f);
                    content.anchorMax = new Vector2(0, 0.5f);
                    content.pivot = new Vector2(0, 0.5f);

                    break;
                case Direction.RightToLeft:
                    content.anchorMin = new Vector2(1, 0.5f);
                    content.anchorMax = new Vector2(1, 0.5f);
                    content.pivot = new Vector2(1, 0.5f);

                    break;
                case Direction.TopToBottom:
                    content.anchorMin = new Vector2(0.5f, 1);
                    content.anchorMax = new Vector2(0.5f, 1);
                    content.pivot = new Vector2(0.5f, 1);

                    break;
                case Direction.BottomToTop:
                    content.anchorMin = new Vector2(0.5f, 0);
                    content.anchorMax = new Vector2(0.5f, 0);
                    content.pivot = new Vector2(0.5f, 0);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            content.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 获取滚动总长度
        /// </summary>
        /// <returns></returns>
        private float GetTotalSize()
        {
            float size = 0;
            var num = moveIndex;
            if (gridLayout != null)
            {
                num = moveIndex / CellCount + 1;
            }

            for (var i = 0; i < Mathf.Abs(num); i++)
            {
                size += GetSize(i);
                size += Spacing;
            }

            var offset = Horizontal ? ViewRect.rect.width : ViewRect.rect.height;

            return Mathf.Abs(size) - offset;
        }

        /// <summary>
        /// 获取指定索引项的大小
        /// </summary>
        /// <param name="index">项索引</param>
        /// <returns></returns>
        protected float GetSize(int index)
        {
            if (index >= 0 && index < childrenList.Count)
            {
                var child = childrenList[index];

                float size = Spacing;
                switch (ScrollDirection)
                {
                    case Direction.LeftToRight:
                    case Direction.RightToLeft:
                        if (gridLayout != null)
                        {
                            size += gridLayout.cellSize.x;
                        }
                        else
                        {
                            size += child.Width;
                        }

                        break;
                    case Direction.TopToBottom:
                    case Direction.BottomToTop:
                        if (gridLayout != null)
                        {
                            size += gridLayout.cellSize.y;
                        }
                        else
                        {
                            size += child.Height;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return size;
            }

            if (GetWidthOrHeight == null)
            {
                throw new Exception("获取高度或宽度异常");
            }

            return GetWidthOrHeight.Invoke(index);
        }

        /// <summary>
        /// 获取指定项的大小
        /// </summary>
        /// <param name="item">项</param>
        /// <returns></returns>
        protected float GetSize(ScrollItem item)
        {
            float size = Spacing;
            switch (ScrollDirection)
            {
                case Direction.LeftToRight:
                case Direction.RightToLeft:
                    if (gridLayout != null)
                    {
                        size += gridLayout.cellSize.x;
                    }
                    else
                    {
                        size += item.Width;
                    }

                    break;
                case Direction.TopToBottom:
                case Direction.BottomToTop:
                    if (gridLayout != null)
                    {
                        size += gridLayout.cellSize.y;
                    }
                    else
                    {
                        size += item.Height;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return size;
        }

        /// <summary>
        /// 获取二维向量
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected Vector2 GetVector(float value)
        {
            if (Horizontal)
            {
                return new Vector2(-value, 0);
            }

            return new Vector2(0, value);
        }

        /// <summary>
        /// 更新当前项
        /// </summary>
        /// <returns></returns>
        protected bool UpdateItems()
        {
            return Horizontal ? UpdateHorizontalItem() : UpdateVerticalItem();
        }

        private bool UpdateHorizontalItem()
        {
            bool changed = false;
            //当内容区域的最小值小于视图区域的最小值时
            if (viewBounds.max.x > contentBounds.max.x)
            {
                float size = NewItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.max.x > contentBounds.max.x + totalSize)
                {
                    size = NewItemAtEnd();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }

            //当视图区域的最大值时 > 内容区域的最大值
            if (viewBounds.min.x < contentBounds.min.x)
            {
                float size = NewItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.min.x < contentBounds.min.x - totalSize)
                {
                    size = NewItemAtStart();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }

            //当视图区域的最小值时 > 内容区域的最小值 + 阀值
            if (viewBounds.max.x < contentBounds.max.x - threshold)
            {
                float size = DeleteItemAtEnd(), totalSize = size;
                while (size > 0 && 
                       viewBounds.max.x < contentBounds.max.x - threshold - totalSize)
                {
                    size = DeleteItemAtEnd();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }

            //当视图区域的最大值时 < 内容区域的最大值 - 阀值
            if (viewBounds.min.x > contentBounds.min.x + threshold)
            {
                float size = DeleteItemAtStart(), totalSize = size;
                while (size > 0 && 
                       viewBounds.min.x > contentBounds.min.x + threshold + totalSize)
                {
                    size = DeleteItemAtStart();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }

            return changed;
        }

        private bool UpdateVerticalItem()
        {
            bool changed = false;
            //当内容区域的最小值小于视图区域的最小值时
            if (viewBounds.min.y < contentBounds.min.y)
            {
                float size = NewItemAtEnd(), totalSize = size;
                while (size > 0 && viewBounds.min.y < contentBounds.min.y - totalSize)
                {
                    size = NewItemAtEnd();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }
            //当视图区域的最小值时 > 内容区域的最小值 + 阀值
            else if (viewBounds.min.y > contentBounds.min.y + threshold)
            {
                float size = DeleteItemAtEnd(), totalSize = size;
                while (size > 0 && 
                       viewBounds.min.y > contentBounds.min.y + threshold + totalSize)
                {
                    size = DeleteItemAtEnd();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }

            //当视图区域的最大值时 > 内容区域的最大值
            if (viewBounds.max.y > contentBounds.max.y)
            {
                float size = NewItemAtStart(), totalSize = size;
                while (size > 0 && viewBounds.max.y > contentBounds.max.y + totalSize)
                {
                    size = NewItemAtStart();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }
            //当视图区域的最大值时 < 内容区域的最大值 - 阀值
            else if (viewBounds.max.y < contentBounds.max.y - threshold)
            {
                float size = DeleteItemAtStart(), totalSize = size;
                while (size > 0 && 
                       viewBounds.max.y < contentBounds.max.y - threshold - totalSize)
                {
                    size = DeleteItemAtStart();
                    totalSize += size;
                }

                if (totalSize > 0)
                {
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// 当前能否移动
        /// </summary>
        /// <returns></returns>
        protected bool CanMove()
        {
            if (!CanMove(0) || !CanMove(childrenList.Count - 1))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断能否继续移动
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool CanMove(int index)
        {
            if (index < 0 || index >= childrenList.Count)
            {
                return true;
            }

            viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
            var itemBounds = GetBounds4Item(index);
            float offset;
            switch (ScrollDirection)
            {
                case Direction.LeftToRight:
                    offset = itemBounds.min.x - viewBounds.min.x;

                    break;
                case Direction.RightToLeft:
                    offset = itemBounds.max.x - viewBounds.max.x;

                    break;
                case Direction.TopToBottom:
                    offset = itemBounds.min.y - viewBounds.min.y;

                    break;
                case Direction.BottomToTop:
                    offset = itemBounds.max.y - viewBounds.max.y;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ItemEnd >= moveIndex && offset >= 0 && !ReverseDirection)
            {
                var count = ItemEnd == moveIndex ? 
                    childrenList.Count - 1 : childrenList.Count - 2;
                itemBounds = GetBounds4Item(count);
                if (!Horizontal && itemBounds.min.y > viewBounds.min.y)
                {
                    return false;
                }

                if (Horizontal && itemBounds.max.x < viewBounds.max.x)
                {
                    return false;
                }
            }
            else if (ItemStart <= totalCount - moveIndex &&
                     offset <= 0 && ReverseDirection)
            {
                var count = ItemStart == totalCount - moveIndex ? 0 : 1;
                itemBounds = GetBounds4Item(count);
                if (!Horizontal && itemBounds.max.y < viewBounds.max.y)
                {
                    return false;
                }

                if (Horizontal && itemBounds.min.x > viewBounds.min.x)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 设置内容节点位置
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="updateItems">是否更新项</param>
        protected void SetContentPosition(Vector2 position, bool updateItems = true)
        {
            if (!Horizontal)
            {
                position.x = content.anchoredPosition.x;
            }
            else
            {
                position.y = content.anchoredPosition.y;
            }

            if (position != content.anchoredPosition)
            {
                content.anchoredPosition = position;
                UpdateBounds(updateItems);
            }
        }

        /// <summary>
        /// 获取减速距离
        /// </summary>
        /// <returns></returns>
        private float GetSlowDownDistance(Vector2 speed)
        {
            var distance = 0f;
            while (speed != Vector2.zero)
            {
                for (int axis = 0; axis < 2; axis++)
                {
                    speed[axis] *= Mathf.Pow(
                        decelerationRate,
                        Time.unscaledDeltaTime * slowDownCoefficient);
                    if (Mathf.Abs(speed[axis]) < 100)
                    {
                        speed[axis] = 0;
                    }

                    distance += speed[axis] * Time.unscaledDeltaTime;
                }
            }

            return Mathf.Abs(distance);
        }

        /// <summary>
        /// 更新内容区域大小
        /// </summary>
        /// <param name="updateItems"></param>
        protected virtual void UpdateBounds(bool updateItems = false)
        {
            viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
            contentBounds = GetBounds();

            if (content == null)
            {
                return;
            }

            //检查是否可以添加或删除节点
            if (updateItems && UpdateItems())
            {
                contentBounds = GetBounds();
            }

            //调整内容区域大小, 锚点和轴心
            var size = contentBounds.size;
            var center = contentBounds.center;
            var pivot = content.pivot;
            AdjustBounds(
                ref viewBounds, 
                ref pivot, 
                ref size,
                ref center);
            contentBounds.size = size;
            contentBounds.center = center;
        }

        /// <summary>
        /// 调整区域
        /// </summary>
        /// <param name="viewBounds"></param>
        /// <param name="contentPivot"></param>
        /// <param name="contentSize"></param>
        /// <param name="contentPos"></param>
        private void AdjustBounds(
            ref Bounds viewBounds,
            ref Vector2 contentPivot,
            ref Vector3 contentSize,
            ref Vector3 contentPos)
        {
            var excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }

            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        /// <summary>
        /// 获取内容区域大小
        /// </summary>
        /// <returns></returns>
        protected Bounds GetBounds()
        {
            if (content == null)
            {
                return new Bounds();
            }

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            //通过矩阵变换获得对应点的坐标并取得其最小值,最大值
            var toLocal = ViewRect.worldToLocalMatrix;
            content.GetWorldCorners(corners);
            for (int j = 0; j < 4; j++)
            {
                Vector3 v = toLocal.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            //通过最小值, 最大值重新计算区域
            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);

            return bounds;
        }

        /// <summary>
        /// 根据索引获取项的区域大小
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        protected Bounds GetBounds4Item(int index)
        {
            if (content == null)
            {
                return new Bounds();
            }

            if (index < 0 || index >= childrenList.Count)
            {
                return new Bounds();
            }

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var rectTrans = childrenList[index].RectTrans;
            if (rectTrans == null)
            {
                return new Bounds();
            }

            //通过矩阵变换获得对应点的坐标并取得其最小值,最大值
            rectTrans.GetWorldCorners(corners);
            var toLocal = ViewRect.worldToLocalMatrix;
            for (var j = 0; j < 4; j++)
            {
                Vector3 v = toLocal.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            //通过最小值, 最大值重新计算区域
            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);

            return bounds;
        }

        /// <summary>
        /// 确定网格位置已经计算完毕
        /// </summary>
        protected void EnsureLayoutHasRebuilt()
        {
            if (!hasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
            {
                Canvas.ForceUpdateCanvases();
            }
        }

        /// <summary>
        /// 网格计算完成
        /// </summary>
        void ICanvasElement.LayoutComplete()
        {

        }

        void ICanvasElement.GraphicUpdateComplete()
        {

        }

        void ILayoutElement.CalculateLayoutInputHorizontal()
        {
        }

        void ILayoutElement.CalculateLayoutInputVertical()
        {
        }
        #endregion

        #region Internal Properties
        /// <summary>
        /// 当前移动速度
        /// </summary>
        protected float Speed => Horizontal ? velocity.x : velocity.y;

        /// <summary>
        /// 格子间隔
        /// </summary>
        protected float Spacing
        {
            get
            {
                if (content == null)
                {
                    return 0;
                }

                if (contentLayout != null)
                {
                    return ((HorizontalOrVerticalLayoutGroup)contentLayout).spacing;
                }

                if (gridLayout != null)
                {
                    if (Horizontal)
                    {
                        return -gridLayout.spacing.x;
                    }

                    return gridLayout.spacing.y;
                }

                return 0;
            }
        }

        /// <summary>
        /// 某一行或某一列的物体数量
        /// </summary>
        protected int CellCount
        {
            get
            {
                if (content != null)
                {
                    if (gridLayout != null)
                    {
                        if (gridLayout.constraint == GridLayoutGroup.Constraint.Flexible)
                        {
                            Debug.LogWarning("不支持自由模式");
                        }

                        return gridLayout.constraintCount;
                    }
                }

                return 1;
            }
        }

        /// <summary>
        /// 是否处于自定义移动中(通过代码调用 MoveBySpeed或MoveByTime属于自定义移动)
        /// </summary>
        protected bool CustomMove { get; set; }

        /// <summary>
        /// 当前显示中的开始索引
        /// </summary>
        protected int ItemStart { get; set; }

        /// <summary>
        /// 当前显示中的结束索引 + 1
        /// </summary>
        protected int ItemEnd { get; set; }

        float ILayoutElement.minWidth => -1;

        float ILayoutElement.preferredWidth => -1;

        float ILayoutElement.flexibleWidth => -1;

        float ILayoutElement.minHeight => -1;

        float ILayoutElement.preferredHeight => -1;

        float ILayoutElement.flexibleHeight => -1;

        int ILayoutElement.layoutPriority => -1;
        #endregion

        #region Internal Fields
        /// <summary>
        /// prefab数据
        /// </summary>
        [SerializeField]
        private List<ListViewPrefabSource> prefabSources;

        /// <summary>
        /// 是否使用本地对象池
        /// </summary>
        [SerializeField]
        private bool localPool = false;

        [SerializeField]
        private bool isLoop;

        /// <summary>
        /// 内容父节点
        /// </summary>
        [SerializeField]
        private RectTransform content;

        /// <summary>
        /// 总数量
        /// </summary>
        [SerializeField]
        private int totalCount = 1;

        [SerializeField]
        private float decelerationRate = 0.3f;

        [SerializeField]
        private float slowDownCoefficient = 2;

        [SerializeField]
        private RectTransform viewRect;

        [SerializeField]
        private Direction scrollDirection = Direction.LeftToRight;

        [Tooltip("边缘回弹效果")]
        [SerializeField]
        private BounceType bounceType = BounceType.Custom;

        protected float threshold;
        protected Bounds contentBounds;
        protected Bounds viewBounds;
        protected Vector2 velocity;

        private Action startComplete;
        private Action moveComplete;
        private Action endComplete;
        private bool isPause;
        private MoveState moveState = MoveState.Stop;
        private Vector2 tempVelocity;
        private bool startEnd;
        private int moveIndex;
        private float totalDistance;
        private float slowDistance;
        private float moveDistance;

        private List<Tween> tempTweener = new List<Tween>(4);
        private ReadOnlyCollection<ScrollItem> items;
        private GridLayoutGroup gridLayout;
        private LayoutGroup contentLayout;

        protected readonly List<ScrollItem> childrenList = new List<ScrollItem>();
        protected readonly Vector3[] corners = new Vector3[4];
        protected PoolManager poolManager;
        private bool hasRebuiltLayout;
        #endregion

        #region Public Declarations
        /// <summary>
        /// 滑动方向
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// 左向右
            /// </summary>
            LeftToRight,

            /// <summary>
            /// 右向左
            /// </summary>
            RightToLeft,

            /// <summary>
            /// 顶往底
            /// </summary>
            TopToBottom,

            /// <summary>
            /// 底往顶
            /// </summary>
            BottomToTop
        }

        /// <summary>
        /// 子元素渲染顺序
        /// </summary>
        public enum ChildrenRenderOrder
        {
            /// <summary>
            /// 升序
            /// </summary>
            Ascent,

            /// <summary>
            /// 降序
            /// </summary>
            Descent,

            /// <summary>
            /// 拱形
            /// </summary>
            Arch,
        }

        /// <summary>
        /// 回弹效果, 仅在调用方法移动时有效, 在拖拽时无效
        /// </summary>
        public enum BounceType
        {
            /// <summary>
            /// 自定义回弹效果
            /// </summary>
            Custom,

            /// <summary>
            /// 只有开始回弹
            /// </summary>
            OnlyStart,

            /// <summary>
            /// 只有结束回弹
            /// </summary>
            OnlyEnd,

            /// <summary>
            /// 结束和开始都有
            /// </summary>
            Both
        }

        /// <summary>
        /// 移动状态
        /// </summary>
        public enum MoveState
        {
            /// <summary>
            /// 停止状态
            /// </summary>
            Stop,

            /// <summary>
            /// 暂停状态
            /// </summary>
            Pause,

            /// <summary>
            /// 开始移动
            /// </summary>
            StartMove,

            /// <summary>
            /// 移动中
            /// </summary>
            Moving,

            /// <summary>
            /// 结束移动
            /// </summary>
            EndMove,

            /// <summary>
            /// 移动完成
            /// </summary>
            MoveComplete
        }
        #endregion
    }
}

