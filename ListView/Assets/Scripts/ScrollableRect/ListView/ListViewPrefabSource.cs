using System;
using UnityEngine;

namespace UGUI.ListView
{
    /// <summary>
    /// 列表预制数据
    /// </summary>
    [Serializable]
    public class ListViewPrefabSource
    {
        #region Public Properties
        /// <summary>
        /// 预制体名字
        /// </summary>
        public string PrefabName => prefabName;

        /// <summary>
        /// 池子大小
        /// </summary>
        public int PoolSize => poolSize;

        /// <summary>
        /// 模板
        /// </summary>
        public GameObject Template => template;
        #endregion

        #region Internal Fields
        [SerializeField]
        public string prefabName;
        [SerializeField]
        public int poolSize = 1;
        [SerializeField]
        public GameObject template;
        #endregion
    }
}
