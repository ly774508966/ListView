using System;
using System.Collections;
using System.Collections.Generic;
using UObject = UnityEngine.Object;

namespace UGUI
{
    /// <summary>
    /// 滑动槽控制器
    /// </summary>
    public class ScrollController
    {
        #region Public Properties
        /// <summary>
        /// 滑动槽
        /// </summary>
        public ScrollableRect Target { get; protected set; }

        /// <summary>
        /// 绑定数据
        /// </summary>
        public IList SourceDatas { get; protected set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="slot">滑动列表</param>
        /// <param name="creator">绑定创建器</param>
        /// <param name="datas">准备初始化的数据</param>
        public void Initialize(
            ScrollableRect slot,
            Func<ItemBinder> creator,
            IList datas)
        {
            if (isInit)
            {
                return;
            }

            Target = slot;
            binderCreator = creator;
            SourceDatas = datas;
            OnInitialize();

            Target.FillItemData = FillItemData;
            Target.TotalCount = SourceDatas.Count;

            isInit = true;
        }

        /// <summary>
        /// 重置数据并重新初始化
        /// </summary>
        /// <param name="datas">重新绑定的数据</param>
        public virtual void ResetData(IList datas)
        {
            SourceDatas = datas;
            if (Target.Loop)
            {
                Target.FillCells();
            }
            else
            {
                Target.TotalCount = SourceDatas.Count;
                Target.FillCells();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public ItemBinder GetBinder(UObject item)
        {
            return binders.TryGetValue(item, out var binder) ? binder : null;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        public void Release()
        {
            if (!isInit)
            {
                return;
            }

            OnRelease();
            foreach (var binder in binders)
            {
                if (!binder.Value.resetFlag)
                {
                    binder.Value.Reset();
                    binder.Value.resetFlag = true;
                }

                binder.Value.Release();
            }

            binders.Clear();
            Target.ClearCells(true);
            SourceDatas = null;
        }
        #endregion

        #region Internal Methods
        protected internal virtual void OnInitialize()
        {

        }

        protected internal virtual void OnRelease()
        {

        }

        /// <summary>
        /// Item填充数据
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        private void FillItemData(ScrollItem item, int index)
        {
            if (Target.Loop)
            {
                int itemCount = SourceDatas.Count;
                if (index >= 0)
                {
                    index %= itemCount;
                }
                else
                {
                    index = itemCount + ((index + 1) % itemCount) - 1;
                }
            }

            if (index < 0 || index >= SourceDatas.Count)
            {
                return;
            }

            var data = SourceDatas[index];
            item.name = index.ToString();
            bool isNewCreated = false;

            if (!binders.TryGetValue(item, out var binder))
            {
                isNewCreated = true;
                binder = binderCreator();
                binder.Controller = this;
                binder.Target = item;
                binder.Initialize();
                binders.Add(item, binder);
            }

            if (!isNewCreated)
            {
                if (!binder.resetFlag)
                {
                    binder.Reset();
                }
            }

            binder.Index = index;
            binder.Data = data;
            binder.Bind(data, index);
            binder.resetFlag = false;
        }
        #endregion

        #region Internal Fields
        protected bool isInit;
        private Func<ItemBinder> binderCreator;
        protected Dictionary<UObject, ItemBinder> binders =
            new Dictionary<UObject, ItemBinder>();
        #endregion
    }

    /// <summary>
    /// 数据绑定基类
    /// </summary>
    public abstract class ItemBinder
    {
        #region Public Properties
        /// <summary>
        /// 列表控制器
        /// </summary>
        public ScrollController Controller { get; internal set; }

        /// <summary>
        /// 目标节点
        /// </summary>
        public ScrollItem Target { get; internal set; }

        /// <summary>
        /// 当前绑定的数据
        /// </summary>
        public object Data { get; internal set; }

        /// <summary>
        /// 数据索引
        /// </summary>
        public int Index { get; internal set; } = -1;
        #endregion

        #region Public Methods
        /// <summary>
        ///   初始化 <see cref="T:System.Object" /> 类的新实例。
        /// </summary>
        public ItemBinder()
        {
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// 初始化
        /// </summary>
        protected internal abstract void Initialize();

        /// <summary>
        /// 卸载
        /// </summary>
        protected internal abstract void Release();

        /// <summary>
        /// 绑定数据到界面
        /// </summary>
        /// <param name="data">具体数据</param>
        /// <param name="index">数据索引</param>
        protected internal abstract void Bind(object data, int index);

        /// <summary>
        /// 重置数据
        /// </summary>
        protected internal virtual void Reset()
        {

        }
        #endregion

        #region Internal Fields
        internal bool resetFlag;
        #endregion
    }
}