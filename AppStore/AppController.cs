using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AppStore {
    public class AppController : Controller {
        private readonly StoreOptions _storeOptions;

        public AppController(IOptions<StoreOptions> storeOptions) {
            _storeOptions = storeOptions.Value;
        }

        public object Index() {
            return "Working v1.0 2018";
        }

        /// <summary>
        /// 获取最新的版本信息，包含线路信息和缓存的版本信息
        /// </summary>
        public object GetLatest(string appid) {
            var li = GetLatestInfo(appid);
            if (li == null) {
                return new LatestInfo();
            }

            // sites.json 从 appid 根目录获取
            var sites = new List<Site>();
            var root = Path.Combine(_storeOptions.RootPath, appid);
            var siteConfigFile = Path.Combine(root, "sites.json");
            if (System.IO.File.Exists(siteConfigFile)) {
                var json = System.IO.File.ReadAllText(siteConfigFile);
                sites = JsonConvert.DeserializeObject<List<Site>>(json);
            }

            return new LatestInfo {
                AppVer = li.VerInfo.AppVer,
                WebVer = li.VerInfo.WebVer,
                Sites = sites
            };
        }

        /// <summary>
        /// 下载 app
        /// </summary>
        public object DownloadApp(string appid) {
            var li = GetLatestInfo(appid);
            if (li == null || li.VerInfo.AppVer == 0) {
                return Error("获取安装包信息失败！");
            }

            var root = Path.GetDirectoryName(li.File);
            if (root == null) {
                return Error("安装包不存在！");
            }
            var appFile = Path.Combine(root, li.VerInfo.AppFile);
            return !System.IO.File.Exists(appFile)
                ? Error("安装包不存在！")
                : PhysicalFile(appFile, MediaTypeNames.Application.Octet, li.VerInfo.AppFile);
        }

        /// <summary>
        /// 下载 web 缓存
        /// </summary>
        public object DownloadWeb(string appid) {
            var li = GetLatestInfo(appid);
            if (li == null || li.VerInfo.AppVer == 0
                           || li.VerInfo.WebVer == 0) {
                return Error("获取安装包信息失败！");
            }

            var webFile = Path.Combine(_storeOptions.RootPath, appid, $"web\\{li.VerInfo.WebVer}.zip");
            return !System.IO.File.Exists(webFile) ? Error("Web缓存不存在！") : PhysicalFile(webFile, MediaTypeNames.Application.Zip, "web.zip");
        }

        private VersionFile GetLatestInfo(string appid) {
            if (string.IsNullOrWhiteSpace(appid)
                || !Regex.IsMatch(appid, @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$")) {
                return null;
            }

            var root = Path.Combine(_storeOptions.RootPath, appid);
            if (!Directory.Exists(root)) {
                return null;
            }

            var versionFiles = Directory.GetFiles(root, "version.json", SearchOption.AllDirectories);
            if (versionFiles.Length == 0) {
                // 没有 version 文件
                return null;
            }

            // app 的最新版本信息
            var vi = versionFiles
                .Select(x => new VersionFile {File = x, VerInfo = JsonConvert.DeserializeObject<VersionInfo>(System.IO.File.ReadAllText(x))})
                .OrderByDescending(x => x.VerInfo.AppVer)
                .First();

            // web 的最新版本信息
            var webRoot = Path.Combine(root, "web");
            if (!Directory.Exists(webRoot)) {
                return vi;
            }

            var webFiles = Directory.GetFiles(webRoot, "*.zip");
            if (webFiles.Length == 0) {
                return vi;
            }

            // web 的最新版本（文件名格式：{版本号}.zip）
            vi.VerInfo.WebVer = webFiles.Select(x => Convert.ToInt32(Path.GetFileNameWithoutExtension(x))).Max();

            return vi;
        }

        /// <summary>
        /// 下载app
        /// </summary>
        public ActionResult Download(string id /* 代理用户名称 */) {
            var appid = $"com.android.app{id}";
            var li = GetLatestInfo(appid);
            if (li == null || li.VerInfo.AppVer == 0) {
                appid = "com.android.app286"; // 默认为主站的app
                li = GetLatestInfo(appid);
            }

            if (li == null || li.VerInfo.AppVer == 0) {
                return NotFound("获取安装包信息失败！");
            }

            var root = Path.GetDirectoryName(li.File);
            if (root == null) {
                return NotFound("安装包不存在！");
            }
            var appFile = Path.Combine(root, li.VerInfo.AppFile);
            return !System.IO.File.Exists(appFile)
                ? (ActionResult) NotFound("安装包不存在！")
                : PhysicalFile(appFile, MediaTypeNames.Application.Octet, li.VerInfo.AppFile);
        }

        private object Error(string message = "操作失败") {
            return new CommonResponseModel {
                HasError = true,
                Message = message
            };
        }

        /// <summary>
        /// 检查 app 是否存在
        /// </summary>
        public object Check(string id) {
            var appid = $"com.android.app{id}";
            var li = GetLatestInfo(appid);
            if (li == null || li.VerInfo.AppVer == 0) {
                return "安装包不存在";
            }
            return $"app 版本号：{li.VerInfo.AppVer}";
        }
    }

    #region models

    public class VersionInfo {
        /// <summary>
        /// app 版本
        /// </summary>
        public int AppVer { get; set; }

        /// <summary>
        /// 网页版本
        /// </summary>
        public int WebVer { get; set; }

        /// <summary>
        /// app 文件名
        /// </summary>
        public string AppFile { get; set; }
    }

    public class VersionFile {
        /// <summary>
        /// 版本信息
        /// </summary>
        public VersionInfo VerInfo { get; set; }

        /// <summary>
        /// 版本信息对应的文件路径
        /// </summary>
        public string File { get; set; }
    }

    /// <summary>
    /// 站点切换信息
    /// </summary>
    public class Site {
        /// <summary>
        /// 站点名称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 站点路径
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// 最新版信息
    /// </summary>
    public class LatestInfo {
        /// <summary>
        /// app 版本
        /// </summary>
        public int AppVer { get; set; }

        /// <summary>
        /// 网页版本
        /// </summary>
        public int WebVer { get; set; }

        /// <summary>
        /// 线路切换的站点信息
        /// </summary>
        public List<Site> Sites { get; set; }

        /// <summary>
        /// 选项（local或空或null 时用本地 html，其它值从服务器下载html，用于伪站)
        /// </summary>
        public string Option => "remote";
    }

    public class CommonResponseModel {
        public bool HasError { get; set; }
        public string Message { get; set; }
    }

    #endregion
}