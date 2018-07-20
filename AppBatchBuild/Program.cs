using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace AppBatchBuild {
    class Program {
        static void Main(string[] args) {
            SendBuildRequest();
        }

        private static void AutoBuild() {
            if (!File.Exists("agents.txt")) {
                Console.WriteLine(" !!!!! File NOT exists !!!!! ");
                return;
            }

            var lines = File.ReadAllLines("agents.txt");
            var comparer = new AgentModelComparer();
            var agents = lines.Select(x => {
                if (string.IsNullOrWhiteSpace(x)) return null;
                var fields = x.Split(",");
                return new AgentModel {
                    Name = fields[0]?.Trim() ?? "",
                    Host = fields[1]?.Trim() ?? "",
                    ProxyUrl = fields[2]?.Trim() ?? ""
                };
            }).Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name)).Distinct(comparer).ToList();

            var constantsTpl = File.ReadAllText("Constants.kt");
            var buildTpl = File.ReadAllText("build.gradle");

            foreach (var agent in agents) {
                var url = new Uri(agent.ProxyUrl);
                var host = agent.Host;
                host = !string.IsNullOrWhiteSpace(agent.Host) ? $"https://{host}" : null;
                var constants = constantsTpl.Replace("{siteUrl}", host ?? agent.ProxyUrl).Replace("{siteBaseUrl}", host ?? $"https://{url.Host}");
                File.WriteAllText(@"android\app\src\app\java\Constants.kt", constants);

                var build = buildTpl.Replace("{agentName}", agent.Name);
                File.WriteAllText(@"android\app\build.gradle", build);

                var appRoot = Path.Combine(Directory.GetCurrentDirectory(), "android");

                Console.WriteLine($"build for {agent.Name} ===================== begin ");
                var cmdsi = new ProcessStartInfo("cmd.exe") {
                    Arguments = @"/C gradle assembleRelease",
                    WorkingDirectory = appRoot,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var cmd = Process.Start(cmdsi)) {
                    cmd?.WaitForExit();
                }
                Console.WriteLine($"build for {agent.Name} ===================== done ");
            }

            Console.WriteLine(" !!!!! build all done !!!!! ");
        }

        private static void SendBuildRequest() {
            var root = Directory.GetCurrentDirectory();
            if (!File.Exists("android.txt")) {
                Console.WriteLine(" !!!!! File NOT exists !!!!! ");
                return;
            }

            var lines = File.ReadAllLines("android.txt");
            var comparer = new AgentModelComparer();
            var agents = lines.Select(x => {
                if (string.IsNullOrWhiteSpace(x)) return null;
                var fields = x.Split("\t");
                return new AgentModel {
                    Name = fields[0]?.Trim() ?? "",
                    Host = fields[1]?.Trim() ?? ""
//                    ProxyUrl = fields[2]?.Trim() ?? ""
                };
            }).Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name)).Distinct(comparer).ToList();

            var client = new WebClient();
            agents.ForEach(item => {
                var host = item.Host;
                if (!string.IsNullOrWhiteSpace(host)) {
                    host = $"/{host}";
                }
                var state = client.DownloadString($"https://dl.bibcdn.com/app/check/{item.Name}");
                if (!string.IsNullOrWhiteSpace(state) && state.StartsWith("app 版本号：")) {
                    Console.WriteLine($" app exists: {item.Name} ============================");
                } else {
                    var text = client.DownloadString($"https://appbuilder.bibcdn.com/app/build/{item.Name}{host}");
                    Console.WriteLine($" send build for {item.Name}: {text} ============================");
                }
            });
        }
    }
}