using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppBuilder {
    public class AppController : Controller {
        private readonly IBuildTaskDataService _service;
        private readonly ILogger _logger;

        public AppController(IBuildTaskDataService service, ILogger<AppController> logger) {
            _service = service;
            _logger = logger;
        }

        public object Index() {
            return "Working v1.0 2018";
        }

        public object Build(string user, string host) {
            if (string.IsNullOrWhiteSpace(user)) return "User can not be empty.";

            var (hasError, message) = _service.AddTask(user, host);
            var msg = hasError ? message : "添加成功";
            _logger.LogInformation($"添加打包任务，agent:{user}, host:{host}, 添加结果：{msg}");
            return msg;
        }

        /// <summary>
        /// 查询任务状态
        /// </summary>
        public IActionResult Check() {
            var model = _service.GetBuildState();
            return View(model);
        }
    }
}