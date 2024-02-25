//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    public class ProcedureInitResources : ProcedureBase {
        private bool m_InitResourcesComplete = false;

        public override bool UseNativeDialog { get { return true; } }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);

            Debug.Log("流程:单机模式过来的ProcedureInitResources-OnEnable");
            m_InitResourcesComplete = false;

            // 注意：使用单机模式并初始化资源前，需要先构建 AssetBundle 并复制到 StreamingAssets 中，否则会产生 HTTP 404 错误
            GameEntry.Resource.InitResources(OnInitResourcesComplete);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

                // 初始化资源未完成则继续等待
            if (!m_InitResourcesComplete) return;
            Debug.Log("流程:单机模式过来的ProcedureInitResources-OnUpdate,下一步去预加载资源");

            ChangeState<ProcedurePreload>(procedureOwner);
        }

        private void OnInitResourcesComplete() {
            m_InitResourcesComplete = true;
            Log.Info("初始化资源完成");
        }
    }
}
