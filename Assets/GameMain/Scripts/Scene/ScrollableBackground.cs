//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;

namespace StarForce {
    //可滚动的背景
    public class ScrollableBackground : MonoBehaviour {
        [SerializeField] private float m_ScrollSpeed = -0.25f; //背景滚动速度

        [SerializeField] private float m_TileSize = 30f;

        [SerializeField] private BoxCollider m_VisibleBoundary = null; //可见边界

        [SerializeField] private BoxCollider m_PlayerMoveBoundary = null; // 玩家可移动的边界

        [SerializeField] private BoxCollider m_EnemySpawnBoundary = null; //敌人生成的边界

        private Transform m_CachedTransform = null; //缓存位置
        private Vector3 m_StartPosition = Vector3.zero; //背景的初始位置

        private void Start() {
            m_CachedTransform = transform;
            m_StartPosition = m_CachedTransform.position;
        }

        private void Update() {
            float newPosition = Mathf.Repeat(Time.time * m_ScrollSpeed, m_TileSize);
            m_CachedTransform.position = m_StartPosition + Vector3.forward * newPosition;
        }

        public BoxCollider VisibleBoundary {
            get { return m_VisibleBoundary; }
        }

        public BoxCollider PlayerMoveBoundary {
            get { return m_PlayerMoveBoundary; }
        }

        public BoxCollider EnemySpawnBoundary {
            get { return m_EnemySpawnBoundary; }
        }
    }
}
