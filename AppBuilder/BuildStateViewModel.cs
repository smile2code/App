using System.Collections.Generic;

namespace AppBuilder {
    public class BuildStateViewModel {
        public int Total { get; set; }
        public int Finished { get; set; }
        public int Working { get; set; }
        public int New { get; set; }
        public List<BuildTask> WorkingItems { get; set; }
    }
}