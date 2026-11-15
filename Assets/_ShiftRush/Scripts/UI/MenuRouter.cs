using Michsky.UI.Heat;
using UnityEngine;

namespace ShiftRush.UI
{
    /// <summary>
    /// Moves between the menu's screens on top of Heat's PanelManager: Home → Garage (bike shed) → Tune → Start Gate (ride mode) → Tracks, plus Settings
    /// and Credits. Back (Esc / B) goes one screen up; on Home it asks to quit. Also tells the showroom
    /// camera how to frame the bike for each screen.
    /// </summary>
    public class MenuRouter : MonoBehaviour
    {
        public const string Home = "Home", Garage = "Garage", Tune = "Tune", Modes = "Modes", Tracks = "Tracks", Settings = "Settings";

        [SerializeField] PanelManager panels;
        [SerializeField] Showroom showroom;
        [SerializeField] CreditsManager credits;
        [SerializeField] ModalWindowManager quitWindow;

        string current = Home;

        void Awake()
        {
            // So Settings → Controls shows the keys a race will actually use.
            ShiftRush.Vehicles.ControlBindings.Apply();
        }

        public void OpenHome() { Open(Home); }
        public void OpenGarage() { Open(Garage); }
        public void OpenTune() { Open(Tune); }
        public void OpenModes() { Open(Modes); }
        public void OpenTracks() { Open(Tracks); }
        public void OpenSettings() { Open(Settings); }

        public void Open(string panel)
        {
            if (panel == current) return;
            current = panel;
            panels.OpenPanel(panel);
            if (showroom != null)
            {
                if (panel == Garage || panel == Tune) showroom.FrameGarage();
                else if (panel == Tracks || panel == Modes) showroom.FrameTracks();
                else showroom.FrameHome();
            }
        }

        public void OpenCredits()
        {
            if (credits != null) credits.OpenPanel();
        }

        public void AskQuit()
        {
#if UNITY_WEBGL
            return;
#else
            if (quitWindow != null) quitWindow.OpenWindow();
            else Quit();
#endif
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Wired to each screen's Back hotkey.</summary>
        public void Back()
        {
            if (BindingRow.IsListening) return;
            MenuSounds.Back();
            switch (current)
            {
                case Tracks: Open(Modes); break;
                case Modes: Open(Tune); break;
                case Tune: Open(Garage); break;
                case Garage:
                case Settings: Open(Home); break;
                default: AskQuit(); break;
            }
        }
    }
}
