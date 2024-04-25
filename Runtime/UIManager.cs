using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wsh.Singleton;
using Wsh.UIAnimation;

namespace Wsh.UI {

    public class UIManager : MonoSingleton<UIManager> {

        public struct UIDefine {
            public string Name;
            public string PrefabPath;
            public Type ClassType;
            public bool IsLoading;
            public float DebugMaskAlpha;
            public bool CanCloseByEsc;
        }

        public RectTransform CanvasRectTransform { get { return m_canvasRectTransform; } }

        public Camera UICamera { get { return m_camera; } }

        private Camera m_camera;
        private RectTransform m_canvasRectTransform;
        private GameObject m_viewRoot;
        private GameObject m_msgRoot;
        private GameObject m_loadingRoot;
        private UIAnimationGroupPlayer m_greenMsgAnimationPlayer;
        private UIAnimationGroupPlayer m_redMsgAnimationPlayer;
        private Text m_textGreenMsg;
        private Text m_textRedMsg;
        private Image m_imageMaskRaycast;
        private Image m_imageMaskDebug;
        private GameObject m_root;
        private List<BaseView> m_viewList;
        private int m_inputLockNumber;
        private bool m_isDarkMode;
        private Dictionary<Type, UIDefine> m_uiDefDic;
        private iUILoader m_loader;
        
        protected override void OnInit() {
            m_uiDefDic = new Dictionary<Type, UIDefine>();
            m_inputLockNumber = 0;
            m_viewList = new List<BaseView>();
        }

        public void InitAsync(iUILoader loader, Vector3 rootPosition, Action onComplete) {
            m_loader = loader;
            string rootPrefabPath = m_loader.GetUIRootPrefabPath();
            m_loader.InstanceAsync(rootPrefabPath, null, root => {
                root.transform.position = rootPosition;
                InitObjects(root);
                onComplete?.Invoke();
            });
        }

        private void InitObjects(GameObject root) {
            m_root = root;
            m_camera = root.transform.Find("Camera").GetComponent<Camera>();
            m_canvasRectTransform = root.transform.Find("Canvas").GetComponent<RectTransform>();
            m_viewRoot = root.transform.Find("Canvas/Panel_Views").gameObject;
            m_msgRoot = root.transform.Find("Canvas/Panel_Message").gameObject;
            m_loadingRoot = root.transform.Find("Canvas/Panel_Loading").gameObject;
            m_greenMsgAnimationPlayer = root.transform.Find("Canvas/Panel_Message/Green_Message").GetComponent<UIAnimationGroupPlayer>();
            m_redMsgAnimationPlayer = root.transform.Find("Canvas/Panel_Message/Red_Message").GetComponent<UIAnimationGroupPlayer>();
            m_textGreenMsg = root.transform.Find("Canvas/Panel_Message/Green_Message/Text_Green_Message").GetComponent<Text>();
            m_textRedMsg = root.transform.Find("Canvas/Panel_Message/Red_Message/Text_Red_Message").GetComponent<Text>();
            m_imageMaskRaycast = root.transform.Find("Canvas/Image_Mask_Raycast_Enable").GetComponent<Image>();
            m_imageMaskDebug = root.transform.Find("Canvas/Image_Mask_Debug").GetComponent<Image>();
        }

        public List<T> GetViews<T>() where T : BaseView {
            UIDefine uiDefine = m_uiDefDic[typeof(T)];
            var list = m_viewList.FindAll(v => v.Name == uiDefine.Name);
            if(list != null && list.Count > 0) {
                List<T> ls = new List<T>();
                for(int i = 0; i < list.Count; i++) {
                    ls.Add(list[i] as T);
                }
                return ls;
            }
            return null;
        }

        public T GetView<T>() where T : BaseView {
            var list = GetViews<T>();
            if(list != null && list.Count > 0) {
                if(list.Count > 1) {
                    list.Sort((v1, v2) => {
                        if(v1.OpenTime > v2.OpenTime) {
                            return 1;
                        } else {
                            return -1;
                        }
                    });
                }
                return list[0];
            }
            return null;
        }

        public BaseView GetLatestView() {
            if(m_viewList.Count > 0) {
                return m_viewList[m_viewList.Count - 1];
            }
            return null;
        }

        private void RemoveView(BaseView view) {
            int removeIndex = -1;
            for(int i = 0; i < m_viewList.Count; i++) {
                if(m_viewList[i] == view) {
                    removeIndex = i;
                }
            }
            if(removeIndex != -1) { m_viewList.RemoveAt(removeIndex); }
        }

        private void DestroyView(BaseView view) {
            RemoveView(view);
            Destroy(view.gameObject);
        }

        public void CloseView(BaseView view) {
            view.OnClose(DestroyView);
        }

        public void ShowViewAsync<T>(int id, Action<T> onComplete, params object[] pm) where T : BaseView {
            Type type = typeof(T);
            if(!m_uiDefDic.ContainsKey(type)) {
                UIDefine uiDef = new UIDefine {
                    Name = m_loader.GetName(id),
                    PrefabPath = m_loader.GetPrefabPath(id),
                    ClassType = m_loader.GetClassType(id),
                    IsLoading = m_loader.IsLoadingUI(id),
                    DebugMaskAlpha = m_loader.GetDebugMaskAlpha(id),
                    CanCloseByEsc = m_loader.CanCloseByEsc(id),
                };
                m_uiDefDic.Add(type, uiDef);
            }
            UIDefine uiDefine = m_uiDefDic[type];
            var root = uiDefine.IsLoading ? m_loadingRoot : m_viewRoot;
            m_loader.InstanceAsync(uiDefine.PrefabPath, root, go => {
                T v = go.GetComponent<T>();
                if(v == null) {
                    v = go.AddComponent<T>() as T;
                }
                v.OnStart(this, uiDefine);
                v.OnInit(pm);
                m_viewList.Add(v);
                onComplete?.Invoke(v);
            });
        }

        private void Update() {
            for(int i = 0; i < m_viewList.Count; i++) {
                if(!m_viewList[i].IsCloseing) {
                    m_viewList[i].OnUpdate(Time.deltaTime);
                }
            }
        }

        private void SetMessage(UIAnimationGroupPlayer animationPlayer, Text textMsg, string msg) {
            textMsg.text = msg;
            animationPlayer.Play();
        }

        public void GreenMessage(string msg) {
            SetMessage(m_greenMsgAnimationPlayer, m_textGreenMsg, msg);
        }

        public void RedMessage(string msg) {
            SetMessage(m_redMsgAnimationPlayer, m_textRedMsg, msg);
        }

        public void SetDarkMode(bool isDarkMode) {
            m_isDarkMode = isDarkMode;
        }

        public void UpdateImageMaskDebug(float alpha) {
            if(m_isDarkMode) {
                SetImageAlpha(m_imageMaskDebug, alpha);
            }
        }

        public float GetImageMaskDebugAlpha() {
            return m_imageMaskDebug.color.a;
        }

        public void SetImageAlpha(Image image, float alpha) {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private void SetEventSystemLockState(bool islock) {
            m_imageMaskRaycast.raycastTarget = islock;
        }

        public void UnlockInput() {
            m_inputLockNumber--;
            if(m_inputLockNumber < 0)
                UnityEngine.Debug.LogError("Unlock input number error");
            if(m_inputLockNumber == 0) {
                SetEventSystemLockState(false);
            }
        }

        public void LockInput() {
            if(m_inputLockNumber == 0) {
                SetEventSystemLockState(true);
            }
            m_inputLockNumber++;
        }

        public bool IsLockInput() {
            return m_inputLockNumber > 0;
        }

        protected override void OnDeinit() {
            m_uiDefDic.Clear();
            m_viewList.Clear();
            Destroy(m_root);
        }

    }
}