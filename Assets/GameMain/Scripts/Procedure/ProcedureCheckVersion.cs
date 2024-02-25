//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    public class ProcedureCheckVersion : ProcedureBase {
        [SerializeField]private bool m_CheckVersionComplete = false;//检查资源是否完成
        [SerializeField]private bool m_NeedUpdateVersion = false;//需要更新资源
        [SerializeField]private VersionInfo m_VersionInfo = null;//版本信息(版本号,是否需要更新,Hash,压缩后的Hash等)

        [SerializeField]public override bool UseNativeDialog { get { return true; } }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);
            Debug.Log("可更新模式过来的,流程到了ProcedureCheckVersion检查版本-OnEnter");

            m_CheckVersionComplete = false;
            m_NeedUpdateVersion = false;
            m_VersionInfo = null;

            GameEntry.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
            GameEntry.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

            // 向服务器请求版本信息(url在BuildInfo.txt文件里)
            GameEntry.WebRequest.AddWebRequest(Utility.Text.Format(GameEntry.BuiltinData.BuildInfo.CheckVersionUrl, GetPlatformPath()), this);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown) {
            Debug.Log("可更新模式过来的,流程到了ProcedureCheckVersion检查版本-OnLeave");
            GameEntry.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
            GameEntry.Event.Unsubscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_CheckVersionComplete) return;
            Debug.Log("可更新模式过来的,流程到了ProcedureCheckVersion检查版本-OnUpdate");

            //走到这里说明检查版本的行为完毕了,接下来看是否需要更新
            if (m_NeedUpdateVersion) {
                //设置版本列表的长度,版本的MD5值,压缩后的长度和MD5值
                procedureOwner.SetData<VarInt32>("VersionListLength", m_VersionInfo.VersionListLength);
                procedureOwner.SetData<VarInt32>("VersionListHashCode", m_VersionInfo.VersionListHashCode);
                procedureOwner.SetData<VarInt32>("VersionListCompressedLength", m_VersionInfo.VersionListCompressedLength);
                procedureOwner.SetData<VarInt32>("VersionListCompressedHashCode", m_VersionInfo.VersionListCompressedHashCode);
                ChangeState<ProcedureUpdateVersion>(procedureOwner);
            }
            else {
                ChangeState<ProcedureVerifyResources>(procedureOwner);
            }
        }

        //跳转外部网页开始升级游戏
        private void GotoUpdateApp(object userData) {
            string url = null;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            url = GameEntry.BuiltinData.BuildInfo.WindowsAppUrl;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            url = GameEntry.BuiltinData.BuildInfo.MacOSAppUrl;
#elif UNITY_IOS
            url = GameEntry.BuiltinData.BuildInfo.IOSAppUrl;
#elif UNITY_ANDROID
            url = GameEntry.BuiltinData.BuildInfo.AndroidAppUrl;
#endif
            if (!string.IsNullOrEmpty(url)) {
                Application.OpenURL(url);
            }
        }

        private void OnWebRequestSuccess(object sender, GameEventArgs e) {
            WebRequestSuccessEventArgs ne = (WebRequestSuccessEventArgs)e;
            if (ne.UserData != this) return;

            // 请求游戏版本文件成功,准备解析版本信息
            byte[] versionInfoBytes = ne.GetWebResponseBytes();
            string versionInfoString = Utility.Converter.GetString(versionInfoBytes);
            m_VersionInfo = Utility.Json.ToObject<VersionInfo>(versionInfoString);
            if (m_VersionInfo == null) {
                Debug.LogError("下载到的版本信息为空,这还对比更新个屁");
                return;
            }

            Debug.Log(
                $"最新的游戏版本是 '{m_VersionInfo.LatestGameVersion} ({m_VersionInfo.InternalGameVersion.ToString()})', 此时本地游戏的版本是 '{Version.GameVersion} ({Version.InternalGameVersion.ToString()})'.");

            if (m_VersionInfo.ForceUpdateGame) {
                // 走到这说明游戏版本过低,需要强制更新游戏 
                GameEntry.UI.OpenDialog(new DialogParams {
                    Mode = 2,
                    Title = GameEntry.Localization.GetString("ForceUpdate.Title"),
                    Message = GameEntry.Localization.GetString("ForceUpdate.Message"),
                    ConfirmText = GameEntry.Localization.GetString("ForceUpdate.UpdateButton"),
                    OnClickConfirm = GotoUpdateApp,
                    CancelText = GameEntry.Localization.GetString("ForceUpdate.QuitButton"),
                    OnClickCancel = delegate(object userData) { UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit); },
                });

                return;
            }

            // 走到这说明游戏不需要更新,开始设置资源更新下载地址
            GameEntry.Resource.UpdatePrefixUri = Utility.Path.GetRegularPath(m_VersionInfo.UpdatePrefixUri);

            // 检查版本的行为已经完成
            m_CheckVersionComplete = true;
            m_NeedUpdateVersion = GameEntry.Resource.CheckVersionList(m_VersionInfo.InternalResourceVersion) == CheckVersionListResult.NeedUpdate;
        }

        private void OnWebRequestFailure(object sender, GameEventArgs e) {
            WebRequestFailureEventArgs ne = (WebRequestFailureEventArgs)e;
            if (ne.UserData != this) {
                return;
            }

            Debug.LogError($"下载版本信息失败,错误信息是 '{ne.ErrorMessage}'.");
        }

        private string GetPlatformPath() {
            switch (Application.platform) {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "MacOS";

                case RuntimePlatform.IPhonePlayer:
                    return "IOS";

                case RuntimePlatform.Android:
                    return "Android";

                default:
                    throw new System.NotSupportedException(Utility.Text.Format("平台 '{0}' 不被支持", Application.platform));
            }
        }
    }
}
