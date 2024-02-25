//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Localization;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    public class ProcedureLaunch : ProcedureBase {
        public override bool UseNativeDialog { get { return true; } }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);

            // 构建信息：把Assets/GameMain/Configs/BuildInfo.txt里的数据读入游戏内,里面都是游戏版本的一些信息
            GameEntry.BuiltinData.InitBuildInfo();

            // 设置语言,查看语言配置表里设了啥,默认英语
            InitLanguageSettings();

            // 变体配置：根据使用的语言，通知底层加载对应的资源变体
            InitCurrentVariant();

            // 声音配置：根据用户配置数据，设置即将使用的声音选项
            InitSoundSettings();

            // 默认字典：加载默认字典文件 Assets/GameMain/Configs/DefaultDictionary.xml
            // 此字典文件记录了资源更新前使用的各种语言的字符串，会随 App 一起发布，故不可更新
            GameEntry.BuiltinData.InitDefaultDictionary();
            Debug.Log("流程到了ProcedureLaunch-OnEnter");
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            Debug.Log("流程到了ProcedureLaunch-OnUpdate");
            // 运行一帧即切换到 Splash 展示流程
            ChangeState<ProcedureSplash>(procedureOwner);
        }

        private void InitLanguageSettings() {
            //1 先检查是不是编辑器模式,是就直接用面板上设置的语言
            if (GameEntry.Base.EditorResourceMode && GameEntry.Base.EditorLanguage != Language.Unspecified) return;
            
            Language language = GameEntry.Localization.Language;
            //2 检查一下有没有语言相关的配置表,有就看看应该那游戏设置为什么语言
            if (GameEntry.Setting.HasSetting(Constant.Setting.Language)) {
                try {
                    string languageString = GameEntry.Setting.GetString(Constant.Setting.Language);
                    language = (Language)Enum.Parse(typeof(Language), languageString);
                }
                catch { }
            }

            //3 这四种语言之外就用英语
            if (language != Language.English
                && language != Language.ChineseSimplified
                && language != Language.ChineseTraditional
                && language != Language.Korean) {
                language = Language.English;

                GameEntry.Setting.SetString(Constant.Setting.Language, language.ToString());
                GameEntry.Setting.Save();
            }

            GameEntry.Localization.Language = language;
            Log.Info("语言配置完成,现在语言是 '{0}'.", language.ToString());
        }

        //给资源组件语言信息
        private void InitCurrentVariant() {
            //1 编辑器资源模式不使用 AssetBundle，也就没有变体了
            if (GameEntry.Base.EditorResourceMode) return;

            string currentVariant = null;
            switch (GameEntry.Localization.Language) {
                case Language.English:
                    currentVariant = "en-us";
                    break;

                case Language.ChineseSimplified:
                    currentVariant = "zh-cn";
                    break;

                case Language.ChineseTraditional:
                    currentVariant = "zh-tw";
                    break;

                case Language.Korean:
                    currentVariant = "ko-kr";
                    break;

                default:
                    currentVariant = "zh-cn";
                    break;
            }

            //???????????什么意思啊这里,拉资源要此时语言?,我觉得吧这里是设置那几百个翻译的语言资源,把对应语言的资源导进来
            GameEntry.Resource.SetCurrentVariant(currentVariant);
            Log.Info("?????资源组件要语言干嘛,我怀疑是给拉语言资源准备的");
        }

        //从c盘GameFrameworkSetting.dat拿到数据,设置好声音设置
        private void InitSoundSettings() {
            //游戏配置项的文件在哪啊(在这,但我不知道怎么查看里面的dat数据 C:\Users\Sumol\AppData\LocalLow\Game Framework\Star Force,就当是注册表吧)
            GameEntry.Sound.Mute("Music", GameEntry.Setting.GetBool(Constant.Setting.MusicMuted, false));
            GameEntry.Sound.SetVolume("Music", GameEntry.Setting.GetFloat(Constant.Setting.MusicVolume, 0.3f));
            GameEntry.Sound.Mute("Sound", GameEntry.Setting.GetBool(Constant.Setting.SoundMuted, false));
            GameEntry.Sound.SetVolume("Sound", GameEntry.Setting.GetFloat(Constant.Setting.SoundVolume, 1f));
            GameEntry.Sound.Mute("UISound", GameEntry.Setting.GetBool(Constant.Setting.UISoundMuted, false));
            GameEntry.Sound.SetVolume("UISound", GameEntry.Setting.GetFloat(Constant.Setting.UISoundVolume, 1f));
            Log.Info("初始化声音设置完成");
        }
    }
}
