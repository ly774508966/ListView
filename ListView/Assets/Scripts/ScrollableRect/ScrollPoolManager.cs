using System.Collections.Generic;
using UnityEngine;

namespace UGUI
{
    /// <summary>
    /// 列表专用对象池
    /// </summary>
    public class ScrollPoolManager
    {
        #region Properties
        public static ScrollPoolManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = new ScrollPoolManager();

                return instance;
            }
        }
        #endregion

        #region Methods
        public ScrollItem Spawn(int hashCode, string name, GameObject template)
        {
            var pool = CreateOrUpdatePoll(hashCode, name, template);

            return  pool.Spawn(name);
        }

        public void Recycle(int hashCode, ScrollItem item)
        {
            var pool = GetPoll(hashCode);
            if (pool == null)
            {
                Debug.LogError("没有当前池: " + hashCode);    

                return;
            }

            pool.Recycle(item);
        }

        public void Remove(int listHash)
        {
            if (poolDict.TryGetValue(listHash, out var pool))
            {
                pool.RemoveAll();
            }

            poolDict.Remove(listHash);
        }

        public void RemoveAll()
        {
            foreach (var pool in poolDict)
            {
                pool.Value.RemoveAll();
            }

            poolDict.Clear();
        }
        #endregion

        #region Internal Methods
        private ScrollPoll CreateOrUpdatePoll(int hashCode, string name, GameObject template)
        {
            if (poolDict.TryGetValue(hashCode, out var pool))
            {
                return pool;
            }

            pool = new ScrollPoll();
            pool.AddTemplate(name, template);
            poolDict.Add(hashCode, pool);

            return pool;
        }

        private ScrollPoll GetPoll(int hashCode)
        {
            if (poolDict.TryGetValue(hashCode, out var pool))
            {
                return pool;
            }

            return null;
        }
        #endregion

        #region Internal Fields
        private static ScrollPoolManager instance;
        private Dictionary<int, ScrollPoll> poolDict = new Dictionary<int, ScrollPoll>();
        #endregion

        #region Internal Declarations
        public class ScrollPoll
        {
            #region Methods
            public void AddTemplate(string name, GameObject template)
            {
                if (templateDict.TryGetValue(name, out _))
                {
                    templateDict[name] = template;
                }
                else
                {
                    templateDict.Add(name, template);
                }
            }

            public ScrollItem Spawn(string name)
            {
                if (objectDict.TryGetValue(name, out var list))
                {
                    return GetItem(name, list);
                }

                list = new List<ScrollItem>();
                objectDict.Add(name, list);

                return GetItem(name, list);
            }

            public void Recycle(ScrollItem item)
            {
                item.gameObject.SetActive(false);
            }

            public void RemoveAll()
            {
                foreach (var list in objectDict)
                {
                    foreach (var item in list.Value)
                    {
                        Object.Destroy(item);
                    }
                }

                objectDict.Clear();
            }
            #endregion

            #region Internal Methods
            private ScrollItem GetItem(string name, List<ScrollItem> items)
            {
                bool spawn = true;
                ScrollItem scrollItem = null;
                foreach (var item in items)
                {
                    if (!item.IsActive)
                    {
                        spawn = false;
                        scrollItem = item;

                        break;
                    }
                }

                if (spawn)
                {
                    var template = templateDict[name];
                    var go = Object.Instantiate(template, Vector3.zero, Quaternion.identity);
                    scrollItem = go.AddComponent<ScrollItem>();
                    items.Add(scrollItem);
                }

                return scrollItem;
            }
            #endregion

            #region Internal Fields
            private Dictionary<string, GameObject> templateDict = new Dictionary<string, GameObject>();
            private Dictionary<string, List<ScrollItem>> objectDict = new Dictionary<string, List<ScrollItem>>();
            #endregion
        }
        #endregion
    }
}