using System;
using System.Collections;
using UnityEngine;
using Wsh.UIAnimation;

namespace Wsh.UI {
    public class BaseView : MonoBehaviour {

        public const int SHOW_ANIMATION_GROUP = 100;
        public const int CLOSE_ANIMATION_GROUP = 200;

        public event Action OnBeforeAnimationShowEvent;
        public event Action OnAfterAnimationShowEvent;
        public event Action OnBeforeAnimationCloseEvent;
        public event Action OnAfterAnimationCloseEvent;

        public string Name { get { return m_uiDefine.Name; } }
        public float OpenTime { get { return m_openTime; } }
        public bool CanCloseByEsc { get { return m_uiDefine.CanCloseByEsc; } }

        private float m_openTime;
        private UIManager.UIDefine m_uiDefine;
        protected UIManager m_uiMgr;
        protected UIBaseAnimation[] m_uIAnimations;

        //debug
        private float m_lastImageMaskDebugAlpha;

        public void OnStart(UIManager uiMgr, UIManager.UIDefine uiDefine) {
            m_uiMgr = uiMgr;
            m_uiDefine = uiDefine;
            m_uIAnimations = transform.gameObject.GetComponentsInChildren<UIBaseAnimation>();
            m_openTime = Time.realtimeSinceStartup;
        }

        protected Transform Find(string path) {
            var tf = transform.Find(path);
            if(tf != null) {
                return tf;
            }
            return null;
        }

        protected T TryGetComponent<T>(string path) where T : MonoBehaviour {
            var tf = Find(path);
            if(tf != null) {
                return tf.GetComponent<T>();
            }
            return null;
        }

        IEnumerator WaitFor(float time, Action<BaseView> onComplete) {
            yield return new WaitForSeconds(time);
            onComplete?.Invoke(this);
        }

        private void PlayAnimation(int group, Action<BaseView> onComplete = null) {
            float maxDuration = UIAnimationUtils.PlayAnimation(m_uIAnimations, group);
            if(maxDuration > 0) {
                StartCoroutine(WaitFor(maxDuration, onComplete));
            } else {
                onComplete?.Invoke(this);
            }
        }

        public virtual void OnInit(params object[] pm) {
            m_lastImageMaskDebugAlpha = m_uiMgr.GetImageMaskDebugAlpha();
            m_uiMgr.UpdateImageMaskDebug(m_uiDefine.DebugMaskAlpha);
            m_uiMgr.LockInput();
            OnBeforeAnimationShowEvent?.Invoke();
            PlayAnimation(SHOW_ANIMATION_GROUP, view => {
                OnAfterAnimationShowEvent?.Invoke();
                OnAfterStartAnimation(view);
                m_uiMgr.UnlockInput();
            });
        }

        public virtual void OnClose(Action<BaseView> onComplete) {
            m_uiMgr.LockInput();
            m_uiMgr.UpdateImageMaskDebug(m_lastImageMaskDebugAlpha);
            OnBeforeAnimationCloseEvent?.Invoke();
            PlayAnimation(CLOSE_ANIMATION_GROUP, view => {
                m_uiMgr.UnlockInput();
                OnAfterAnimationCloseEvent?.Invoke();
                onComplete?.Invoke(view);
            });
        }

        public void Close() {
            m_uiMgr.CloseView(this);
        }

        protected virtual void OnAfterStartAnimation(BaseView view) {

        }
    }
}