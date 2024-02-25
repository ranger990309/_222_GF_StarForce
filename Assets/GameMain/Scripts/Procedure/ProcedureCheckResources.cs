using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce {
    public class ProcedureCheckResources : ProcedureBase {
        private bool m_CheckResourcesComplete = false;//检查资源是否完成
        private bool m_NeedUpdateResources = false;//需要更新资源
        private int m_UpdateResourceCount = 0;//资源数量
        private long m_UpdateResourceTotalCompressedLength = 0L;//更新资源压缩总长度

        public override bool UseNativeDialog { get { return true; } }

        protected override void OnEnter(ProcedureOwner procedureOwner) {
            base.OnEnter(procedureOwner);

            m_CheckResourcesComplete = false;
            m_NeedUpdateResources = false;
            m_UpdateResourceCount = 0;
            m_UpdateResourceTotalCompressedLength = 0L;

            GameEntry.Resource.CheckResources(OnCheckResourcesComplete);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds) {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_CheckResourcesComplete) return;

            //需要更新资源
            if (m_NeedUpdateResources) {
                //需更新的资源的数量和压缩长度
                procedureOwner.SetData<VarInt32>("UpdateResourceCount", m_UpdateResourceCount);
                procedureOwner.SetData<VarInt64>("UpdateResourceTotalCompressedLength", m_UpdateResourceTotalCompressedLength);
                ChangeState<ProcedureUpdateResources>(procedureOwner);
            }
            else {
                ChangeState<ProcedurePreload>(procedureOwner);
            }
        }

        //检查资源完毕回调
        private void OnCheckResourcesComplete(int movedCount, int removedCount, int updateCount, long updateTotalLength,
            long updateTotalCompressedLength) {
            m_CheckResourcesComplete = true;
            m_NeedUpdateResources = updateCount > 0;
            m_UpdateResourceCount = updateCount;
            m_UpdateResourceTotalCompressedLength = updateTotalCompressedLength;
            Debug.Log($"检查资源完毕, '{updateCount.ToString()}' 资源需要更新, 压缩长度是 '{updateTotalCompressedLength.ToString()}', 不压缩长度是 '{updateTotalLength.ToString()}'.");
        }
    }
}
