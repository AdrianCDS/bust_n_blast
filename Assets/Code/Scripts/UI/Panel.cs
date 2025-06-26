using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TeamBasedShooter
{
    public class Panel : MonoBehaviour
    {
        public enum HideLocation
        {
            SAME_POSITION,
            LEFT_EDGE,
            RIGHT_EDGE,
            UPPER_EDGE,
            LOWER_EDGE,
            SPECIFIED_RECT,
            SPECIFIED_POS
        }

        public HideLocation _hideAt = HideLocation.SAME_POSITION;

        public bool _hideInitially = true;
        public bool _disableWhenHidden = true;

        public RectTransform _hideRect;

        public Vector2 _hiddenPos;

        public Vector3 _hideScale = Vector3.one;

        public float _hideAlpha = 1.0f;

        public Vector2 _shownPos;

        public Vector3 _shownScale = Vector3.one;

        public float _showAlpha = 1.0f;

        public bool _rememberAlphas;

        public bool _rememberPosition;

        private Vector2 _shownSize;

        private bool _isShowing;
        private Coroutine _coroutine;

        public bool IsShowing
        {
            get { return _isShowing; }
        }

        public Vector2 ShownPos
        {
            get { return _shownPos; }
            set { _shownPos = value; }
        }

        private Dictionary<Graphic, float> _orgAlphas;
        private bool _captured;

        protected void Awake()
        {
            _isShowing = true;
            if (_hideInitially)
            {
                CaptureRect();
                ((RectTransform)transform).anchoredPosition = HiddenPos;
                ((RectTransform)transform).localScale = _hideScale;
                if (_disableWhenHidden)
                    gameObject.SetActive(false);
                _isShowing = false;
            }
        }

        private void CaptureRect()
        {
            if (!_captured)
            {
                _shownPos = ((RectTransform)transform).anchoredPosition;
                _shownScale = transform.localScale;
                _captured = true;
            }
        }

        public virtual Vector2 HiddenSize
        {
            get
            {
                if (_hideAt == HideLocation.SPECIFIED_RECT)
                    return _hideRect.rect.size;
                return _shownSize;
            }
        }

        public virtual Vector2 HiddenPos
        {
            get
            {
                if (transform.parent == null || !(transform.parent is RectTransform))
                    return _shownPos;

                RectTransform rt = (RectTransform)transform;
                RectTransform prt = (RectTransform)transform.parent;

                Vector2 hidden = _shownPos;

                switch (_hideAt)
                {
                    case HideLocation.SAME_POSITION:
                        break;
                    case HideLocation.LOWER_EDGE:
                        hidden.y = prt.rect.height * (rt.anchorMin.y * (rt.pivot.y - 1.0f) - rt.anchorMax.y * rt.pivot.y) + (rt.pivot.y - 1.0f) * _hideScale.y * rt.rect.height;
                        break;
                    case HideLocation.UPPER_EDGE:
                        hidden.y = prt.rect.height * (1 + rt.anchorMin.y * (rt.pivot.y - 1.0f) - rt.anchorMax.y * rt.pivot.y) + (rt.pivot.y) * _hideScale.y * rt.rect.height;
                        break;
                    case HideLocation.LEFT_EDGE:
                        hidden.x = prt.rect.width * (rt.anchorMin.x * (rt.pivot.x - 1.0f) - rt.anchorMax.x * rt.pivot.x) + (rt.pivot.x - 1.0f) * _hideScale.x * rt.rect.width;
                        break;
                    case HideLocation.RIGHT_EDGE:
                        hidden.x = prt.rect.width * (1 + rt.anchorMin.x * (rt.pivot.x - 1.0f) - rt.anchorMax.x * rt.pivot.x) + (rt.pivot.x) * _hideScale.x * rt.rect.width;
                        break;
                    case HideLocation.SPECIFIED_RECT:
                        hidden = MapAnchoredPosition(_hideRect, (RectTransform)transform);
                        break;
                    case HideLocation.SPECIFIED_POS:
                        hidden = _hiddenPos;
                        break;
                }

                return hidden;
            }
        }

        private Vector2 MapAnchoredPosition(RectTransform from, RectTransform to)
        {
            float wto = ((RectTransform)from.parent).rect.width;
            float hto = ((RectTransform)from.parent).rect.height;
            Vector2 vto = from.anchoredPosition;
            Vector2 cto = new Vector2((from.anchorMin.x + from.anchorMax.x) * wto / 2, (from.anchorMin.y + from.anchorMax.y) * hto / 2);

            float wfrom = ((RectTransform)to.parent).rect.width;
            float hfrom = ((RectTransform)to.parent).rect.height;
            Vector2 cfrom = new Vector2((to.anchorMin.x + to.anchorMax.x) * wfrom / 2, (to.anchorMin.y + to.anchorMax.y) * hfrom / 2);

            return cto + vto - cfrom;
        }

        public void SetVisible(bool v, bool immediately = false, Action then = null)
        {
            if (_shownSize.x == 0 || _shownSize.y == 0)
                _shownSize = ((RectTransform)transform).rect.size;
            if (v == IsShowing)
                return;
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            if (v)
                Show(immediately, then);
            else
                Hide(immediately, then);
        }

        private void Hide(bool immediately = false, Action then = null)
        {
            _isShowing = false;
            if (OnHide != null)
                OnHide(this);
            WillHide();
            CaptureRect();
            if (_rememberPosition)
                _shownPos = ((RectTransform)transform).anchoredPosition;
            if (_hideAlpha < 1.0f && (_rememberAlphas || _orgAlphas == null))
                RememberAlphas();

            DidHide();
            if (OnDidHide != null)
                OnDidHide(this);
            if (_disableWhenHidden)
                gameObject.SetActive(false);
            if (then != null)
                then();
        }

        private void RememberAlphas()
        {
            _orgAlphas = new Dictionary<Graphic, float>();
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                _orgAlphas[graphic] = graphic.color.a;
            }
        }

        private void Show(bool immediately = false, Action then = null)
        {
            if (_hideInitially)
            {
                ((RectTransform)transform).anchoredPosition = HiddenPos;
                ((RectTransform)transform).localScale = _hideScale;
                _hideInitially = false; // Prevent immediately hiding it again if this is the first time we show it!
            }

            if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
            {
                return;
            }

            gameObject.SetActive(true);
            if (_hideAlpha < 1.0f && _orgAlphas == null)
                RememberAlphas();

            DidShow();
            if (OnDidShow != null)
                OnDidShow(this);
            if (then != null)
                then();

            _isShowing = true;
            if (OnShow != null)
                OnShow(this);
            WillShow();
        }

        public event Action<Panel> OnHide;
        public event Action<Panel> OnShow;
        public event Action<Panel> OnDidHide;
        public event Action<Panel> OnDidShow;

        public virtual void WillShow()
        {
        }

        public virtual void DidShow()
        {
        }

        public virtual void WillHide()
        {
        }

        public virtual void DidHide()
        {
        }
    }
}
