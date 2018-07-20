using System.Collections.Generic;

namespace AppBatchBuild {
    public class AgentModel {
        public string Name { get; set; }
        public string Host { get; set; }
        public string ProxyUrl { get; set; }
    }

    public class AgentModelComparer : IEqualityComparer<AgentModel> {
        public bool Equals(AgentModel x, AgentModel y) {
            return x.Name == y.Name;
        }

        public int GetHashCode(AgentModel obj) {
            return obj.GetHashCode();
        }
    }
}