using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UGUI
{
    /// <summary>
    /// 可配置的UI节点关联脚本
    /// </summary>
    public class UIElements : MonoBehaviour
    {
        #region Public Methods
        /// <summary>
        /// 获取关联数据
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="name">名字</param>
        /// <returns>返回关联数据</returns>
        public T GetObject<T>(string name) where T : UObject
        {
            if (keys == null || values == null)
            {
                return null;
            }

            int count = keys.Count;
            for (int i = 0; i < count; i++)
            {
                var key = keys[i];
                if (name == key)
                {
                    var result = values[i] as T;
                    if (result != null)
                    {
                        return result;
                    }

                    var go = values[i] as GameObject;

                    return go.GetComponent<T>();
                }
            }

            return default(T);
        }
        #endregion

        #region Internal Fields
        [SerializeField]
        private List<string> keys = null;
        [SerializeField]
        private List<UObject> values = null;
        #endregion
    }
}