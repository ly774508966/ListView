namespace Pool
{
    public enum PoolMode
    {
        /// <summary>
        /// 当对象池中数量不足时 回收旧的重复利用
        /// </summary>
        Recovery = -2,

        /// <summary>
        /// 当前数量*2
        /// </summary>
        Multiple = -1,

        /// <summary>
        /// 当前数量加上出事数量
        /// </summary>
        Add = 0,
    }
}