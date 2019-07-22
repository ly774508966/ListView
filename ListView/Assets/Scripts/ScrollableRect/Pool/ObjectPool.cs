using System;
using System.Collections;
using System.Collections.Generic;

namespace Pool
{
    /// <summary>
    /// 对象池
    /// </summary>
    /// <typeparam name="T">对象的类型</typeparam>
    public class ObjectPool<T> : IEnumerable<KeyValuePair<T, bool>>
      where T : class
    {
        #region Events
        /// <summary>
        /// 当对象创建时
        /// </summary>
        public event Action<T> Created;

        /// <summary>
        /// 当对象销毁时
        /// </summary>
        public event Action<T> Destroying;

        /// <summary>
        /// 当对象生成时
        /// </summary>
        public event Action<T> Spawned;

        /// <summary>
        /// 当对象回收时
        /// </summary>
        public event Action<T> Recycled;

        /// <summary>
        /// 当对象建造时
        /// </summary>
        public event Action Builded;
        #endregion

        #region Properties
        /// <summary>
        /// 剩余数量
        /// </summary>
        public int ResidueCount
        {
            get
            {
                lock (syncObject)
                {
                    return residueCount;
                }
            }
        }

        /// <summary>
        /// 总容量
        /// </summary>
        public int Capacity
        {
            get
            {
                lock (syncObject)
                {
                    if (itemList == null)
                    {
                        return 0;
                    }

                    return itemList.Count;
                }
            }
            set
            {
                if (itemList == null || Template == null)
                {
                    return;
                }

                int count = Math.Max(0, value);
                if (count - itemList.Count <= 0)
                {
                    return;
                }

                lock (syncObject)
                {
                    Rebuild(count);
                }
            }
        }

        /// <summary>
        /// 模式
        /// </summary>
        public PoolMode Mode { get; set; } = PoolMode.Add;

        /// <summary>
        /// 对象模板
        /// </summary>
        public T Template { get; private set; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object UserData { get; set; } = null;
        #endregion

        #region Methods
        public ObjectPool(int count = 0)
        {
            if (typeof(T).IsValueType)
            {
                throw new NotSupportedException();
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            builder = t => (T)Activator.CreateInstance(typeof(T));
            originCount = count;
            itemList = new List<PoolItem>(originCount);
            if (originCount <= 0)
            {
                return;
            }

            Rebuild(count);
        }

        public ObjectPool(int count, Func<T, T> builder)
        {
            if (typeof(T).IsValueType)
            {
                throw new NotSupportedException();
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.builder = builder;
            originCount = count;
            itemList = new List<PoolItem>(originCount);
            if (originCount <= 0)
                return;
            Rebuild(count);
        }

        public ObjectPool(T template, int count = 0, Func<T, T> builder = null)
        {
            if (typeof(T).IsValueType)
            {
                throw new NotSupportedException();
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.builder = builder;
            Template = template ?? throw new ArgumentNullException();
            originCount = count;
            itemList = new List<PoolItem>(originCount);
            if (originCount <= 0)
            {
                return;
            }

            Rebuild(count);
        }

        public ObjectPool(T template, T[] array, Func<T, T> builder = null)
        {
            if (typeof(T).IsValueType)
            {
                throw new NotSupportedException();
            }

            if (array == null)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.builder = builder;
            Template = template ?? throw new ArgumentNullException();
            originCount = array.Length;
            itemList = new List<PoolItem>(originCount);
            foreach (var t in array)
            {
                if (t != null)
                {
                    PoolItem poolItem = new PoolItem(t);
                    ++residueCount;
                    itemList.Add(poolItem);
                    poolItem.Item?.OnCreated(this);
                    Created?.Invoke(poolItem.Instance);
                }
            }

            ++version;
            Builded?.Invoke();
        }

        public ObjectPool(T template, List<T> list, Func<T, T> builder = null)
        {
            if (typeof(T).IsValueType)
            {
                throw new NotSupportedException();
            }

            if (list == null)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.builder = builder;
            Template = template ?? throw new ArgumentNullException();
            originCount = list.Count;
            itemList = new List<PoolItem>(list.Count);
            foreach (var t in list)
            {
                if (t != null)
                {
                    PoolItem poolItem = new PoolItem(t);
                    ++residueCount;
                    itemList.Add(poolItem);
                    poolItem.Item?.OnCreated(this);
                    Created?.Invoke(poolItem.Instance);
                }
            }

            ++version;
            Builded?.Invoke();
        }

        /// <summary>
        /// 生成一个对象
        /// </summary>
        /// <returns></returns>
        public T Spawn()
        {
            lock (syncObject)
            {
                foreach (var poolItem in itemList)
                {
                    if (!poolItem.Used)
                    {
                        --residueCount;
                        poolItem.Used = true;
                        poolItem.Item?.OnSpawned();
                        Spawned?.Invoke(poolItem.Instance);

                        return poolItem.Instance;
                    }
                }

                switch (Mode)
                {
                    case PoolMode.Recovery:
                        while (!Recycle(itemList[stopIndex].Instance))
                        {
                            ++stopIndex;
                            if (stopIndex >= itemList.Count - 1)
                            {
                                stopIndex = 0;
                            }
                        }

                        ++stopIndex;
                        if (stopIndex >= itemList.Count - 1)
                        {
                            stopIndex = 0;
                        }

                        break;
                    case PoolMode.Multiple:
                        Rebuild(Math.Max(1, Capacity) * 2);

                        break;
                    case PoolMode.Add:
                        Rebuild(Capacity + Math.Max(1, originCount));

                        break;
                    default:
                        Rebuild(Capacity + (int)Mode);

                        break;
                }

                return Spawn();
            }
        }

        /// <summary>
        /// 回收一个对象
        /// </summary>
        /// <param name="instance">要回收的对象实例</param>
        /// <returns></returns>
        public bool Recycle(T instance)
        {
            lock (syncObject)
            {
                foreach (var poolItem in itemList)
                {
                    if (poolItem.Instance == instance)
                    {
                        if (!poolItem.Used)
                        {
                            return true;
                        }

                        ++residueCount;
                        poolItem.Used = false;
                        poolItem.Item?.OnRecycled();
                        Recycled?.Invoke(poolItem.Instance);

                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 全部回收
        /// </summary>
        public void RecycleAll()
        {
            lock (syncObject)
            {
                foreach (var poolItem in itemList)
                {
                    if (poolItem.Used)
                    {
                        ++residueCount;
                        poolItem.Used = false;
                        poolItem.Item?.OnRecycled();
                        Recycled?.Invoke(poolItem.Instance);
                    }
                }
            }
        }

        /// <summary>
        /// 销毁所有
        /// </summary>
        /// <param name="destroyTemplate">是否销毁模板</param>
        public void DestroyAll(bool destroyTemplate = true)
        {
            lock (syncObject)
            {
                if (itemList.Count == 0)
                {
                    return;
                }

                RecycleAll();
                foreach (var poolItem in itemList)
                {
                    if (poolItem.Instance != null)
                    {
                        poolItem.Item?.OnDestroying();
                        Destroying?.Invoke(poolItem.Instance);
                        DestroyImpl(poolItem.Instance);
                    }
                }

                if (itemList != null)
                {
                    itemList.Clear();
                    itemList.Capacity = 0;
                }

                if (destroyTemplate && Template != null)
                {
                    DestroyImpl(Template);
                    Template = default;
                }

                ++version;
                residueCount = 0;
            }
        }

        public IEnumerator<KeyValuePair<T, bool>> GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion

        #region Internal Methods
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        private void Rebuild(int count)
        {
            for (int count1 = itemList.Count; count1 < count; ++count1)
            {
                T instance = BuildImpl(Template);
                PoolItem poolItem = new PoolItem(instance);
                ++residueCount;
                poolItem.Instance = instance;
                itemList.Add(poolItem);
                poolItem.Item?.OnCreated(this);
                Created?.Invoke(poolItem.Instance);
            }

            Builded?.Invoke();
        }

        protected virtual T BuildImpl(T template)
        {
            T obj = default;
            if (builder != null)
            {
                return builder(template);
            }

            if (template == null)
            {
                return default;
            }

            if (template is ICloneable cloneable)
            {
                obj = cloneable.Clone() as T;
            }
            else if (template is IList || template is IDictionary)
            {
                obj = Activator.CreateInstance<T>();
            }

            if (obj == null)
            {
                throw new NotSupportedException(template.GetType().Name + " type can not support build by pool.");
            }

            return obj;
        }

        protected virtual void DestroyImpl(T instance)
        {
        }
        #endregion

        #region Internal Fields
        private object syncObject = new object();
        private int originCount;
        private int stopIndex;
        private int version = -1;
        private int residueCount;
        private List<PoolItem> itemList;
        private Func<T, T> builder;
        #endregion
       

        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<T, bool>>
        {
            private ObjectPool<T> pool;
            private int version;
            private int index;

            public KeyValuePair<T, bool> Current { get; private set; }

            public bool MoveNext()
            {
                if (version != pool.version)
                {
                    throw new InvalidOperationException("Object pool was modified, enumeration operation may not execute.");
                }

                if (index >= pool.Capacity)
                {
                    Current = new KeyValuePair<T, bool>();

                    return false;
                }

                PoolItem poolItem = pool.itemList[index];
                Current = new KeyValuePair<T, bool>(poolItem.Instance, poolItem.Used);
                ++index;

                return true;
            }

            public void Dispose()
            {
            }

            internal Enumerator(ObjectPool<T> pool)
            {
                this.pool = pool;
                version = pool.version;
                index = -1;
                Current = new KeyValuePair<T, bool>();
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index < 0 || index >= pool.Capacity)
                    {
                        throw new InvalidOperationException("Current is not valid");
                    }

                    return new KeyValuePair<T, bool>(Current.Key, Current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if (version != pool.version)
                {
                    throw new InvalidOperationException("Object pool was modified, enumeration operation may not execute.");
                }

                index = -1;
                Current = new KeyValuePair<T, bool>();
            }
        }

        /// <summary>
        /// 对象
        /// </summary>
        private class PoolItem
        {
            #region Properties
            /// <summary>
            /// 当前是否正在使用中
            /// </summary>
            public bool Used { get; set; }

            /// <summary>
            /// 对象类型实例
            /// </summary>
            public T Instance { get; set; }

            /// <summary>
            /// 池对象
            /// </summary>
            public IPoolItem Item;
            #endregion

            #region Internal Methods
            public PoolItem(T instance)
            {
                Used = false;
                Instance = instance;
                if (!(instance is IPoolItem))
                {
                    return;
                }

                Item = (IPoolItem) instance;
            }
            #endregion
        }
    }
}