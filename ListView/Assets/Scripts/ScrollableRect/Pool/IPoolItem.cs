namespace Pool
{
    /// <summary>
    /// 池对象接口
    /// </summary>
    public interface IPoolItem
    {
        /// <summary>
        /// 当对象创建时调用
        /// </summary>
        bool OnCreated<T>(ObjectPool<T> pool) where T : class;

        /// <summary>
        /// 当对象销毁时调用
        /// </summary>
        bool OnDestroying();

        /// <summary>
        /// 当对象生成都指玩时调用
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// 当对象回收时调用
        /// </summary>
        void OnRecycled();
    }
}