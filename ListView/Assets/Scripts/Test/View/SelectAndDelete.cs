using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UGUI.Test.View
{
    public class SelectAndDelete : DemoBase
    {
        /// <summary>
        /// 初始化模式 1 默认初始化 其他代表用控制器初始化
        /// </summary>
        protected override int Mode { get; set; } = 2;

        /// <summary>
        /// 数据创建器
        /// </summary>
        protected override Func<ItemBinder> ItemBinder { get; set; } = () => new ItemCreator();

        /// <summary>
        /// 数据集
        /// </summary>
        protected override List<object> DataList { get; set; } = new List<object>();

        protected override void OnEnable()
        {
            for (int i = 0; i < 30; i++)
            {
                var data = ItemData.CreateData(i);
                DataList.Add(data);
            }

            base.OnEnable();
            delete.onClick.AddListener(DeleteClick);
            selectAll.onClick.AddListener(SelectAllClick);
            cancelAll.onClick.AddListener(CancelAllClick);
        }

        private void DeleteClick()
        {
            for (int i = DataList.Count - 1; i >= 0; i--)
            {
                var data = DataList[i] as ItemData;
                if (data.IsOn)
                {
                    DataList.RemoveAt(i);
                }
            }

            listController.Target.RefreshCells();
        }

        private void CancelAllClick()
        {
            foreach (ItemData data in DataList)
            {
                data.IsOn = false;
            }

            listController.Target.RefreshCells();
        }

        private void SelectAllClick()
        {
            foreach (ItemData data in DataList)
            {
                data.IsOn = true;
            }

            listController.Target.RefreshCells();
        }

        [SerializeField]
        private Button selectAll;
        [SerializeField]
        private Button cancelAll;
        [SerializeField]
        private Button delete;
    }

    public class ItemCreator : ItemBinder
    {
        #region Internal Methods
        /// <summary>
        /// 初始化
        /// </summary>
        protected internal override void Initialize()
        {
            var elements = Target.GetComponent<UIElements>();
            icon = elements.GetObject<Image>("Icon");
            name = elements.GetObject<Text>("Name");
            desc = elements.GetObject<Text>("Desc");
            toggle = elements.GetObject<Toggle>("Toggle");
            toggle.onValueChanged.AddListener(OnToggle);
        }

        private void OnToggle(bool value)
        {
            if (Data is ItemData data)
            {
                data.IsOn = value;
            }
        }

        /// <summary>
        /// 卸载
        /// </summary>
        protected internal override void Release()
        {
            toggle.onValueChanged.RemoveListener(OnToggle);
        }

        /// <summary>
        /// 绑定数据到界面
        /// </summary>
        /// <param name="data"> 具体数据</param>
        /// <param name="index">数据索引</param>
        protected internal override void Bind(object data, int index)
        {
            if (data is ItemData item)
            {
                name.text = item.Name;
                desc.text = item.Desc;
                icon.sprite = item.Icon;
                toggle.isOn = item.IsOn;
            }
        }
        #endregion

        #region Internal Fields
        private Text name;
        private Text desc;
        private Image icon;
        private Toggle toggle;
        #endregion
    }

    public class ItemData
    {
        public string Name { get; set; }

        public string Desc { get; set; }

        public Sprite Icon { get; set; }

        public bool IsOn { get; set; }

        public static ItemData CreateData(int index)
        {
            ItemData data = new ItemData();
            data.Name = "Item" + index;
            data.Desc = "Item Desc " + index;
            data.IsOn = false;
            var spriteName = "grid_flower_200_" + Random.Range(0, 24);
            data.Icon = Resources.Load<Sprite>("Texture/Pic/" + spriteName);

            return data;
        }
    }
}