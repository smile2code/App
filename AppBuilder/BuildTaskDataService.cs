using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

namespace AppBuilder {
    public interface IBuildTaskDataService {
        /// <summary>
        /// 添加任务
        /// </summary>
        (bool, string) AddTask(string user, string host);

        /// <summary>
        /// 锁定一个任务
        /// </summary>
        BuildTask TryLockATask();

        /// <summary>
        /// 获取一个正在打包的任务
        /// </summary>
        BuildTask GetAWorkingTask();

        void FinishWork(int id, int state);

        /// <summary>
        /// 重置工作中的任务（软件异常退出后重启时重置）
        /// </summary>
        void ResetWorking();

        BuildStateViewModel GetBuildState();
    }

    public class BuildTaskDataService : IBuildTaskDataService {
        private readonly BuildTaskDbContext _db;

        public BuildTaskDataService(BuildTaskDbContext db) {
            _db = db;
        }

        public (bool, string) AddTask(string user, string host) {
            var task = _db.BuildTask.FirstOrDefault(x => x.AgentName == user);
            if (task == null) {
                // add new
                _db.BuildTask.Add(new BuildTask {
                    AgentName = user,
                    CreateAt = DateTime.Now,
                    Host = host,
                    State = 0
                });
                _db.SaveChanges();

                return (false, "");
            }

            // edit
            switch (task.State) {
                case 0:
                    return (true, "添加失败，已存在");
                case 1:
                    return (true, "添加失败，处理中");
                default: // 2 - 处理完成
                    task.State = 0;
                    task.Host = host;
                    _db.BuildTask.Attach(task);
                    _db.Entry(task).State = EntityState.Modified;
                    _db.SaveChanges();
                    return (false, "");
            }
        }

        public BuildTask TryLockATask() {
            var task = _db.BuildTask.FirstOrDefault(x => x.State == 0);
            if (task == null) {
                return null;
            }

            // 更新状态
            task.State = 1;
            task.UpdateAt = DateTime.Now;

            _db.BuildTask.Attach(task);
            _db.Entry(task).State = EntityState.Modified;
            _db.SaveChanges();
            return task;
        }

        public BuildTask GetAWorkingTask() {
            return _db.BuildTask.FirstOrDefault(x => x.State == 1);
        }

        public void FinishWork(int id, int state) {
            var task = _db.BuildTask.FirstOrDefault(x => x.Id == id);
            if (task == null) {
                return;
            }

            task.State = state;
            task.UpdateAt = DateTime.Now;
            _db.BuildTask.Attach(task);
            _db.Entry(task).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void ResetWorking() {
            _db.BuildTask.Where(x => x.State == 1).Update(x => new BuildTask {State = 0});
        }

        public BuildStateViewModel GetBuildState() {
            var tasks = _db.BuildTask.ToList();
            return new BuildStateViewModel {
                Finished = tasks.Count(x => x.State == 2),
                New = tasks.Count(x => x.State == 0),
                Total = tasks.Count,
                Working = tasks.Count(x => x.State == 1),
                WorkingItems = tasks.Where(x => x.State == 1).ToList()
            };
        }
    }
}