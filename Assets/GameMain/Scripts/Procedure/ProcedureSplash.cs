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
    //溅射闪光动画
    public class ProcedureSplash : ProcedureBase {
        public override bool UseNativeDialog { get { return true; } }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            // TODO: 这里可以播放一个 Splash 动画
            // ...

            if (GameEntry.Base.EditorResourceMode) {
                // 编辑器模式
                Debug.Log("流程到了ProcedureSplash-OnUpdate 编辑器模式,接下来去预加载ProcedurePreload");
                ChangeState<ProcedurePreload>(procedureOwner);
            }
            else if (GameEntry.Resource.ResourceMode == ResourceMode.Package) {
                // 单机模式
                Debug.Log("流程到了ProcedureSplash-OnUpdate 包资源模式(单机模式),接下来去预加载资源ProcedureInitResources");
                ChangeState<ProcedureInitResources>(procedureOwner);
            }
            else {
                // 可更新模式
                Debug.Log("流程到了ProcedureSplash-OnUpdate 可更新模式,接下来去检查版本ProcedureCheckVersion");
                ChangeState<ProcedureCheckVersion>(procedureOwner);
            }
        }
    }
}
