
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace SilverPillar.Core.Editor
{
    public enum GitSyncMode
    {
        Automatic,
        Notify,
        Manual
    }

    [InitializeOnLoad]
    public static class SilverPillarGitAutoSync
    {
        #region Configuration

        private const string Repository =
            "https://github.com/SantiagoHerreraH/Unity-Frameworks.git";

        private const string Branch = "main";
        private const string PackagePrefix = "com.silverpillar.";
        private const string PackageRoot = "/packages/";

        private const int MinInterval = 30;
        private const int MaxInterval = 3600;
        private const int GitTimeoutMs = 15000;
        private const int UpdateCooldownSeconds = 120;

        private const string MenuRoot =
            "Tools/SilverPillar/Git Sync/";

        private static readonly string SettingsKey =
            "SilverPillar.GitSync." +
            Hash128.Compute(Application.dataPath) + ".";

        #endregion

        #region State

        private enum RequestedAction
        {
            None,
            Check,
            Update
        }

        private static RequestedAction s_RequestedAction;

        private static ListRequest s_ListRequest;
        private static Task<string> s_GitTask;
        private static AddAndRemoveRequest s_UpdateRequest;

        // Cached list of installed, eligible Git packages.
        private static readonly List<PackageInfo>
            s_EligiblePackages = new List<PackageInfo>();

        private static bool s_PackagesCached;

        // Used to discard results made stale by package changes.
        private static int s_PackageGeneration;
        private static int s_ListGeneration;
        private static int s_GitGeneration;

        private static double s_NextCheck;

        private static string s_RemoteHash;
        private static string s_AvailableHash;

        private static string[] s_PendingUrls =
            Array.Empty<string>();

        private static string s_LastError;
        private static string s_Status = "Initializing";

        #endregion

        #region Public Properties

        public static GitSyncMode Mode
        {
            get => (GitSyncMode)EditorPrefs.GetInt(
                SettingsKey + "Mode",
                (int)GitSyncMode.Automatic);

            set
            {
                EditorPrefs.SetInt(
                    SettingsKey + "Mode",
                    (int)value);

                s_NextCheck = 0;

                if (value == GitSyncMode.Manual)
                    s_RequestedAction = RequestedAction.None;
            }
        }

        public static int CheckIntervalSeconds
        {
            get => EditorPrefs.GetInt(
                SettingsKey + "Interval",
                60);

            set
            {
                EditorPrefs.SetInt(
                    SettingsKey + "Interval",
                    Mathf.Clamp(
                        value,
                        MinInterval,
                        MaxInterval));

                s_NextCheck = 0;
            }
        }

        public static bool IsBusy =>
            s_ListRequest != null ||
            s_GitTask != null ||
            s_UpdateRequest != null;

        public static string RemoteHash => s_RemoteHash;
        public static string AvailableHash => s_AvailableHash;
        public static string LastError => s_LastError;
        public static string Status => s_Status;

        public static int EligiblePackageCount =>
            s_EligiblePackages.Count;

        public static bool PackagesCached => s_PackagesCached;

        #endregion

        #region Initialization

        static SilverPillarGitAutoSync()
        {
            s_NextCheck =
                EditorApplication.timeSinceStartup + 10;

            EditorApplication.update += Update;

            Events.registeredPackages +=
                OnPackagesRegistered;
        }

        private static void OnPackagesRegistered(
            PackageRegistrationEventArgs args)
        {
            InvalidatePackageCache();

            // Recheck promptly after package changes.
            if (Mode != GitSyncMode.Manual)
                s_NextCheck = 0;
        }

        private static void InvalidatePackageCache()
        {
            s_PackageGeneration++;

            s_PackagesCached = false;
            s_EligiblePackages.Clear();

            s_RemoteHash = null;
            s_AvailableHash = null;
            s_PendingUrls = Array.Empty<string>();

            s_Status = "Package list changed";
        }

        #endregion

        #region Menu Items

        [MenuItem(MenuRoot + "Settings")]
        private static void OpenSettings()
        {
            SilverPillarGitSyncWindow.ShowWindow();
        }

        [MenuItem(MenuRoot + "Check Now")]
        public static void CheckNow()
        {
            RequestAction(RequestedAction.Check);
        }

        [MenuItem(MenuRoot + "Update Now")]
        public static void UpdateNow()
        {
            RequestAction(RequestedAction.Update);
        }

        private static void RequestAction(
            RequestedAction action)
        {
            // An update request takes priority over a check.
            if (action == RequestedAction.Update ||
                s_RequestedAction == RequestedAction.None)
            {
                s_RequestedAction = action;
            }

            s_NextCheck = 0;
        }

        #endregion

        #region Main Update Loop

        private static void Update()
        {
            // Process existing operations before starting new ones.
            if (s_UpdateRequest != null)
            {
                ProcessUpdateRequest();
                return;
            }

            if (s_ListRequest != null)
            {
                ProcessListRequest();
                return;
            }

            if (s_GitTask != null)
            {
                ProcessGitTask();
                return;
            }

            if (!IsEditorReady())
                return;

            if (EditorApplication.timeSinceStartup <
                s_NextCheck)
                return;

            bool manualRequest =
                s_RequestedAction != RequestedAction.None;

            if (Mode == GitSyncMode.Manual &&
                !manualRequest)
                return;

            s_NextCheck =
                EditorApplication.timeSinceStartup +
                CheckIntervalSeconds;

            // Only query installed packages if needed.
            if (!s_PackagesCached)
            {
                StartPackageList();
                return;
            }

            // No eligible Git packages:
            // Do not contact GitHub.
            if (s_EligiblePackages.Count == 0)
            {
                s_RequestedAction = RequestedAction.None;
                s_AvailableHash = null;
                s_PendingUrls = Array.Empty<string>();
                s_Status = "No eligible Git packages";
                return;
            }

            StartGitCheck();
        }

        private static bool IsEditorReady()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   !EditorApplication.isCompiling &&
                   !EditorApplication.isUpdating;
        }

        #endregion

        #region Package Discovery

        private static void StartPackageList()
        {
            try
            {
                s_Status = "Scanning installed packages";

                s_ListGeneration = s_PackageGeneration;

                // Offline: local package information only.
                // false: direct dependencies only.
                s_ListRequest = Client.List(true, false);
            }
            catch (Exception ex)
            {
                ReportError(
                    "Could not list installed packages", ex);
            }
        }

        private static void ProcessListRequest()
        {
            if (!s_ListRequest.IsCompleted)
                return;

            var request = s_ListRequest;
            s_ListRequest = null;

            if (request.Status != StatusCode.Success)
            {
                ReportError(
                    "Package list failed: " + request.Error);
                return;
            }

            // Packages changed while the request was running.
            // Discard the stale result and rescan.
            if (s_ListGeneration != s_PackageGeneration)
            {
                s_NextCheck = 0;
                return;
            }

            s_EligiblePackages.Clear();

            foreach (PackageInfo package in request.Result)
            {
                if (!IsEligiblePackage(package))
                    continue;

                s_EligiblePackages.Add(package);
            }

            s_PackagesCached = true;

            if (s_EligiblePackages.Count == 0)
            {
                s_Status = "No eligible Git packages";
                s_AvailableHash = null;
                s_PendingUrls = Array.Empty<string>();
                s_RequestedAction = RequestedAction.None;

                return;
            }

            // GitHub is only queried when at least one
            // eligible package exists.
            StartGitCheck();
        }

        private static bool IsEligiblePackage(
            PackageInfo package)
        {
            if (package == null)
                return false;

            if (!package.name.StartsWith(
                    PackagePrefix,
                    StringComparison.Ordinal))
                return false;

            // Ignore local, embedded and registry packages.
            if (package.source != PackageSource.Git ||
                package.git == null)
                return false;

            if (string.IsNullOrEmpty(package.packageId))
                return false;

            // Ensure this package belongs to our monorepo.
            if (package.packageId.IndexOf(
                    Repository,
                    StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }

        #endregion

        #region Git Operations

        private static void StartGitCheck()
        {
            s_Status = "Checking GitHub";
            s_LastError = null;

            s_GitGeneration = s_PackageGeneration;

            // Do not block Unity's main thread.
            s_GitTask = Task.Run(GetLatestCommitHash);
        }

        private static string GetLatestCommitHash()
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "git",

                    Arguments =
                        "ls-remote --heads \"" +
                        Repository + "\" " +
                        "refs/heads/" + Branch,

                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.Start();

                // Read both streams asynchronously to avoid
                // blocking on full output buffers.
                Task<string> stdout =
                    process.StandardOutput.ReadToEndAsync();

                Task<string> stderr =
                    process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(GitTimeoutMs))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process might have already exited.
                    }

                    throw new TimeoutException(
                        "Git remote check timed out.");
                }

                string output =
                    stdout.GetAwaiter().GetResult();

                string error =
                    stderr.GetAwaiter().GetResult();

                if (process.ExitCode != 0)
                {
                    throw new Exception(
                        "git ls-remote failed: " + error);
                }

                string[] parts = output.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2 ||
                    !IsValidCommitHash(parts[0]))
                {
                    throw new Exception(
                        "Could not resolve the GitHub branch.");
                }

                return parts[0];
            }
        }

        private static bool IsValidCommitHash(string hash)
        {
            if (string.IsNullOrEmpty(hash) ||
                hash.Length != 40)
                return false;

            foreach (char c in hash)
            {
                bool hex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');

                if (!hex)
                    return false;
            }

            return true;
        }

        private static void ProcessGitTask()
        {
            if (!s_GitTask.IsCompleted)
                return;

            var task = s_GitTask;
            s_GitTask = null;

            try
            {
                string remoteHash =
                    task.GetAwaiter().GetResult();

                // Do not update from stale package information.
                if (s_GitGeneration != s_PackageGeneration)
                {
                    s_NextCheck = 0;
                    return;
                }

                s_RemoteHash = remoteHash;

                BuildPendingUpdates();

                HandleAvailableUpdates();
            }
            catch (Exception ex)
            {
                ReportError("GitHub check failed", ex);

                // Do not lose an explicit Update Now request.
                // It can be retried by the user.
                s_RequestedAction = RequestedAction.None;
            }
        }

        #endregion

        #region Update Detection

        private static void BuildPendingUpdates()
        {
            var urls = new List<string>();

            foreach (PackageInfo package in s_EligiblePackages)
            {
                if (string.Equals(
                        package.git.hash,
                        s_RemoteHash,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                string url =
                    Repository +
                    "?path=" + PackageRoot +
                    package.name +
                    "#" + s_RemoteHash;

                urls.Add(url);
            }

            s_PendingUrls = urls.ToArray();

            s_AvailableHash =
                urls.Count > 0 ? s_RemoteHash : null;

            s_Status = urls.Count > 0
                ? urls.Count + " update(s) available"
                : "All Git packages are up to date";
        }

        private static void HandleAvailableUpdates()
        {
            RequestedAction requested = s_RequestedAction;
            s_RequestedAction = RequestedAction.None;

            if (s_PendingUrls.Length == 0)
                return;

            bool explicitUpdate =
                requested == RequestedAction.Update;

            if (explicitUpdate)
            {
                InstallUpdates(true);
                return;
            }

            if (Mode == GitSyncMode.Automatic)
            {
                InstallUpdates(false);
                return;
            }

            if (Mode == GitSyncMode.Notify &&
                requested == RequestedAction.None)
            {
                NotifyAboutUpdate();
            }

            // Manual checks only update the displayed status.
        }

        #endregion

        #region Notifications

        private static void NotifyAboutUpdate()
        {
            string hash = s_AvailableHash;

            if (string.IsNullOrEmpty(hash))
                return;

            // Notify only once per commit per project.
            if (EditorPrefs.GetString(
                    SettingsKey + "NotifiedHash", "") == hash)
                return;

            EditorPrefs.SetString(
                SettingsKey + "NotifiedHash", hash);

            EditorApplication.delayCall += () =>
            {
                if (Mode != GitSyncMode.Notify ||
                    !string.Equals(
                        s_AvailableHash,
                        hash,
                        StringComparison.OrdinalIgnoreCase))
                    return;

                bool update = EditorUtility.DisplayDialog(
                    "SilverPillar Update Available",

                    "A new SilverPillar framework commit " +
                    "is available.\n\n" +
                    "Commit: " + ShortHash(hash) +
                    "\n\nUpdate the installed Git packages?",

                    "Update Now",
                    "Later");

                if (update)
                    UpdateNow();
            };
        }

        #endregion

        #region Package Installation

        private static void InstallUpdates(bool explicitUpdate)
        {
            if (s_PendingUrls == null ||
                s_PendingUrls.Length == 0)
                return;

            if (!IsEditorReady())
            {
                if (explicitUpdate)
                    s_RequestedAction = RequestedAction.Update;

                s_NextCheck = 0;
                return;
            }

            if (!explicitUpdate &&
                IsRecentUpdateAttempt(s_AvailableHash))
            {
                s_Status =
                    "Update cooldown (recent attempt)";
                return;
            }

            string hash = s_AvailableHash;

            RecordUpdateAttempt(hash);

            try
            {
                s_Status =
                    "Installing " + s_PendingUrls.Length +
                    " package update(s)";

                Debug.Log(
                    "[SilverPillar Git Sync] Updating " +
                    s_PendingUrls.Length +
                    " package(s) to " +
                    ShortHash(hash));

                // Update every package in one resolution.
                s_UpdateRequest = Client.AddAndRemove(
                    s_PendingUrls,
                    Array.Empty<string>());

                // The request can cause an assembly reload,
                // including a reload of this updater.
                // SessionState retains the attempt information.
            }
            catch (Exception ex)
            {
                ClearUpdateAttempt();
                ReportError("Could not start package update", ex);
            }
        }

        private static void ProcessUpdateRequest()
        {
            if (!s_UpdateRequest.IsCompleted)
                return;

            var request = s_UpdateRequest;
            s_UpdateRequest = null;

            if (request.Status == StatusCode.Success)
            {
                Debug.Log(
                    "[SilverPillar Git Sync] " +
                    "Packages updated successfully.");

                s_Status = "Update completed";
                s_LastError = null;

                ClearUpdateAttempt();

                // Installed hashes are now different.
                // Discard the previous cached snapshot.
                InvalidatePackageCache();

                s_NextCheck =
                    EditorApplication.timeSinceStartup + 5;
            }
            else
            {
                ReportError(
                    "Package update failed: " +
                    request.Error);

                // Keep the cooldown so a failed automatic
                // update does not immediately retry forever.
                s_NextCheck =
                    EditorApplication.timeSinceStartup +
                    CheckIntervalSeconds;
            }
        }

        #endregion

        #region Self-Update Protection

        private static void RecordUpdateAttempt(string hash)
        {
            SessionState.SetString(
                SettingsKey + "AttemptedHash",
                hash ?? "");

            SessionState.SetString(
                SettingsKey + "AttemptedTicks",
                DateTime.UtcNow.Ticks.ToString());
        }

        private static bool IsRecentUpdateAttempt(
            string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            string previousHash =
                SessionState.GetString(
                    SettingsKey + "AttemptedHash",
                    "");

            if (!string.Equals(
                    previousHash,
                    hash,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            string value =
                SessionState.GetString(
                    SettingsKey + "AttemptedTicks",
                    "0");

            if (!long.TryParse(value, out long ticks) ||
                ticks <= 0)
                return false;

            try
            {
                DateTime lastAttempt =
                    new DateTime(ticks, DateTimeKind.Utc);

                double elapsed =
                    (DateTime.UtcNow - lastAttempt)
                    .TotalSeconds;

                return elapsed >= 0 &&
                       elapsed < UpdateCooldownSeconds;
            }
            catch
            {
                return false;
            }
        }

        private static void ClearUpdateAttempt()
        {
            SessionState.EraseString(
                SettingsKey + "AttemptedHash");

            SessionState.EraseString(
                SettingsKey + "AttemptedTicks");
        }

        #endregion

        #region Utilities

        private static string ShortHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return "Unknown";

            return hash.Length > 8
                ? hash.Substring(0, 8)
                : hash;
        }

        private static void ReportError(
            string message,
            Exception exception = null)
        {
            s_LastError = exception == null
                ? message
                : message + ": " + exception.Message;

            s_Status = "Error";

            Debug.LogError(
                "[SilverPillar Git Sync] " + s_LastError);
        }

        #endregion
    }

    public sealed class SilverPillarGitSyncWindow : EditorWindow
    {
        [MenuItem("Tools/SilverPillar/Git Sync/Open Window")]
        public static void ShowWindow()
        {
            GetWindow<SilverPillarGitSyncWindow>(
                "SilverPillar Git Sync");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "SilverPillar Frameworks",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Synchronizes SilverPillar Git packages " +
                "with the main branch of " +
                "SantiagoHerreraH/Unity-Frameworks.\n\n" +
                "Local and embedded packages are ignored.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            DrawSettings();

            EditorGUILayout.Space(12);

            DrawStatus();

            EditorGUILayout.Space(12);

            DrawActions();
        }

        private static void DrawSettings()
        {
            EditorGUILayout.LabelField(
                "Synchronization Settings",
                EditorStyles.boldLabel);

            GitSyncMode currentMode =
                SilverPillarGitAutoSync.Mode;

            GitSyncMode newMode =
                (GitSyncMode)EditorGUILayout.EnumPopup(
                    "Mode",
                    currentMode);

            if (newMode != currentMode)
                SilverPillarGitAutoSync.Mode = newMode;

            if (newMode != GitSyncMode.Manual)
            {
                int currentInterval =
                    SilverPillarGitAutoSync
                        .CheckIntervalSeconds;

                int newInterval =
                    EditorGUILayout.IntField(
                        "Check Interval (seconds)",
                        currentInterval);

                if (newInterval != currentInterval)
                {
                    SilverPillarGitAutoSync
                        .CheckIntervalSeconds = newInterval;
                }
            }

            EditorGUILayout.Space(5);

            switch (newMode)
            {
                case GitSyncMode.Automatic:
                    EditorGUILayout.HelpBox(
                        "Checks GitHub periodically and " +
                        "automatically installs detected updates.",
                        MessageType.Warning);
                    break;

                case GitSyncMode.Notify:
                    EditorGUILayout.HelpBox(
                        "Checks GitHub periodically and " +
                        "asks before installing updates.",
                        MessageType.Info);
                    break;

                case GitSyncMode.Manual:
                    EditorGUILayout.HelpBox(
                        "Only checks or updates when requested.",
                        MessageType.Info);
                    break;
            }
        }

        private static void DrawStatus()
        {
            EditorGUILayout.LabelField(
                "Status",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Current State",
                SilverPillarGitAutoSync.Status);

            string remote =
                SilverPillarGitAutoSync.RemoteHash;

            EditorGUILayout.LabelField(
                "Latest GitHub Commit",
                FormatHash(remote));

            string available =
                SilverPillarGitAutoSync.AvailableHash;

            EditorGUILayout.LabelField(
                "Available Update",
                FormatHash(available));

            EditorGUILayout.LabelField(
                "Eligible Git Packages",
                SilverPillarGitAutoSync.PackagesCached
                    ? SilverPillarGitAutoSync
                        .EligiblePackageCount.ToString()
                    : "Not scanned");

            if (SilverPillarGitAutoSync.IsBusy)
            {
                EditorGUILayout.HelpBox(
                    "Synchronization operation in progress.",
                    MessageType.Info);
            }

            string error =
                SilverPillarGitAutoSync.LastError;

            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(
                    error,
                    MessageType.Error);
            }
        }

        private static void DrawActions()
        {
            EditorGUILayout.LabelField(
                "Actions",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(
                SilverPillarGitAutoSync.IsBusy))
            {
                if (GUILayout.Button(
                    "Check for Updates",
                    GUILayout.Height(28)))
                {
                    SilverPillarGitAutoSync.CheckNow();
                }

                if (GUILayout.Button(
                    "Update to Latest",
                    GUILayout.Height(28)))
                {
                    SilverPillarGitAutoSync.UpdateNow();
                }
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Package updates may trigger Unity " +
                "recompilation and assembly reloads.\n\n" +
                "After testing an update, commit " +
                "Packages/manifest.json and " +
                "Packages/packages-lock.json to your " +
                "project's version control.",
                MessageType.Warning);
        }

        private static string FormatHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return "None";

            return hash.Length > 8
                ? hash.Substring(0, 8)
                : hash;
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}

#endif
