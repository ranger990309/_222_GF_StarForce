//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce {
    //处理数据
    public class BuiltinDataComponent : GameFrameworkComponent {
        [SerializeField] private TextAsset m_BuildInfoTextAsset = null;//(版本号,各平台Url)
        [SerializeField] private TextAsset m_DefaultDictionaryTextAsset = null;//中文繁文英文
        [SerializeField] private UpdateResourceForm m_UpdateResourceFormTemplate = null;//存储更新资源表单模板

        public UpdateResourceForm UpdateResourceFormTemplate {
            get => m_UpdateResourceFormTemplate;
            set => m_UpdateResourceFormTemplate = value;
        }

        private BuildInfo m_BuildInfo = null;//(版本号,各平台Url)

        public BuildInfo BuildInfo {
            get { return m_BuildInfo; }
        }

        public void InitBuildInfo() {
            if (m_BuildInfoTextAsset == null || string.IsNullOrEmpty(m_BuildInfoTextAsset.text)) {
                Log.Info("Build info can not be found or empty.");
                return;
            }

            m_BuildInfo = Utility.Json.ToObject<BuildInfo>(m_BuildInfoTextAsset.text);
            if (m_BuildInfo == null) {
                Log.Warning("Parse build info failure.");
                return;
            }
        }

        public void InitDefaultDictionary() {
            if (m_DefaultDictionaryTextAsset == null || string.IsNullOrEmpty(m_DefaultDictionaryTextAsset.text)) {
                Log.Info("Default dictionary can not be found or empty.");
                return;
            }

            if (!GameEntry.Localization.ParseData(m_DefaultDictionaryTextAsset.text)) {
                Log.Warning("Parse default dictionary failure.");
                return;
            }
        }
    }
}
