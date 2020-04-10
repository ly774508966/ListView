using System.Collections.Generic;
using UnityEngine;

namespace UGUI
{
    /// <summary>
    /// 池管理器
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        #region Public Properties
        /// <summary>
        /// 管理器实例
        /// </summary>
        public static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("PoolManager");
                    go.transform.localPosition = Vector3.zero;
                    instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        /// <summary>
        /// 池子根节点
        /// </summary>
        public Transform PoolsRoot { get; set; }
        #endregion

        #region Public Methods
        public bool TryGetPool(string poolName, out ObjectPool<GameObject> pool)
        {
            pool = null;
            if (pools == null)
            {
                return false;
            }

            if (pools.TryGetValue(poolName, out var info))
            {
                pool = info.pool;
            }

            return  pools.ContainsKey(poolName);
        }

        /// <summary>
        /// 获取或创建池子
        /// </summary>
        /// <param name="poolName"></param>
        /// <param name="template"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public ObjectPool<GameObject> GetOrCreatePool(string poolName,
            GameObject template, int count)
        {
            if (pools == null || !pools.TryGetValue(poolName, out var info))
            {
                if (pools == null)
                {
                    pools = new Dictionary<string, PoolInfo>();
                }

                info = new PoolInfo();
                var pool = new GameObject(poolName + "-Pool");
                pool.transform.SetParent(PoolsRoot ?? transform);
                info.root = pool.transform;
                pools.Add(poolName, info);

                info.pool = new ObjectPool<GameObject>(
                    template,
                    Mathf.Max(1, count),
                    t =>
                    {
                        var go = Instantiate(t, info.root);
                        go.name = t.name;
                        go.SetActive(false);

                        return go;
                    });

                info.pool.Recycled += go =>
                {
                    if (!go)
                    {
                        return;    
                    }

                    go.transform.SetParent(info.root, false);
                    if (go.activeInHierarchy)
                    {
                        go.SetActive(false);
                    }
                };
            }

            return info.pool;
        }

        /// <summary>
        /// 移除池子
        /// </summary>
        /// <param name="poolName">池名字</param>
        public void RemovePool(string poolName)
        {
            if (pools == null || !pools.TryGetValue(poolName, out var info))
            {
                return;
            }

            info.pool.DestroyAll(false);
            pools.Remove(poolName);
            Destroy(info.root.gameObject);
        }

        /// <summary>
        /// 移除所有
        /// </summary>
        public void RemoveAll()
        {
            if (pools != null)
            {
                foreach (var pair in pools)
                {
                    var pool = pair.Value.pool;

                    pool.DestroyAll(false);
                    Destroy(pair.Value.root.gameObject);
                }

                pools = null;
            }
        }
        #endregion

        #region Internal Methods
        private void OnDestroy()
        {
            RemoveAll();
        }
        #endregion

        #region Internal Fields
        private Dictionary<string, PoolInfo> pools;
        private static PoolManager instance;
        #endregion

        #region Internal Declarations
        private class PoolInfo
        {
            public ObjectPool<GameObject> pool;
            public Transform root;
        }
        #endregion
    }
}
