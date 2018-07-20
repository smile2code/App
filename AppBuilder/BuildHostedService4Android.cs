using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBuilder {
    public class BuildHostedService4Android : IHostedService {
        private readonly BuildOptions _buildOptions;
        private readonly IHostingEnvironment _env;

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private Task _buildTask;
        private Task _checkTask;

        public BuildHostedService4Android(IHostingEnvironment env, ILogger<BuildHostedService4Android> logger,
            IOptions<BuildOptions> buildOptions, IServiceProvider serviceProvider) {
            _env = env;
            _logger = logger;
            _buildOptions = buildOptions.Value;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is starting.");
            _buildTask = Task.Run(BuildProcessing, _shutdown.Token);
            _checkTask = Task.Factory.StartNew(CheckProcessing, _shutdown.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is stopping.");
            _shutdown.Cancel();
            var tasks = new List<Task>();
            if (_buildTask != null) tasks.Add(_buildTask);
            if (_checkTask != null) tasks.Add(_checkTask);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// 获取打包任务并执行打包指令
        /// </summary>
        private async Task BuildProcessing() {
            var templateRoot = Path.Combine(_env.ContentRootPath, @"Resources\templates");
            var constantsTpl = await File.ReadAllTextAsync(Path.Combine(templateRoot, "Constants.kt"));
            var buildTpl = await File.ReadAllTextAsync(Path.Combine(templateRoot, "build.gradle"));

            var service = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IBuildTaskDataService>();
            service.ResetWorking();
            
            while (!_shutdown.IsCancellationRequested) {
                var model = service.TryLockATask();
                if (model == null) {
                    Thread.Sleep(10000);
                    continue;
                }

                _logger.LogInformation($"Build app for {model.AgentName}, host is {model.Host}.");

                var siteUrl = $"https://{model.Host}";
                if (string.IsNullOrWhiteSpace(model.Host)) siteUrl = _buildOptions.AgentDefaultUrl.Replace("$username", model.AgentName);

                var constants = constantsTpl.Replace("{siteUrl}", siteUrl).Replace("{updateUrl}", _buildOptions.UpdateUrl);
                var build = buildTpl.Replace("{agentName}", model.AgentName);

                var srcRoot = Path.Combine(_env.ContentRootPath, @"Resources\src");
                await File.WriteAllTextAsync(Path.Combine(srcRoot, @"app\src\app\java\Constants.kt"), constants);
                await File.WriteAllTextAsync(Path.Combine(srcRoot, @"app\build.gradle"), build);

                try {
                    // 执行打包命令，生成 app
                    var cmdsi = new ProcessStartInfo("cmd.exe") {
                        Arguments = @"/C gradle assembleRelease",
                        WorkingDirectory = srcRoot,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (var cmd = Process.Start(cmdsi)) {
                        cmd?.WaitForExit();
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "打包出错");
                }
            }
        }

        /// <summary>
        /// 检查打包是否成功
        /// </summary>
        private void CheckProcessing() {
            var appRoot = Path.Combine(_env.ContentRootPath, @"Resources\src\app\build\publish");
            var service = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IBuildTaskDataService>();

            while (!_shutdown.IsCancellationRequested) {
                var model = service.GetAWorkingTask();
                if (model == null) {
                    Thread.Sleep(10000);
                    continue;
                }

                // check file
                // TODO 如何获取版本信息
                var path = Path.Combine(appRoot, $"com.android.app{model.AgentName}");
                
                if (!Directory.Exists(path)) {
                    Thread.Sleep(10000);
                    continue;
                }
                
                var files = Directory.GetFiles(path, "app.apk", SearchOption.AllDirectories);
                if (files.Any()) {
                    service.FinishWork(model.Id, 2);
                }

                Thread.Sleep(10000);
            }
        }
    }
}