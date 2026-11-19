using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ShiftRush.UI
{
    /// <summary>
    /// The Razorbill Studio ident: plays StreamingAssets/RazorbillSplash.mp4 full screen once per launch, before the
    /// "press any key" title. Holds Heat's MenuManager until it's done (the menu starts in its Start(), which only runs
    /// once the component is enabled), so the title animates in after the ident, not underneath it.
    /// Any key, click or pad button skips. The menu is only released once the skip press is let go, so the same press
    /// doesn't also skip the title. Browsers can refuse to autoplay video with sound: then it retries muted, and if
    /// that fails too it goes straight to the title.
    /// </summary>
    public class StudioSplash : MonoBehaviour
    {
        const string FileName = "RazorbillSplash.mp4";
        const string MenuScene = "MainMenu";
        static bool shown;

        VideoPlayer player;
        RenderTexture target;
        CanvasGroup group;
        Behaviour menu;
        bool finishing;
        double lastTime;
        float lastProgress, born;
        bool Progressing { get { return player != null && player.time > 0.05; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // sceneLoaded fires after Awake but before Start: in time to hold the menu back.
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (shown || scene.name != MenuScene) return;
            shown = true;
            new GameObject("Studio Splash").AddComponent<StudioSplash>();
        }

        void Awake()
        {
            var mm = FindFirstObjectByType<Michsky.UI.Heat.MenuManager>();
            if (mm != null && mm.enabled) { menu = mm; mm.enabled = false; }
            AudioListener.pause = true; // menu music waits too. The ident's own sound goes straight out, not through the mixer.
            BuildOverlay();
            BuildPlayer();
        }

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>(); // swallows clicks meant for the menu below
            group = gameObject.AddComponent<CanvasGroup>();

            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            black.transform.SetParent(transform, false);
            Fill(black.GetComponent<RectTransform>());
            black.GetComponent<Image>().color = Color.black;

            target = new RenderTexture(1920, 1080, 0) { name = "Studio Splash RT" };
            var video = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            video.transform.SetParent(transform, false);
            Fill(video.GetComponent<RectTransform>());
            video.GetComponent<RawImage>().texture = target;
            var fit = video.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 16f / 9f;
        }

        void BuildPlayer()
        {
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = System.IO.Path.Combine(Application.streamingAssetsPath, FileName);
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.isLooping = false;
            player.skipOnDrop = true;
            player.errorReceived += (vp, msg) => { Debug.LogWarning("[BIKE//RUSH] Studio splash skipped: " + msg); Finish(); };
            player.loopPointReached += vp => Finish();
            player.Play();
            born = lastProgress = Time.realtimeSinceStartup;
            StartCoroutine(Watchdog());
        }

        // Browsers may refuse to autoplay video with sound, and Unity can still report it as started, so only the
        // clock moving counts as playing. Not moving after 1.5 s: retry muted. Still not by 4 s, or stalled for 2 s,
        // or past 14 s in all: straight to the title. Never a black screen that waits forever.
        IEnumerator Watchdog()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Progressing || finishing) yield break;
            for (ushort t = 0; t < player.audioTrackCount; t++) player.SetDirectAudioMute(t, true);
            player.Play();
            yield return new WaitForSecondsRealtime(2.5f);
            if (!Progressing) Finish();
        }

        void Update()
        {
            if (finishing || player == null) return;
            float now = Time.realtimeSinceStartup;
            if (player.time > lastTime + 0.01) { lastTime = player.time; lastProgress = now; }
            else if (Progressing && now - lastProgress > 2f) { Finish(); return; }
            if (now - born > 14f) { Finish(); return; }
            if (player.time > 0.3 && Pressed()) Finish();
        }

        static bool Pressed()
        {
            var k = Keyboard.current; if (k != null && k.anyKey.wasPressedThisFrame) return true;
            var m = Mouse.current; if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) return true;
            var g = Gamepad.current;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame)) return true;
            var t = Touchscreen.current; if (t != null && t.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        static bool AnyHeld()
        {
            var k = Keyboard.current; if (k != null && k.anyKey.isPressed) return true;
            var m = Mouse.current; if (m != null && (m.leftButton.isPressed || m.rightButton.isPressed)) return true;
            var g = Gamepad.current; if (g != null && (g.buttonSouth.isPressed || g.buttonEast.isPressed || g.startButton.isPressed)) return true;
            return false;
        }

        void Finish()
        {
            if (finishing) return;
            finishing = true;
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / 0.35f;
                yield return null;
            }
            group.alpha = 0f;
            if (player != null) player.Stop();
            // Let go of the skip press first, or the title would take it as its "any key".
            float wait = 0f;
            while (AnyHeld() && wait < 2f) { wait += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            AudioListener.pause = false;
            if (menu != null) menu.enabled = true;
            if (target != null) { target.Release(); Destroy(target); }
            Destroy(gameObject);
        }

        static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }
    }
}
