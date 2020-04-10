using System;
using System.Collections;
using System.Collections.Generic;

namespace UGUI
{
    public enum PoolMode
    {
        /// <summary>
        /// When the pool content is not enough, recycling old to new use.
        /// </summary>
        Recovery = -2,

        /// <summary>
        /// Multiply the current number by 2.
        /// </summary>
        Multiple = -1,

        /// <summary>
        /// The current amount plus the initial number.
        /// </summary>
        Add = 0,
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IPoolItem
    {
        /// <summary>
        /// When object is created, this method is triggered.
        /// </summary>
        bool OnCreated<T>(ObjectPool<T> pool) where T : class;

        /// <summary>
        /// When object is destroy, this method is triggered.
        /// </summary>
        bool OnDestroying();

        /// <summary>
        /// When object is generated, this method is triggered.
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// When object is recycled, this method is triggered.
        /// </summary>
        void OnRecycled();
    }

    public class ObjectPool<T> : IEnumerable<KeyValuePair<T, bool>>
        where T : class
    {
        #region Properties
        /// <summary>
        /// 
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
        /// 
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

                int realCapacity = Math.Max(0, value);
                int needAddCount = realCapacity - itemList.Count;
                if (needAddCount <= 0)
                {
                    return;
                }

                lock (syncObject)
                {
                    Rebuild(realCapacity);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Mode { get; set; } = (int)PoolMode.Add;

        /// <summary>
        /// 
        /// </summary>
        public T Template { get; private set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public object UserData { get; set; } = null;
        #endregion

        #region Events
        public event Action<T> Created;
        public event Action<T> Destroying;
        public event Action<T> Spawned;
        public event Action<T> Recycled;
        public event Action Builded;
        #endregion

        #region Methods
        public ObjectPool(int count = 0)
        {
            builder = t => Activator.CreateInstance<T>();

            originCount = count;
            itemList = new List<PoolItem>(originCount);

            if (originCount > 0)
            {
                Rebuild(count);
            }
        }

        public ObjectPool(int count, Func<T, T> builder)
        {
            this.builder = builder;
            
            originCount = count;
            itemList = new List<PoolItem>(originCount);

            if (originCount > 0)
            {
                Rebuild(count);
            }
        }

        public ObjectPool(T template, int count = 0, Func<T, T> builder = null)
        {
            this.builder = builder;

            Template = template;
            originCount = count;
            itemList = new List<PoolItem>(originCount);

            if (originCount > 0)
            {
                Rebuild(count);
            }
        }
        
        public ObjectPool(T template, T[] array, Func<T, T> builder = null)
        {
            this.builder = builder;
            
            Template = template;
            originCount = array.Length;
            itemList = new List<PoolItem>(originCount);
            for (int index = 0; index < array.Length; ++index)
            {
                if (array[index] == null)
                {
                    continue;
                }

                PoolItem item = new PoolItem(array[index]);

                ++residueCount;
                itemList.Add(item);

                item.poolItem?.OnCreated(this);
                Created?.Invoke(item.instance);
            }

            version++;

            Builded?.Invoke();
        }
        
        public ObjectPool(T template, List<T> list, Func<T, T> builder = null)
        {
            this.builder = builder;

            Template = template;
            originCount = list.Count;
            itemList = new List<PoolItem>(list.Count);
            for (int index = 0; index < list.Count; ++index)
            {
                if (list[index] == null)
                {
                    continue;
                }

                PoolItem item = new PoolItem(list[index]);

                ++residueCount;
                itemList.Add(item);

                item.poolItem?.OnCreated(this);
                Created?.Invoke(item.instance);
            }

            version++;

            Builded?.Invoke();
        }
        
        public T Spawn()
        {
            lock (syncObject)
            {
                for (int i = 0; i < itemList.Count; ++i)
                {
                    PoolItem item = itemList[i];
                    if (!item.used)
                    {
                        --residueCount;
                        item.used = true;

                        item.poolItem?.OnSpawned();
                        Spawned?.Invoke(item.instance);

                        return item.instance;
                    }
                }

                if (Mode == (int)PoolMode.Add)
                {
                    Rebuild(Capacity + Math.Max(1, originCount));
                }
                else if (Mode == (int)PoolMode.Multiple)
                {
                    Rebuild(Math.Max(1, Capacity) * 2);
                }
                else if (Mode == (int)PoolMode.Recovery)
                {
                    while (!Recycle(itemList[stopIndex].instance))
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
                }
                else
                {
                    Rebuild(Capacity + Mode);
                }

                return Spawn();
            }
        }

        public bool Recycle(T instance)
        {
            if(instance == null)
            {
                return false;
            }

            lock (syncObject)
            {
                for (int i = 0; i < itemList.Count; ++i)
                {
                    PoolItem item = itemList[i];
                    if (item.instance != instance)
                    {
                        continue;
                    }

                    if (!item.used)
                    {
                        return true;
                    }

                    ++residueCount;
                    item.used = false;

                    item.poolItem?.OnRecycled();
                    Recycled?.Invoke(item.instance);

                    return true;
                }

                return false;
            }
        }

        public void RecycleAll()
        {
            lock (syncObject)
            {
                for (int i = 0; i < itemList.Count; ++i)
                {
                    PoolItem item = itemList[i];
                    if (!item.used)
                    {
                        continue;
                    }

                    ++residueCount;
                    item.used = false;

                    item.poolItem?.OnRecycled();
                    Recycled?.Invoke(item.instance);
                }
            }
        }

        public void DestroyAll(bool destroyTemplate = true)
        {
            lock (syncObject)
            {
                if (itemList.Count == 0)
                {
                    return;
                }

                RecycleAll();

                for (int i = 0; i < itemList.Count; ++i)
                {
                    PoolItem item = itemList[i];
                    if (item.instance != null)
                    {
                        item.poolItem?.OnDestroying();
                        Destroying?.Invoke(item.instance);
                        DestroyImpl(item.instance);
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
                    Template = null;
                }

                version++;
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
            for (int i = itemList.Count; i < count; ++i)
            {
                T instance = BuildImpl(Template);

                PoolItem item = new PoolItem(instance);

                ++residueCount;
                item.instance = instance;
                itemList.Add(item);

                item.poolItem?.OnCreated(this);
                Created?.Invoke(item.instance);
            }

            Builded?.Invoke();
        }

        protected virtual T BuildImpl(T template)
        {
            T instance = null;
            if (builder != null)
            {
                return builder(template);
            }

            if (template == null)
            {
                return null;
            }

            if (template is ICloneable)
            {
                ICloneable cloneable = template as ICloneable;
                instance = cloneable.Clone() as T;
            }
            else if (template is IList || template is IDictionary)
            {
                instance = (T)Activator.CreateInstance(typeof(T), template);
            }

            if (instance == null)
            {
                throw new NotSupportedException(template.GetType().Name + " type can not support build by pool.");
            }

            return instance;
        }

        protected virtual void DestroyImpl(T instance)
        {

        }
        #endregion

        #region Internal Fields
        private object syncObject = new object();
        private int originCount = -1;
        private int stopIndex = 0;
        private int version = -1;
        private int residueCount = 0;
        private List<PoolItem> itemList = null;
        private Func<T, T> builder = null;
        #endregion

        #region Internal Declarations
        private class PoolItem
        {
            public bool used;
            public T instance;
            public IPoolItem poolItem;
            
            public PoolItem(T instance)
            {
                used = false;
                this.instance = instance;
                Type itemType = instance.GetType();
                if (typeof(IPoolItem).IsAssignableFrom(itemType))
                {
                    poolItem = instance as IPoolItem;
                }
            }
        }
        
        [Serializable]
        public struct Enumerator : IEnumerator<KeyValuePair<T, bool>>
        {
            #region Properties
            public KeyValuePair<T, bool> Current
            {
                get { return current; }
            }
            #endregion

            #region Methods
            public bool MoveNext()
            {
                if (version != pool.version)
                {
                    throw new InvalidOperationException(
                        "Object pool was modified, enumeration operation may not execute.");
                }

                if (index >= pool.Capacity)
                {
                    current = new KeyValuePair<T, bool>();

                    return false;
                }

                PoolItem item = pool.itemList[index];

                current = new KeyValuePair<T, bool>(item.instance, item.used);
                index++;

                return true;
            }

            public void Dispose()
            {
            }
            #endregion

            #region Internal Methods
            internal Enumerator(ObjectPool<T> pool)
            {
                this.pool = pool;
                version = pool.version;
                index = -1;
                current = new KeyValuePair<T, bool>();
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index < 0 || index >= pool.Capacity)
                    {
                        throw new InvalidOperationException("Current is not valid");
                    }

                    return new KeyValuePair<T, bool>(current.Key, current.Value);
                }
            }

            void IEnumerator.Reset()
            {
                if (version != pool.version)
                {
                    throw new InvalidOperationException(
                        "Object pool was modified, enumeration operation may not execute.");
                }

                index = -1;
                current = new KeyValuePair<T, bool>();
            }
            #endregion

            #region Internal Fields
            private ObjectPool<T> pool;
            private int version;
            private int index;
            private KeyValuePair<T, bool> current;
            #endregion
        }
        #endregion
    }
}
