using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UGUI
{
    /// <summary>
    /// 滑动列表控制器
    /// </summary>
    public class ListController : ScrollController
    {
        #region Public Properties
        /// <summary>
        /// 默认列表项名字
        /// </summary>
        public string DefaultItemName { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// 列表项提供者
        /// <para>项索引</para>
        /// <para>索引对应的数据</para>
        /// <para>返回对应项的路径</para>
        /// </summary>
        public Func<int, object, string> ListViewItemProvider;
        #endregion

        #region Public Methods
        /// <summary>
        /// 重置数据并重新初始化
        /// </summary>
        /// <param name="datas">重新绑定的数据</param>
        public override void ResetData(IList datas)
        {
            SourceDatas = datas;

            if (!isInit)
            {
                return;
            }

            Target.TotalCount = SourceDatas.Count;
            Target.FillCells();
        }

        /// <summary>
        /// 通过路径添加配置项
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public string AddItemByPath(string path)
        {
            if (!isInit)
            {
                return string.Empty;
            }

            var index = path.LastIndexOf('/');
            var name = path.Substring(index + 1);

            var data = new ListViewPrefabSource();
            data.poolSize = 1;
            data.prefabName = name;

            var prefab = Resources.Load<GameObject>(path);
            if (!prefab)
            {
                Debug.LogError("加载Prefab失败 :" + path);

                return null;
            }


            prefab.name = name;
            data.template = prefab;
            prefabConfigs.Add(data);

            return name;
        }
        #endregion

        #region Internal Methods
        protected internal override void OnInitialize()
        {
            if (Target is ListView view)
            {
                prefabConfigs = view.PrefabSources;
            }
            else
            {
                Debug.LogError("获取列表预制数据失败!");
            }

            Target.GetCustomItem = ListItemProvider;
            if (string.IsNullOrEmpty(DefaultItemName))
            {
                DefaultItemName = prefabConfigs.Count > 0
                    ? prefabConfigs[0].prefabName
                    : string.Empty;
            }

            if (!Target.gameObject.activeSelf)
            {
                Target.gameObject.SetActive(true);
            }
        }

        private string ListItemProvider(int index)
        {
            if (ListViewItemProvider == null)
            {
                return string.Empty;
            }

            var data = SourceDatas[index];

            return ListViewItemProvider.Invoke(index, data);
        }
        #endregion

        #region Internal Fields
        private List<ListViewPrefabSource> prefabConfigs;
        #endregion
    }
}
