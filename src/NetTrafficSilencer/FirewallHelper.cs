using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace NetTrafficSilencer
{
    public static class FirewallHelper
    {
        private static ConcurrentDictionary<string, bool> firewallRulesCache;

        // Call this method once at the beginning to initialize firewall rules from netsh output
        public static void LoadFirewallRules()
        {
            firewallRulesCache = new ConcurrentDictionary<string, bool>();

            string command = "netsh advfirewall firewall show rule name=all";
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    ParseNetshOutput(output);
                }
            }
        }


        // Parse the netsh output and populate the firewallRulesCache dictionary
        private static void ParseNetshOutput(string netshOutput)
        {
            using (var reader = new StringReader(netshOutput))
            {
                string line;
                string currentRuleName = string.Empty;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Rule Name:"))
                    {
                        currentRuleName = line.Substring("Rule Name:".Length).Trim();

                        // Check if the rule name follows our custom naming format: "Block ExecutableName - ExecutablePath"
                        if (currentRuleName.StartsWith("Block "))
                        {
                            // Extract the executable path from the rule name
                            int pathStartIndex = currentRuleName.IndexOf("- ") + 2;
                            if (pathStartIndex > 0 && pathStartIndex < currentRuleName.Length)
                            {
                                string executablePath = currentRuleName.Substring(pathStartIndex).Trim();

                                // Store the executable path in the cache (use lowercase to ensure case-insensitivity)
                                if (!string.IsNullOrEmpty(executablePath))
                                {
                                    firewallRulesCache[executablePath.ToLower()] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Generates a unique rule name for the firewall rule based on the executable path
        public static string GenerateRuleName(string executablePath)
        {
            return $"Block {System.IO.Path.GetFileName(executablePath)} - {executablePath}";
        }

        // Checks if a firewall rule for the given executable path exists using the preloaded cache
        public static bool RuleExists(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                return false;

            return firewallRulesCache.ContainsKey(executablePath.ToLower());
        }

        // Adds a firewall rule with a custom name to block outgoing traffic for the given executable
        public static bool AddFirewallRule(string executablePath)
        {
            Debug.WriteLine($"Adding rule for {executablePath}");
            string ruleName = GenerateRuleName(executablePath);
            string command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir=out program=\"{executablePath}\" action=block enable=yes";
            var res = ExecuteCommand(command);

            Debug.WriteLine($"Adding rule for {executablePath}, RES: {res}");
            return res;
        }

        // Removes a firewall rule with a custom name
        public static bool RemoveFirewallRule(string executablePath)
        {
            Debug.WriteLine($"Removing rule for {executablePath}");
            string ruleName = GenerateRuleName(executablePath);
            string command = $"netsh advfirewall firewall delete rule name=\"{ruleName}\"";
            var res = ExecuteCommand(command);

            Debug.WriteLine($"Removing rule for {executablePath}, RES: {res}");
            return res;
        }

        // Executes a given command and returns true if successful
        private static bool ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas" // Ensure elevated privileges
            };

            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }

            return false;
        }
    }
}
