using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace ET
{
    public static class TortoiseGitMenu
    {
        private const string CustomTortoiseGitProcKey = "ET.TortoiseGitProc.Path";
        private const string MenuRoot = "ET/Git/";
        private const string TortoiseGitProcName = "TortoiseGitProc.exe";
        private const string PullRequestBaseBranch = "Develop";
        private const int GitMenuPriority = 2000;

        private static bool creatingPullRequest;

        [MenuItem(MenuRoot + "Pull", false, GitMenuPriority)]
        private static void Pull()
        {
            ExecuteGitCommand("pull", "Pull");
        }

        [MenuItem(MenuRoot + "Pull", true)]
        private static bool ValidatePull()
        {
            return IsWindowsEditor() && HasGitRepository();
        }

        [Shortcut("ET/Git/Pull", KeyCode.F5)]
        private static void PullShortcut()
        {
            if (ValidatePull())
            {
                Pull();
            }
        }

        [MenuItem(MenuRoot + "Commit", false, GitMenuPriority + 1)]
        private static void Commit()
        {
            ExecuteGitCommand("commit", "Commit");
        }

        [MenuItem(MenuRoot + "Commit", true)]
        private static bool ValidateCommit()
        {
            return IsWindowsEditor() && HasGitRepository();
        }

        [Shortcut("ET/Git/Commit", KeyCode.F4)]
        private static void CommitShortcut()
        {
            if (ValidateCommit())
            {
                Commit();
            }
        }

        [MenuItem(MenuRoot + "Push", false, GitMenuPriority + 2)]
        private static void Push()
        {
            ExecuteGitCommand("push", "Push");
        }

        [MenuItem(MenuRoot + "Push", true)]
        private static bool ValidatePush()
        {
            return IsWindowsEditor() && HasGitRepository();
        }

        [Shortcut("ET/Git/Push", KeyCode.F5)]
        private static void PushShortcut()
        {
            if (ValidatePush())
            {
                Push();
            }
        }

        /// <summary>
        /// 为当前已推送的分支发起指向 Develop 的 GitHub Pull Request
        /// </summary>
        [MenuItem(MenuRoot + "Create Pull Request", false, GitMenuPriority + 3)]
        private static async void CreatePullRequest()
        {
            if (creatingPullRequest || !TryGetProjectRoot(out string projectRoot))
            {
                return;
            }

            creatingPullRequest = true;
            try
            {
                string gitPath = FindExecutableInPath("git.exe");
                if (string.IsNullOrEmpty(gitPath))
                {
                    throw new InvalidOperationException("PATH 中未找到 git.exe，请安装 Git 并重启 Unity 编辑器");
                }

                string headBranch = await RunPullRequestCommandAsync(gitPath, projectRoot, "branch", "--show-current");
                if (string.IsNullOrEmpty(headBranch) || headBranch == PullRequestBaseBranch)
                {
                    throw new InvalidOperationException($"请先切换到 {PullRequestBaseBranch} 之外的工作分支");
                }

                string remoteUrl = await RunPullRequestCommandAsync(gitPath, projectRoot, "remote", "get-url", "origin");
                string repository = GetGitHubRepository(remoteUrl);
                string headRef = $"refs/heads/{headBranch}";
                string baseRef = $"refs/heads/{PullRequestBaseBranch}";
                string localCommit = await RunPullRequestCommandAsync(gitPath, projectRoot, "rev-parse", "--verify", headRef);
                string remoteRefs = await RunPullRequestCommandAsync(gitPath, projectRoot, "ls-remote", "--refs", "origin", baseRef, headRef);
                string[] remoteLines = remoteRefs.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (!Array.Exists(remoteLines, line => line.EndsWith($"\t{baseRef}", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException($"origin 上没有目标分支 {PullRequestBaseBranch}");
                }

                if (!Array.Exists(remoteLines, line => line == $"{localCommit}\t{headRef}"))
                {
                    throw new InvalidOperationException($"{headBranch} 的本地提交与 origin 不一致，请通过 ET/Git 的 Pull 或 Push 同步后重试");
                }

                string githubCliPath = FindExecutableInPath("gh.exe");
                if (string.IsNullOrEmpty(githubCliPath))
                {
                    string compareUrl = $"https://github.com/{repository}/compare/{PullRequestBaseBranch}...{Uri.EscapeDataString(headBranch)}?quick_pull=1";
                    Application.OpenURL(compareUrl);
                    return;
                }

                string githubRepository = $"github.com/{repository}";
                string pullRequestUrl = await RunPullRequestCommandAsync(githubCliPath, projectRoot,
                    "pr", "list", "--repo", githubRepository, "--base", PullRequestBaseBranch, "--head", headBranch,
                    "--state", "open", "--json", "url,isCrossRepository", "--jq", "[.[] | select(.isCrossRepository == false)][0].url // empty");
                if (string.IsNullOrEmpty(pullRequestUrl))
                {
                    // 显式指定远端源分支，避免 gh 在创建过程中发起推送或 fork
                    pullRequestUrl = await RunPullRequestCommandAsync(githubCliPath, projectRoot,
                        "pr", "create", "--repo", githubRepository, "--base", PullRequestBaseBranch, "--head", headBranch, "--fill");
                }

                Application.OpenURL(pullRequestUrl);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("发起 Pull Request 失败", e.Message, "确定");
            }
            finally
            {
                creatingPullRequest = false;
            }
        }

        /// <summary>
        /// 判断是否可以在当前 Windows 编辑器中发起 Pull Request
        /// </summary>
        [MenuItem(MenuRoot + "Create Pull Request", true)]
        private static bool ValidateCreatePullRequest()
        {
            return IsWindowsEditor() && HasGitRepository() && !creatingPullRequest;
        }

        /// <summary>
        /// 从 GitHub 的 HTTPS 或 SSH 远端地址提取仓库的所有者和名称
        /// </summary>
        private static string GetGitHubRepository(string remoteUrl)
        {
            Match match = Regex.Match(remoteUrl,
                @"\A(?:https://(?:[^/@\s]+@)?github\.com/|ssh://git@github\.com/|git@github\.com:)([a-z0-9-]+/[a-z0-9_.-]+?)(?:\.git)?/?\z",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                throw new InvalidOperationException("origin 必须指向 GitHub 仓库的 HTTPS 或 SSH 地址");
            }

            return match.Groups[1].Value;
        }

        /// <summary>
        /// 运行 Git 或 GitHub CLI 并异步读取命令输出
        /// </summary>
        private static async Task<string> RunPullRequestCommandAsync(string exePath, string projectRoot, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(startInfo);
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(output, error, Task.Run(process.WaitForExit));
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{Path.GetFileName(exePath)} {arguments[0]} 执行失败（{process.ExitCode}）\n{error.Result.Trim()}");
            }

            return output.Result.Trim();
        }

        [MenuItem(MenuRoot + "Select TortoiseGitProc", false, GitMenuPriority + 20)]
        private static void SelectTortoiseGitProc()
        {
            string defaultDirectory = GetDefaultTortoiseGitDirectory();
            string selectedPath = EditorUtility.OpenFilePanel("选择 TortoiseGitProc.exe", defaultDirectory, "exe");
            if (string.IsNullOrEmpty(selectedPath))
            {
                return;
            }

            if (!string.Equals(Path.GetFileName(selectedPath), TortoiseGitProcName, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("选择失败", "请选择 TortoiseGitProc.exe。", "确定");
                return;
            }

            EditorPrefs.SetString(CustomTortoiseGitProcKey, selectedPath);
            Debug.Log($"TortoiseGitProc 已设置为: {selectedPath}");
        }

        [MenuItem(MenuRoot + "Clear Custom TortoiseGitProc", false, GitMenuPriority + 21)]
        private static void ClearCustomTortoiseGitProc()
        {
            EditorPrefs.DeleteKey(CustomTortoiseGitProcKey);
            Debug.Log("已清除自定义 TortoiseGitProc 路径。");
        }

        private static void ExecuteGitCommand(string command, string displayName)
        {
            if (!TryGetProjectRoot(out string projectRoot))
            {
                EditorUtility.DisplayDialog("Git 命令不可用", "未找到当前 Unity 工程的 Git 仓库根目录。", "确定");
                return;
            }

            if (!TryGetTortoiseGitProcPath(out string exePath))
            {
                bool shouldSelect = EditorUtility.DisplayDialog(
                    "未找到 TortoiseGit",
                    "没有找到 TortoiseGitProc.exe。\n你可以先通过 ET/Git/Select TortoiseGitProc 指定安装路径。",
                    "立即选择",
                    "取消");

                if (shouldSelect)
                {
                    SelectTortoiseGitProc();
                }

                return;
            }

            string arguments = $"/command:{command} /path:\"{projectRoot}\" /closeonend:1";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WorkingDirectory = projectRoot,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                });
                Debug.Log($"{displayName} 已启动: {projectRoot}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("启动失败", $"无法启动 TortoiseGit {displayName}。\n{e.Message}", "确定");
            }
        }

        private static bool TryGetProjectRoot(out string projectRoot)
        {
            DirectoryInfo directory = new DirectoryInfo(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));

            while (directory != null)
            {
                string gitPath = Path.Combine(directory.FullName, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    projectRoot = directory.FullName;
                    return true;
                }

                directory = directory.Parent;
            }

            projectRoot = string.Empty;
            return false;
        }

        private static bool HasGitRepository()
        {
            return TryGetProjectRoot(out _);
        }

        private static bool TryGetTortoiseGitProcPath(out string exePath)
        {
            string customPath = EditorPrefs.GetString(CustomTortoiseGitProcKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            {
                exePath = customPath;
                return true;
            }

            string[] candidatePaths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TortoiseGit", "bin", TortoiseGitProcName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "TortoiseGit", "bin", TortoiseGitProcName),
            };

            foreach (string candidatePath in candidatePaths)
            {
                if (File.Exists(candidatePath))
                {
                    exePath = candidatePath;
                    return true;
                }
            }

            string pathExecutable = FindExecutableInPath(TortoiseGitProcName);
            if (!string.IsNullOrEmpty(pathExecutable))
            {
                exePath = pathExecutable;
                return true;
            }

            exePath = string.Empty;
            return false;
        }

        private static string FindExecutableInPath(string executableName)
        {
            string environmentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] pathEntries = environmentPath.Split(Path.PathSeparator);

            foreach (string pathEntry in pathEntries)
            {
                if (string.IsNullOrWhiteSpace(pathEntry))
                {
                    continue;
                }

                string fullPath = Path.Combine(pathEntry.Trim().Trim('"'), executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return string.Empty;
        }

        private static string GetDefaultTortoiseGitDirectory()
        {
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TortoiseGit", "bin");
            if (Directory.Exists(defaultPath))
            {
                return defaultPath;
            }

            defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "TortoiseGit", "bin");
            if (Directory.Exists(defaultPath))
            {
                return defaultPath;
            }

            return Application.dataPath;
        }

        private static bool IsWindowsEditor()
        {
            return Application.platform == RuntimePlatform.WindowsEditor;
        }
    }
}
