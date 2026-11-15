using ShiftRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftRush.UI
{
    /// <summary>
    /// Start Gate: three tall slanted cards, one per ride mode (<see cref="RideModes"/>). A/D picks, Enter locks it
    /// in and moves on to the trail. The picked card lifts, grows and lights up in that mode's own colour; the
    /// others sink back. Click a card to pick it, click it again to lock in.
    /// </summary>
    public class ModeSelectPanel : MonoBehaviour
    {
        [System.Serializable]
        public class Card
        {
            public RectTransform root;
            public Image fill, edge, icon, stripe;
            public TMP_Text index, title, tag, description, badge;
        }

        [SerializeField] CanvasGroup visibleGroup;
        [SerializeField] Showroom showroom;
        [SerializeField] Card[] cards = new Card[3];
        [SerializeField] Color steel = new Color32(154, 160, 180, 255);
        public UnityEvent onConfirm = new UnityEvent();

        int selected;
        float[] lift = new float[3];
        Vector2[] home;

        void Awake()
        {
            home = new Vector2[cards.Length];
            if (lift.Length != cards.Length) lift = new float[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].root == null) continue;
                home[i] = cards[i].root.anchoredPosition;
                int k = i;
                var t = cards[i].root.gameObject.GetComponent<EventTrigger>() ?? cards[i].root.gameObject.AddComponent<EventTrigger>();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                down.callback.AddListener(_ => { if (k == selected) Confirm(); else { Select(k); MenuSounds.Shift(); } });
                t.triggers.Add(down);
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => MenuSounds.Hover());
                t.triggers.Add(enter);
            }
        }

        void OnEnable()
        {
            selected = Mathf.Clamp((int)RaceSession.Mode, 0, cards.Length - 1);
            Refresh();
        }

        bool Accepting { get { return visibleGroup == null || (visibleGroup.alpha > 0.95f && visibleGroup.interactable); } }

        void Update()
        {
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f);
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i];
                if (c.root == null) continue;
                lift[i] = Mathf.Lerp(lift[i], i == selected ? 1f : 0f, k);
                c.root.anchoredPosition = home[i] + new Vector2(0f, 34f * lift[i]);
                c.root.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.04f, lift[i]);
            }
            if (!Accepting) return;
            if (MenuInput.Left) { Select((selected + cards.Length - 1) % cards.Length); MenuSounds.Shift(); }
            else if (MenuInput.Right) { Select((selected + 1) % cards.Length); MenuSounds.Shift(); }
            else if (MenuInput.KeyPressed("1")) { Select(0); MenuSounds.Shift(); }
            else if (MenuInput.KeyPressed("2")) { Select(1); MenuSounds.Shift(); }
            else if (MenuInput.KeyPressed("3")) { Select(2); MenuSounds.Shift(); }
            else if (MenuInput.Submit) Confirm();
        }

        void Select(int i)
        {
            selected = Mathf.Clamp(i, 0, cards.Length - 1);
            RaceSession.Mode = (RideMode)selected;
            if (showroom) showroom.Kick(0.45f);
            Refresh();
        }

        public void Confirm()
        {
            if (!Accepting) return;
            RaceSession.Mode = (RideMode)selected;
            PlayerPrefs.Save();
            MenuSounds.Select();
            if (RaceSession.Mode == RideMode.AirShow)
            {
                // Its own side-on venue, no trail to pick.
                var car = RaceSession.Car;
                SceneLoader.Load(RideModes.AirShowScene, RideModes.AirShowVenue,
                    (car != null ? car.displayName.ToUpperInvariant() + "  //  " : "") + "AIR SHOW", RideModes.AirShowTip());
                return;
            }
            onConfirm.Invoke();
        }

        void Refresh()
        {
            for (int i = 0; i < cards.Length && i < RideModes.All.Length; i++)
            {
                var c = cards[i];
                var m = RideModes.All[i];
                bool on = i == selected;
                Color a = m.accent;
                if (c.fill) c.fill.color = on ? new Color(a.r * 0.22f, a.g * 0.22f, a.b * 0.22f, 0.92f) : new Color(0.04f, 0.04f, 0.07f, 0.78f);
                if (c.edge) c.edge.color = on ? a : new Color(1f, 1f, 1f, 0.16f);
                if (c.stripe) c.stripe.color = on ? a : new Color(a.r, a.g, a.b, 0.35f);
                if (c.icon) c.icon.color = on ? a : new Color(1f, 1f, 1f, 0.35f);
                if (c.index) { c.index.text = "0" + (i + 1); c.index.color = on ? new Color(a.r, a.g, a.b, 0.5f) : new Color(1f, 1f, 1f, 0.08f); }
                if (c.title) { c.title.text = m.name; c.title.color = on ? Color.white : steel; }
                if (c.tag) { c.tag.text = m.tag; c.tag.color = on ? a : new Color(steel.r, steel.g, steel.b, 0.6f); }
                if (c.description) { c.description.text = m.description; c.description.color = on ? new Color(1f, 1f, 1f, 0.85f) : new Color(steel.r, steel.g, steel.b, 0.45f); }
                if (c.badge)
                {
                    c.badge.text = m.ready ? "" : m.badge;
                    c.badge.color = on ? a : new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
    }
}
