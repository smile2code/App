using System;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder {
    public class BuildTaskDbContext : DbContext {
        public BuildTaskDbContext(DbContextOptions<BuildTaskDbContext> options)
            : base(options) { }

        public DbSet<BuildTask> BuildTask { get; set; }
    }

    public class BuildTask {
        public int Id { get; set; }
        public string AgentName { get; set; }
        public string Host { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        /// <summary>
        /// 状态：0-待处理，1-处理中，2-处理完成
        /// </summary>
        public int State { get; set; }
    }
}