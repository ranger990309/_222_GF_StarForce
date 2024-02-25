using GameFramework;
using GameFramework.Event;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    public class ProcedureUpdateResources : ProcedureBase {
        private bool m_UpdateResourcesComplete = false;//更新资源完毕
        private int m_UpdateCount = 0;//需要更新的数量
        private long m_UpdateTotalCompressedLength = 0L;//压缩总长度
        private int m_UpdateSuccessCount = 0;//成功更新的数量
        private List<UpdateLengthData> m_UpdateLengthData = new List<UpdateLengthData>();//正在更新的资源列表
        private UpdateResourceForm m_UpdateResourceForm = null;

        public override bool UseNativeDialog { get { return true; } }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);

            m_UpdateResourcesComplete = false;
            m_UpdateCount = procedureOwner.GetData<VarInt32>("UpdateResourceCount");
            procedureOwner.RemoveData("UpdateResourceCount");
            m_UpdateTotalCompressedLength = procedureOwner.GetData<VarInt64>("UpdateResourceTotalCompressedLength");
            procedureOwner.RemoveData("UpdateResourceTotalCompressedLength");
            m_UpdateSuccessCount = 0;
            m_UpdateLengthData.Clear();
            m_UpdateResourceForm = null;

            GameEntry.Event.Subscribe(ResourceUpdateStartEventArgs.EventId, OnResourceUpdateStart);
            GameEntry.Event.Subscribe(ResourceUpdateChangedEventArgs.EventId, OnResourceUpdateChanged);
            GameEntry.Event.Subscribe(ResourceUpdateSuccessEventArgs.EventId, OnResourceUpdateSuccess);
            GameEntry.Event.Subscribe(ResourceUpdateFailureEventArgs.EventId, OnResourceUpdateFailure);

            //1 游戏处于可联网模式的情况下,就打开UI面板(模式2UI,)
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {
                GameEntry.UI.OpenDialog(new DialogParams {
                    Mode = 2,
                    Title = GameEntry.Localization.GetString("UpdateResourceViaCarrierDataNetwork.Title"),
                    Message = GameEntry.Localization.GetString("UpdateResourceViaCarrierDataNetwork.Message"),
                    ConfirmText = GameEntry.Localization.GetString("UpdateResourceViaCarrierDataNetwork.UpdateButton"),
                    OnClickConfirm = StartUpdateResources,
                    CancelText = GameEntry.Localization.GetString("UpdateResourceViaCarrierDataNetwork.QuitButton"),
                    OnClickCancel = delegate(object userData) { UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit); },
                });

                return;
            }

            //2 开始更新资源
            StartUpdateResources(null);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown) {
            if (m_UpdateResourceForm != null) {
                Object.Destroy(m_UpdateResourceForm.gameObject);
                m_UpdateResourceForm = null;
            }

            GameEntry.Event.Unsubscribe(ResourceUpdateStartEventArgs.EventId, OnResourceUpdateStart);
            GameEntry.Event.Unsubscribe(ResourceUpdateChangedEventArgs.EventId, OnResourceUpdateChanged);
            GameEntry.Event.Unsubscribe(ResourceUpdateSuccessEventArgs.EventId, OnResourceUpdateSuccess);
            GameEntry.Event.Unsubscribe(ResourceUpdateFailureEventArgs.EventId, OnResourceUpdateFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_UpdateResourcesComplete) return;

            ChangeState<ProcedurePreload>(procedureOwner);
        }

        //点击UI面板的确定升级的按钮回调
        private void StartUpdateResources(object userData) {
            if (m_UpdateResourceForm == null) {
                m_UpdateResourceForm = Object.Instantiate(GameEntry.BuiltinData.UpdateResourceFormTemplate);
            }

            Debug.Log("正式开始升级资源...");
            GameEntry.Resource.UpdateResources(OnUpdateResourcesComplete);
        }

        private void RefreshProgress() {
            long currentTotalUpdateLength = 0L;
            for (int i = 0; i < m_UpdateLengthData.Count; i++) {
                currentTotalUpdateLength += m_UpdateLengthData[i].Length;
            }

            float progressTotal = (float)currentTotalUpdateLength / m_UpdateTotalCompressedLength;
            string descriptionText = GameEntry.Localization.GetString("UpdateResource.Tips", m_UpdateSuccessCount.ToString(),
                m_UpdateCount.ToString(), GetByteLengthString(currentTotalUpdateLength), GetByteLengthString(m_UpdateTotalCompressedLength),
                progressTotal, GetByteLengthString((int)GameEntry.Download.CurrentSpeed));
            m_UpdateResourceForm.SetProgress(progressTotal, descriptionText);
        }

        private string GetByteLengthString(long byteLength) {
            if (byteLength < 1024L) // 2 ^ 10
            {
                return Utility.Text.Format("{0} Bytes", byteLength);
            }

            if (byteLength < 1048576L) // 2 ^ 20
            {
                return Utility.Text.Format("{0:F2} KB", byteLength / 1024f);
            }

            if (byteLength < 1073741824L) // 2 ^ 30
            {
                return Utility.Text.Format("{0:F2} MB", byteLength / 1048576f);
            }

            if (byteLength < 1099511627776L) // 2 ^ 40
            {
                return Utility.Text.Format("{0:F2} GB", byteLength / 1073741824f);
            }

            if (byteLength < 1125899906842624L) // 2 ^ 50
            {
                return Utility.Text.Format("{0:F2} TB", byteLength / 1099511627776f);
            }

            if (byteLength < 1152921504606846976L) // 2 ^ 60
            {
                return Utility.Text.Format("{0:F2} PB", byteLength / 1125899906842624f);
            }

            return Utility.Text.Format("{0:F2} EB", byteLength / 1152921504606846976f);
        }

        //更新资源完成
        private void OnUpdateResourcesComplete(GameFramework.Resource.IResourceGroup resourceGroup, bool result) {
            if (result) {
                m_UpdateResourcesComplete = true;
                Debug.LogError("更新资源完成无错误");
            }
            else {
                Log.Error("更新资源完成出现错误!!!!!!!");
            }
        }

        //刚开始更新资源
        private void OnResourceUpdateStart(object sender, GameEventArgs e) {
            ResourceUpdateStartEventArgs ne = (ResourceUpdateStartEventArgs)e;

            //列表里没有这个资源才可以去下载这个资源
            for (int i = 0; i < m_UpdateLengthData.Count; i++) {
                if (m_UpdateLengthData[i].Name == ne.Name) {
                    Debug.LogWarning($"更新资源错误` '{ne.Name}' ");
                    m_UpdateLengthData[i].Length = 0;
                    RefreshProgress();
                    return;
                }
            }

            m_UpdateLengthData.Add(new UpdateLengthData(ne.Name));
        }

        //资源更新中....
        private void OnResourceUpdateChanged(object sender, GameEventArgs e) {
            ResourceUpdateChangedEventArgs ne = (ResourceUpdateChangedEventArgs)e;

            //持续更新资源列表里对应的资源的大小信息
            for (int i = 0; i < m_UpdateLengthData.Count; i++) {
                if (m_UpdateLengthData[i].Name == ne.Name) {
                    m_UpdateLengthData[i].Length = ne.CurrentLength;
                    RefreshProgress();//刷新进度条
                    return;
                }
            }

            Debug.LogError($"更新资源错误 '{ne.Name}' ");
        }

        //资源更新成功
        private void OnResourceUpdateSuccess(object sender, GameEventArgs e) {
            ResourceUpdateSuccessEventArgs ne = (ResourceUpdateSuccessEventArgs)e;
            Debug.Log($"更新资源成功 '{ne.Name}' .");

            //持续更新资源列表里对应的资源的大小信息等
            for (int i = 0; i < m_UpdateLengthData.Count; i++) {
                if (m_UpdateLengthData[i].Name == ne.Name) {
                    m_UpdateLengthData[i].Length = ne.CompressedLength;
                    m_UpdateSuccessCount++;
                    RefreshProgress();//进度条刷新
                    return;
                }
            }

            Debug.LogWarning($"更新资源错误 '{ne.Name}' ");
        }

        //更新资源失败(会不断重试)
        private void OnResourceUpdateFailure(object sender, GameEventArgs e) {
            ResourceUpdateFailureEventArgs ne = (ResourceUpdateFailureEventArgs)e;
            //超过规定的重试次数就不要重试了
            if (ne.RetryCount >= ne.TotalRetryCount) {
                Debug.LogError($"更新资源失败 '{ne.Name}' 地址是 '{ne.DownloadUri}' 错误信息是 '{ne.ErrorMessage}', 重试次数 '{ne.RetryCount.ToString()}'.超限制了,不要再试了");
                return;
            }
            else {
                Debug.Log($"更新资源 '{ne.Name}' 失败,地址是 '{ne.DownloadUri}' 错误信息是 '{ne.ErrorMessage}', 重试次数 '{ne.RetryCount.ToString()}'.");
            }

            //走到这说明更新资源失败但可重试次数还没超过规定的数量,还可以重试
            for (int i = 0; i < m_UpdateLengthData.Count; i++) {
                if (m_UpdateLengthData[i].Name == ne.Name) {
                    m_UpdateLengthData.Remove(m_UpdateLengthData[i]);//
                    RefreshProgress();
                    return;
                }
            }

            Log.Warning("Update resource '{0}' is invalid.", ne.Name);
        }

        private class UpdateLengthData {
            private readonly string m_Name;

            public UpdateLengthData(string name) {
                m_Name = name;
            }

            public string Name { get { return m_Name; } }

            public int Length { get; set; }
        }
    }
}
