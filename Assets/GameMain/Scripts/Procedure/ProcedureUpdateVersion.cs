//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    public class ProcedureUpdateVersion : ProcedureBase {
        private bool m_UpdateVersionComplete = false;
        private UpdateVersionListCallbacks m_UpdateVersionListCallbacks = null;

        public override bool UseNativeDialog { get { return true; } }

        protected override void OnInit(ProcedureOwner procedureOwner) {
            base.OnInit(procedureOwner);

            m_UpdateVersionListCallbacks = new UpdateVersionListCallbacks(OnUpdateVersionListSuccess, OnUpdateVersionListFailure);
        }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);
            Debug.Log("流程到了Procedure UpdateVersion-OnEnter");
            
            m_UpdateVersionComplete = false;

            //更新版本资源,(更新为什么需要资源的长度啊Hash啊什么的)
            GameEntry.Resource.UpdateVersionList(procedureOwner.GetData<VarInt32>("VersionListLength"),
                procedureOwner.GetData<VarInt32>("VersionListHashCode"), procedureOwner.GetData<VarInt32>("VersionListCompressedLength"),
                procedureOwner.GetData<VarInt32>("VersionListCompressedHashCode"), m_UpdateVersionListCallbacks);
            procedureOwner.RemoveData("VersionListLength");
            procedureOwner.RemoveData("VersionListHashCode");
            procedureOwner.RemoveData("VersionListCompressedLength");
            procedureOwner.RemoveData("VersionListCompressedHashCode");
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_UpdateVersionComplete) return;

            ChangeState<ProcedureVerifyResources>(procedureOwner);
        }

        private void OnUpdateVersionListSuccess(string downloadPath, string downloadUri) {
            m_UpdateVersionComplete = true;
            Log.Info("更新资源列表成功 '{0}'.", downloadUri);
        }

        private void OnUpdateVersionListFailure(string downloadUri, string errorMessage) {
            Log.Warning("更新资源列表失败 '{0}' 失败信息是 '{1}'.", downloadUri, errorMessage);
        }
    }
}
