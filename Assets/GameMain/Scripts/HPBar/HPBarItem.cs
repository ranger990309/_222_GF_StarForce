﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace StarForce {
    public class HPBarItem : MonoBehaviour {
        private const float AnimationSeconds = 0.3f; //HP条动画的时间
        private const float KeepSeconds = 0.4f; //HP条保持显示时间
        private const float FadeOutSeconds = 0.3f; //HP条淡出的时间

        [SerializeField] private Slider m_HPBar = null;

        private Canvas m_ParentCanvas = null;
        private RectTransform m_CachedTransform = null;
        private CanvasGroup m_CachedCanvasGroup = null;
        private Entity m_Owner = null;
        private int m_OwnerId = 0;

        public Entity Owner {
            get { return m_Owner; }
        }

        public void Init(Entity owner, Canvas parentCanvas, float fromHPRatio, float toHPRatio) {
            if (owner == null) {
                Log.Error("Owner is invalid.");
                return;
            }

            m_ParentCanvas = parentCanvas;

            gameObject.SetActive(true);
            StopAllCoroutines();

            m_CachedCanvasGroup.alpha = 1f;
            //我懂了,如果子弹第1第2颗都打中一个敌机,那血条不用走这里面(意味着不用创建新的血条),若第1和2打中不同的就要创建新的血条
            if (m_Owner != owner || m_OwnerId != owner.Id) {
                m_HPBar.value = fromHPRatio;
                m_Owner = owner;
                m_OwnerId = owner.Id;
            }

            Refresh();

            StartCoroutine(HPBarCo(toHPRatio, AnimationSeconds, KeepSeconds, FadeOutSeconds));
        }

        //刷新HP条
        public bool Refresh() {
            if (m_CachedCanvasGroup.alpha <= 0f) {
                return false;
            }
            
            if (m_Owner != null && Owner.Available && Owner.Id == m_OwnerId) {
                Vector3 worldPosition = m_Owner.CachedTransform.position + Vector3.forward;
                Vector3 screenPosition = GameEntry.Scene.MainCamera.WorldToScreenPoint(worldPosition);

                Vector2 position;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_ParentCanvas.transform, screenPosition,
                        m_ParentCanvas.worldCamera, out position)) {
                    m_CachedTransform.localPosition = position;
                }
            }

            return true;
        }

        //重置HP条
        public void Reset() {
            StopAllCoroutines();
            m_CachedCanvasGroup.alpha = 1f;
            m_HPBar.value = 1f;
            m_Owner = null;
            gameObject.SetActive(false);
        }

        private void Awake() {
            m_CachedTransform = GetComponent<RectTransform>();
            if (m_CachedTransform == null) {
                Log.Error("RectTransform is invalid.");
                return;
            }

            m_CachedCanvasGroup = GetComponent<CanvasGroup>();
            if (m_CachedCanvasGroup == null) {
                Log.Error("CanvasGroup is invalid.");
                return;
            }
        }

        //淡入淡出
        private IEnumerator HPBarCo(float value, float animationDuration, float keepDuration, float fadeOutDuration) {
            yield return m_HPBar.SmoothValue(value, animationDuration);
            yield return new WaitForSeconds(keepDuration);
            yield return m_CachedCanvasGroup.FadeToAlpha(0f, fadeOutDuration);
        }
    }
}
