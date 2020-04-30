using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UGUI.Test.View
{
    public class DemoBase : MonoBehaviour
    {
        /// <summary>
        /// 数据创建器
        /// </summary>
        protected virtual Func<ItemBinder> ItemBinder { get; set; }

        /// <summary>
        /// 数据集
        /// </summary>
        protected virtual List<object> DataList { get; set; }

        /// <summary>
        /// 初始化模式 1 默认初始化 其他代表用控制器初始化
        /// </summary>
        protected virtual int Mode { get; set; } = 1;

        private void Awake()
        {
            scrollToButton?.onClick.AddListener(OnScrollClick);
            addItemButton?.onClick.AddListener(OnAddItemClick);
            setCountButton?.onClick.AddListener(OnSetCountClick);
            backButton?.onClick.AddListener(OnBackBtnClicked);
        }

        protected virtual void OnEnable()
        {
            if (Mode == 1)
            {
                ListView.FillItemData = FillData;
            }
            else
            {
                listController = new ListController();
                listController.Initialize(ListView, ItemBinder, DataList);
            }
        }

        private void OnDestroy()
        {
            scrollToButton?.onClick.RemoveListener(OnScrollClick);
            addItemButton?.onClick.RemoveListener(OnAddItemClick);
            setCountButton?.onClick.RemoveListener(OnSetCountClick);
            backButton?.onClick.RemoveListener(OnBackBtnClicked);
        }

        protected virtual void FillData(ScrollItem item, int index)
        {

        }

        private void OnSetCountClick()
        {
            if (ListView != null)
            {
                int.TryParse(setCountInput.text, out var count);
                ListView.TotalCount = count;
                ListView.FillCells();
            }
        }

        private void OnAddItemClick()
        {
            ListView.TotalCount++;
            var index = ListView.GetFirstShow().Index;
            ListView.FillCells(index);
        }

        private void OnScrollClick()
        {
            if (ListView != null)
            {
                int.TryParse(scrollToInput.text, out var count);
                ListView.ScrollToView(count, true);
            }
        }

        private void OnBackBtnClicked()
        {
            SceneManager.LoadScene(Init.initSceneName, LoadSceneMode.Single);
        }

        protected static Sprite GetSprite()
        {
            var spriteName = "grid_flower_200_" + Random.Range(0, 24);

            return Resources.Load<Sprite>("Texture/Pic/" + spriteName);
        }

        protected ListController listController;

        [SerializeField]
        protected ListView ListView;
        [SerializeField]
        private Button scrollToButton;
        [SerializeField]
        private Button addItemButton;
        [SerializeField]
        private Button setCountButton;
        [SerializeField]
        private InputField scrollToInput;
        [SerializeField]
        private InputField addItemInput;
        [SerializeField]
        private InputField setCountInput;
        [SerializeField]
        private Button backButton;
    }
}