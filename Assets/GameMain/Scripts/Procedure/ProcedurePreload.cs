//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    //总结:
    public class ProcedurePreload : ProcedureBase {
        //要预加载的数据表
        public static readonly string[] DataTableNames = new string[] {
            "Aircraft", //飞机
            "Armor", //护甲
            "Asteroid", //小行星
            "Entity",
            "Music",
            "Scene",
            "Sound",
            "Thruster", //推进器
            "UIForm",
            "UISound",
            "Weapon",
        };

        //(key是文件地址,value是txt格式还是bytes格式),这里面的value一旦不为空就要开始转换场景了
        [SerializeField] private Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();

        public override bool UseNativeDialog { get { return true; } }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);
            Debug.Log("编辑器模式过来的,流程到了ProcedurePreload预加载流程-OnEnter");

            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            m_LoadedFlag.Clear();

            PreloadResources();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown) {
            Debug.Log("流程到了ProcedurePreload预加载流程-OnLeave");
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            //1 等字典里所有的value都为true再要开始转换场景了
            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag) {
                if (!loadedFlag.Value) return;
            }

            Debug.Log("流程到了ProcedurePreload-OnUpdate,准备去ProcedureChangeScene转换场景");

            //2 设置有限状态机的NextSceneId1数据为1,会转换场景到1场景也就是菜单场景
            procedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.Menu"));
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        //预加载资源
        private void PreloadResources() {
            //加载的是Assets/GameMain/Configs/DefaultConfig.txt,里面有游戏名 菜单场景的index 战斗场景的index
            LoadConfig("DefaultConfig");

            foreach (string dataTableName in DataTableNames) {
                LoadDataTable(dataTableName);
            }

            //Default是个翻译表来的
            LoadDictionary("Default");

            LoadFont("MainFont");
        }

        private void LoadConfig(string configName) {
            //加载的是txt资源
            //string configAssetName = AssetUtility.GetConfigAsset(configName, false);
            string configAssetName = Utility.Text.Format("Assets/GameMain/Configs/{0}.{1}", configName, false ? "bytes" : "txt");
            m_LoadedFlag.Add(configAssetName, false);
            GameEntry.Config.ReadData(configAssetName, this);
        }

        private void LoadDataTable(string dataTableName) {
            //string dataTableAssetName = AssetUtility.GetDataTableAsset(dataTableName, false);
            string dataTableAssetName = Utility.Text.Format("Assets/GameMain/DataTables/{0}.{1}", dataTableName, false ? "bytes" : "txt");
            m_LoadedFlag.Add(dataTableAssetName, false);
            GameEntry.DataTable.LoadDataTable(dataTableName, dataTableAssetName, this);
        }

        private void LoadDictionary(string dictionaryName) {
            //string dictionaryAssetName = AssetUtility.GetDictionaryAsset(dictionaryName, false);
            string dictionaryAssetName = Utility.Text.Format("Assets/GameMain/Localization/{0}/Dictionaries/{1}.{2}", GameEntry.Localization.Language,
                dictionaryName, false ? "bytes" : "xml");
            m_LoadedFlag.Add(dictionaryAssetName, false);
            GameEntry.Localization.ReadData(dictionaryAssetName, this);
        }

        private void LoadFont(string fontName) {
            //Font.MainFont
            m_LoadedFlag.Add(Utility.Text.Format("Font.{0}", fontName), false);
            //Assets/GameMain/Fonts/MainFont.ttf,50优先级,加载完回调(成功失败)
            GameEntry.Resource.LoadAsset(AssetUtility.GetFontAsset(fontName), Constant.AssetPriority.FontAsset, new LoadAssetCallbacks(
                (assetName, asset, duration, userData) => {
                    m_LoadedFlag[Utility.Text.Format("Font.{0}", fontName)] = true;
                    //设置UI主要的字体
                    UGuiForm.SetMainFont((Font)asset);
                    Debug.Log($"加载字体 '{fontName}' 成功.");
                },

                (assetName, status, errorMessage, userData) => {
                    Debug.LogError($"加载不了字体 '{fontName}' 地址是 '{assetName}' 错误信息是 '{errorMessage}'.");
                }));
        }

        //-------------------------------------
        //加载canfig成功回调
        private void OnLoadConfigSuccess(object sender, GameEventArgs e) {
            LoadConfigSuccessEventArgs ne = (LoadConfigSuccessEventArgs)e;
            if (ne.UserData != this) return;

            m_LoadedFlag[ne.ConfigAssetName] = true;
            Debug.Log($"加载 config '{ne.ConfigAssetName}' 成功.");
        }

        //加载config失败回调
        private void OnLoadConfigFailure(object sender, GameEventArgs e) {
            LoadConfigFailureEventArgs ne = (LoadConfigFailureEventArgs)e;
            if (ne.UserData != this) return;

            Debug.LogError($"加载不了 config '{ne.ConfigAssetName}' 地址是 '{ne.ConfigAssetName}' 错误信息是 '{ne.ErrorMessage}'.");
        }

        //加载DataTable成功回调
        private void OnLoadDataTableSuccess(object sender, GameEventArgs e) {
            LoadDataTableSuccessEventArgs ne = (LoadDataTableSuccessEventArgs)e;
            if (ne.UserData != this) return;

            m_LoadedFlag[ne.DataTableAssetName] = true;
            Debug.Log($"加载 data table '{ne.DataTableAssetName}' 成功.");
        }

        //加载DataTable失败回调
        private void OnLoadDataTableFailure(object sender, GameEventArgs e) {
            LoadDataTableFailureEventArgs ne = (LoadDataTableFailureEventArgs)e;
            if (ne.UserData != this) return;

            Debug.LogError($"加载不了 data table '{ne.DataTableAssetName}' 地址是 '{ne.DataTableAssetName}' 错误信息是 '{ne.ErrorMessage}'.");
        }

        //加载Dictionary失败回调????
        private void OnLoadDictionarySuccess(object sender, GameEventArgs e) {
            LoadDictionarySuccessEventArgs ne = (LoadDictionarySuccessEventArgs)e;
            if (ne.UserData != this) return;

            m_LoadedFlag[ne.DictionaryAssetName] = true;
            Debug.Log($"加载 dictionary '{ne.DictionaryAssetName}' 成功.");
        }

        //加载Dictionary失败回调????
        private void OnLoadDictionaryFailure(object sender, GameEventArgs e) {
            LoadDictionaryFailureEventArgs ne = (LoadDictionaryFailureEventArgs)e;
            if (ne.UserData != this) return;

            Debug.LogError(
                $"加载不了 dictionary '{ne.DictionaryAssetName}' 地址是 '{ne.DictionaryAssetName}' 错误信息是 '{ne.ErrorMessage}'.");
        }
    }
}
