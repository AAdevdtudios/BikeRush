using ShiftRush.Core;
using ShiftRush.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftRush.UI
{
    /// <summary>
    /// Tune: the four set-up sliders for the bike picked in the shed. W/S picks a slider, A/D moves it, R puts the
    /// bike back as it came, Enter rides on. Every slider is a trade, so the readout shows what you gain and lose
    /// in real numbers. Saved per bike as you go (<see cref="BikeTuning"/>); the race applies it to your bike.
    /// </summary>
    public class TuningPanel : MonoBehaviour
    {
        [System.Serializable]
        public class Row
        {
            public RectTransform root;
            public TMP_Text label, low, high;
            public Image[] notches = new Image[BikeTuning.Max + 1];
        }

        [SerializeField] CanvasGroup visibleGroup;
        [SerializeField] Showroom showroom;
        [SerializeField] TMP_Text bikeName;
        [SerializeField] TMP_Text description;
        [SerializeField] TMP_Text readout;
        [SerializeField] RectTransform selector;
        [SerializeField] Row[] rows = new Row[BikeTuning.Count];
        [SerializeField] Button resetButton;
        [SerializeField] Button confirmButton;
        [SerializeField] Color idle = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] Color steel = new Color32(154, 160, 180, 255);
        public UnityEvent onConfirm = new UnityEvent();

        RaceCatalog.Car bike;
        BikeTuning.Numbers baseNumbers;
        bool hasBase;
        int[] values = new int[BikeTuning.Count];
        int selected;
        float selectorY;

        void Awake()
        {
            if (resetButton) resetButton.onClick.AddListener(ResetAll);
            if (confirmButton) confirmButton.onClick.AddListener(Confirm);
            // Click or drag along a slider's notches to set it.
            for (int r = 0; r < rows.Length; r++)
                for (int k = 0; k < rows[r].notches.Length; k++)
                {
                    var img = rows[r].notches[k];
                    if (img == null) continue;
                    int row = r, value = k;
                    var trigger = img.gameObject.GetComponent<EventTrigger>() ?? img.gameObject.AddComponent<EventTrigger>();
                    AddTrigger(trigger, EventTriggerType.PointerDown, () => { Select(row); Set(row, value); });
                    AddTrigger(trigger, EventTriggerType.PointerEnter, () => { if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed && row == selected) Set(row, value); });
                }
        }

        static void AddTrigger(EventTrigger t, EventTriggerType type, System.Action a)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(_ => a());
            t.triggers.Add(e);
        }

        void OnEnable()
        {
            bike = RaceSession.Car;
            if (bike == null) return;
            values = BikeTuning.Load(bike.id);
            var adapter = bike.prefab != null ? bike.prefab.GetComponent<BikeVehicleAdapter>() : null;
            hasBase = adapter != null;
            if (hasBase) baseNumbers = adapter.BaseNumbers;
            if (bikeName) bikeName.text = bike.displayName.ToUpperInvariant();
            selected = 0;
            if (rows.Length > 0 && rows[0].root) selectorY = rows[0].root.anchoredPosition.y;
            Refresh();
        }

        bool Accepting { get { return visibleGroup == null || (visibleGroup.alpha > 0.95f && visibleGroup.interactable); } }

        void Update()
        {
            if (selector != null && rows.Length > 0 && rows[selected].root != null)
            {
                selectorY = Mathf.Lerp(selectorY, rows[selected].root.anchoredPosition.y, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
                var p = selector.anchoredPosition; p.y = selectorY; selector.anchoredPosition = p;
            }
            if (!Accepting || bike == null) return;
            if (MenuInput.Up) { Select((selected + rows.Length - 1) % rows.Length); MenuSounds.Tick(); }
            else if (MenuInput.Down) { Select((selected + 1) % rows.Length); MenuSounds.Tick(); }
            else if (MenuInput.Left) Set(selected, values[selected] - 1);
            else if (MenuInput.Right) Set(selected, values[selected] + 1);
            else if (MenuInput.KeyPressed("R")) ResetAll();
            else if (MenuInput.Submit) Confirm();
        }

        void Select(int row)
        {
            selected = Mathf.Clamp(row, 0, rows.Length - 1);
            Refresh();
        }

        void Set(int row, int value)
        {
            value = Mathf.Clamp(value, 0, BikeTuning.Max);
            if (bike == null || values[row] == value) { MenuSounds.Tick(); return; }
            values[row] = value;
            BikeTuning.Save(bike.id, values);
            MenuSounds.Shift();
            if (showroom) showroom.Kick(0.35f);
            Refresh();
        }

        public void ResetAll()
        {
            if (bike == null) return;
            for (int i = 0; i < values.Length; i++) values[i] = BikeTuning.Middle;
            BikeTuning.Save(bike.id, values);
            MenuSounds.Back();
            if (showroom) showroom.Kick(0.6f);
            Refresh();
        }

        public void Confirm()
        {
            if (!Accepting) return;
            MenuSounds.Select();
            onConfirm.Invoke();
        }

        void Refresh()
        {
            Color accent = bike != null ? bike.accent : Color.magenta;
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                bool on = r == selected;
                var s = BikeTuning.Sliders[r];
                if (row.label) { row.label.text = s.name; row.label.color = on ? Color.white : steel; }
                int v = values[r];
                if (row.low) { row.low.text = s.low; row.low.color = v < BikeTuning.Middle ? accent : steel; }
                if (row.high) { row.high.text = s.high; row.high.color = v > BikeTuning.Middle ? accent : steel; }
                for (int k = 0; k < row.notches.Length; k++)
                {
                    var img = row.notches[k];
                    if (img == null) continue;
                    bool between = (k >= BikeTuning.Middle && k <= v) || (k <= BikeTuning.Middle && k >= v);
                    Color c = k == v ? (on ? Color.white : accent) : between ? new Color(accent.r, accent.g, accent.b, 0.45f) : idle;
                    if (k == BikeTuning.Middle && k != v) c = new Color(1f, 1f, 1f, 0.3f); // stock mark
                    img.color = c;
                    img.rectTransform.localScale = k == v ? new Vector3(1f, 1.35f, 1f) : Vector3.one;
                }
            }
            if (description) description.text = BikeTuning.Sliders[selected].description;
            if (readout) readout.text = hasBase ? Readout(BikeTuning.Apply(baseNumbers, values), accent) : "";
        }

        static string Readout(BikeTuning.Numbers n, Color accent)
        {
            string hex = "#" + ColorUtility.ToHtmlStringRGB(accent);
            string unit = GameSettings.SpeedUnit;
            string Speed(float kmh) { return Mathf.RoundToInt(GameSettings.ToDisplaySpeed(kmh)).ToString(); }
            // 0-to-cruise time from the launch pull (the pack's own pedalling adds a little on top).
            float launch = n.cruiseKmh / 3.6f / Mathf.Max(0.1f, n.launchAccel + 1.5f);
            float burn = n.burnPerSecond > 0f ? 1f / n.burnPerSecond : 0f;
            return "PEDAL <color=" + hex + ">" + Speed(n.cruiseKmh) + "</color>   BOOST <color=" + hex + ">" + Speed(n.boostKmh) + "</color> " + unit
                 + "     LAUNCH <color=" + hex + ">" + launch.ToString("0.0") + " s</color>"
                 + "     TANK <color=" + hex + ">" + burn.ToString("0.0") + " s</color>";
        }
    }
}
