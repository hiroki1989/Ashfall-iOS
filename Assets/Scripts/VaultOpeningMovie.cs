using PlayerPrefs = NeonVault.SaveSlotPrefs;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace NeonVault
{
    public partial class VaultGame
    {
        const string OpeningMovieSeenKey = "Vault.OpeningMovieSeen.v1";
        const string OpeningMovieRelativePath = "Movies/Ashfall_Opening_Movie.mp4";
        const float OpeningMoviePrepareTimeout = 8f;
        const float OpeningMovieSkipGraceSeconds = 0.6f;

        bool openingMoviePlaying;
        bool openingMoviePrepared;
        bool openingMovieStartedPlayback;
        float openingMovieStartedAt;
        float openingMoviePlayStartTime;
        GameObject openingMovieHost;
        VideoPlayer openingMoviePlayer;
        RenderTexture openingMovieTexture;

        public bool OpeningMoviePlaying => openingMoviePlaying;
        internal bool OpeningMovieSkipVisible => openingMoviePlaying && Time.unscaledTime >= openingMovieStartedAt + 1.0f;

        void TryBeginOpeningMovieBeforeIntro()
        {
            if (smoke || openingMoviePlaying || WorldStorySeen) return;
            if (PlayerPrefs.GetInt(OpeningMovieSeenKey, 0) != 0) return;

            string path = Path.Combine(Application.streamingAssetsPath, OpeningMovieRelativePath);
            if (!File.Exists(path))
            {
                Debug.LogWarning("Opening movie file missing: " + path);
                PlayerPrefs.SetInt(OpeningMovieSeenKey, 1);
                PlayerPrefs.Save();
                return;
            }

            PlayerPrefs.SetInt(OpeningMovieSeenKey, 1);
            PlayerPrefs.Save();

            CleanupOpeningMovieResources();

            openingMovieTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            openingMovieTexture.Create();
            ClearRenderTextureBlack(openingMovieTexture);

            openingMovieHost = new GameObject("Ashfall Opening Movie Player");
            openingMovieHost.transform.SetParent(transform, false);
            openingMoviePlayer = openingMovieHost.AddComponent<VideoPlayer>();
            openingMoviePlayer.playOnAwake = false;
            openingMoviePlayer.isLooping = false;
            openingMoviePlayer.waitForFirstFrame = true;
            openingMoviePlayer.skipOnDrop = true;
            openingMoviePlayer.source = VideoSource.Url;
            openingMoviePlayer.url = new Uri(path).AbsoluteUri;
            openingMoviePlayer.renderMode = VideoRenderMode.RenderTexture;
            openingMoviePlayer.targetTexture = openingMovieTexture;
            openingMoviePlayer.aspectRatio = VideoAspectRatio.FitInside;
            openingMoviePlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            openingMoviePlayer.prepareCompleted += OnOpeningMoviePrepared;
            openingMoviePlayer.loopPointReached += OnOpeningMovieFinished;
            openingMoviePlayer.errorReceived += OnOpeningMovieError;

            openingMoviePlaying = true;
            openingMoviePrepared = false;
            openingMovieStartedPlayback = false;
            openingMovieStartedAt = Time.unscaledTime;
            openingMoviePlayStartTime = 0f;
            Time.timeScale = 1f;
            openingMoviePlayer.Prepare();
        }

        static void ClearRenderTextureBlack(RenderTexture rt)
        {
            if (rt == null) return;
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
        }

        void OnOpeningMoviePrepared(VideoPlayer vp)
        {
            if (!openingMoviePlaying || vp != openingMoviePlayer) return;
            openingMoviePrepared = true;
            openingMovieStartedPlayback = true;
            openingMoviePlayStartTime = Time.unscaledTime;
            if (vp.audioTrackCount > 0)
            {
                vp.SetDirectAudioMute(0, false);
                vp.SetDirectAudioVolume(0, AudioListener.volume);
            }
            vp.Play();
        }

        void OnOpeningMovieFinished(VideoPlayer vp)
        {
            if (!openingMoviePlaying || vp != openingMoviePlayer) return;
            FinishOpeningMovie();
        }

        void OnOpeningMovieError(VideoPlayer vp, string message)
        {
            Debug.LogWarning("Opening movie playback error: " + message);
            if (vp == openingMoviePlayer) FinishOpeningMovie();
        }

        void TickOpeningMovie()
        {
            if (!openingMoviePlaying) return;
            if (openingMoviePlayer == null)
            {
                FinishOpeningMovie();
                return;
            }

            if (!openingMoviePrepared)
            {
                if (Time.unscaledTime - openingMovieStartedAt > OpeningMoviePrepareTimeout)
                {
                    Debug.LogWarning("Opening movie prepare timed out.");
                    FinishOpeningMovie();
                }
                return;
            }

            if (openingMoviePlayer.audioTrackCount > 0)
                openingMoviePlayer.SetDirectAudioVolume(0, AudioListener.volume);

            if (openingMovieStartedPlayback && !openingMoviePlayer.isPlaying && Time.unscaledTime - openingMoviePlayStartTime > 0.5f)
            {
                FinishOpeningMovie();
                return;
            }

            if (openingMoviePlayer.frameCount > 0 && openingMoviePlayer.frame >= (long)openingMoviePlayer.frameCount - 1)
            {
                FinishOpeningMovie();
                return;
            }

            if (openingMoviePlayer.length > 0 && Time.unscaledTime - openingMoviePlayStartTime >= (float)openingMoviePlayer.length + 0.75f)
            {
                FinishOpeningMovie();
                return;
            }

            if (Time.unscaledTime >= openingMovieStartedAt + OpeningMovieSkipGraceSeconds)
            {
                if (Input.GetKeyDown(KeyCode.Escape) ||
                    Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.JoystickButton1) ||
                    Input.GetKeyDown(KeyCode.JoystickButton7))
                {
                    FinishOpeningMovie();
                }
            }
        }

        void DrawOpeningMovie(Matrix4x4 canvasMatrix)
        {
            GUI.matrix = Matrix4x4.identity;
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.blackTexture);
            if (openingMoviePrepared && openingMovieTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), openingMovieTexture, ScaleMode.ScaleToFit, false);
            }

            GUI.matrix = canvasMatrix;
            if (OpeningMovieSkipVisible)
            {
                bool skipClicked = WorldButton(new Rect(1235, 822, 168, 46), T("SKIP", "スキップ"));
                if (skipClicked && Time.unscaledTime >= openingMovieStartedAt + OpeningMovieSkipGraceSeconds)
                {
                    FinishOpeningMovie();
                }
            }
        }

        void FinishOpeningMovie()
        {
            openingMoviePlaying = false;
            openingMoviePrepared = false;
            openingMovieStartedPlayback = false;
            CleanupOpeningMovieResources();
            if (cam != null) cam.cullingMask = -1;
        }

        void ResetOpeningMovieRuntimeState()
        {
            FinishOpeningMovie();
        }

        void CleanupOpeningMovieResources()
        {
            if (openingMoviePlayer != null)
            {
                openingMoviePlayer.prepareCompleted -= OnOpeningMoviePrepared;
                openingMoviePlayer.loopPointReached -= OnOpeningMovieFinished;
                openingMoviePlayer.errorReceived -= OnOpeningMovieError;
                if (openingMoviePlayer.isPlaying) openingMoviePlayer.Stop();
                openingMoviePlayer.targetTexture = null;
                openingMoviePlayer = null;
            }
            if (openingMovieHost != null)
            {
                Destroy(openingMovieHost);
                openingMovieHost = null;
            }
            if (openingMovieTexture != null)
            {
                openingMovieTexture.Release();
                Destroy(openingMovieTexture);
                openingMovieTexture = null;
            }
        }
    }
}
