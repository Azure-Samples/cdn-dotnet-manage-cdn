// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Headers;
using Renci.SshNet;

namespace Azure.ResourceManager.Samples.Common
{
    public static class Utilities
    {
        public static bool IsRunningMocked { get; set; }
        public static Action<string> LoggerMethod { get; set; }
        public static Func<string> PauseMethod { get; set; }
        public static string ProjectPath { get; set; }
        private static Random _random => new Random();

        static Utilities()
        {
            LoggerMethod = Console.WriteLine;
            PauseMethod = Console.ReadLine;
            ProjectPath = ".";
        }

        public static void Log(string message)
        {
            LoggerMethod.Invoke(message);
        }

        public static void Log(object obj)
        {
            if (obj != null)
            {
                LoggerMethod.Invoke(obj.ToString());
            }
            else
            {
                LoggerMethod.Invoke("(null)");
            }
        }

        public static void Log()
        {
            Utilities.Log("");
        }

        public static string ReadLine()
        {
            return PauseMethod.Invoke();
        }

        public static string CreateRandomName(string namePrefix)
        {
            return $"{namePrefix}{_random.Next(9999)}";
        }

        public static string CreatePassword()
        {
            return "azure12345QWE!";
        }

        public static string CreateUsername()
        {
            return "tirekicker";
        }

        private static string FormatDictionary(IReadOnlyDictionary<string, string> dictionary)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }

            var outputString = new StringBuilder();

            foreach (var entity in dictionary)
            {
                outputString.AppendLine($"{entity.Key}: {entity.Value}");
            }

            return outputString.ToString();
        }

        private static string FormatCollection(IEnumerable<string> collection)
        {
            return string.Join(", ", collection);
        }

        /**
         * Retrieve the secondary service principal client ID.
         * @param envSecondaryServicePrincipal an Azure Container Registry
         * @return a service principal client ID
         */
        public static string GetSecondaryServicePrincipalClientID(string envSecondaryServicePrincipal)
        {
            string clientId = "";
            File.ReadAllLines(envSecondaryServicePrincipal).All(line =>
            {
                var keyVal = line.Trim().Split(new char[] { '=' }, 2);
                if (keyVal.Length < 2)
                    return true; // Ignore lines that don't look like $$$=$$$
                if (keyVal[0].Equals("client"))
                    clientId = keyVal[1];
                return true;
            });

            return clientId;
        }

        /**
         * Retrieve the secondary service principal secret.
         * @param envSecondaryServicePrincipal an Azure Container Registry
         * @return a service principal secret
         */
        public static string GetSecondaryServicePrincipalSecret(string envSecondaryServicePrincipal)
        {
            string secret = "";
            File.ReadAllLines(envSecondaryServicePrincipal).All(line =>
            {
                var keyVal = line.Trim().Split(new char[] { '=' }, 2);
                if (keyVal.Length < 2)
                    return true; // Ignore lines that don't look like $$$=$$$
                if (keyVal[0].Equals("key"))
                    secret = keyVal[1];
                return true;
            });

            return secret;
        }

        public static void CreateCertificate(string domainName, string pfxPath, string password)
        {
            if (!IsRunningMocked)
            {
                string args = string.Format(
                    @".\createCert.ps1 -pfxFileName {0} -pfxPassword ""{1}"" -domainName ""{2}""",
                    pfxPath,
                    password,
                    domainName);
                ProcessStartInfo info = new ProcessStartInfo("powershell", args);
                string assetPath = Path.Combine(ProjectPath, "Asset");
                info.WorkingDirectory = assetPath;
                Process process = Process.Start(info);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    // call "Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass" in powershell if you fail here

                    Utilities.Log("powershell createCert.ps1 script failed");
                }
            }
            else
            {
                //File.Copy(
                //    Path.Combine(Utilities.ProjectPath, "Asset", "SampleTestCertificate.pfx"),
                //    Path.Combine(Utilities.ProjectPath, "Asset", pfxPath),
                //    overwrite: true);
            }
        }

        public static void CreateCertificate(string domainName, string pfxName, string cerName, string password)
        {
            if (!IsRunningMocked)
            {
                string args = string.Format(
                    @".\createCert1.ps1 -pfxFileName {0} -cerFileName {1} -pfxPassword ""{2}"" -domainName ""{3}""",
                    pfxName,
                    cerName,
                    password,
                    domainName);
                ProcessStartInfo info = new ProcessStartInfo("powershell", args);
                string assetPath = Path.Combine(ProjectPath, "Asset");
                info.WorkingDirectory = assetPath;
                Process.Start(info).WaitForExit();
            }
            else
            {
                //File.Copy(
                //    Path.Combine(Utilities.ProjectPath, "Asset", "SampleTestCertificate.pfx"),
                //    Path.Combine(Utilities.ProjectPath, "Asset", pfxName),
                //    overwrite: true);
            }
        }

        public static string CheckAddress(string url, IDictionary<string, string> headers = null)
        {
            if (!IsRunningMocked)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(300);
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }
                        return $"Ping: {url}: {client.GetAsync(url).Result.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }

            return "[Running in PlaybackMode]";
        }

        public static string PostAddress(string url, string body, IDictionary<string, string> headers = null)
        {
            if (!IsRunningMocked)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }
                        return client.PostAsync(url, new StringContent(body)).Result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }

            return "[Running in PlaybackMode]";
        }

        public static void DeprovisionAgentInLinuxVM(string host, int port, string userName, string password)
        {
            Utilities.Log("is mocked:" + IsRunningMocked);
            if (!IsRunningMocked)
            {
                Console.WriteLine("Trying to de-provision: " + host);
                Console.WriteLine("ssh connection status: " + TrySsh(
                    host,
                    port,
                    userName,
                    password,
                    "sudo waagent -deprovision+user --force"));
            }
        }

        public static string GetArmTemplate(string templateFileName)
        {
            var adminUsername = "tirekicker";
            var adminPassword = CreatePassword();
            var hostingPlanName = CreateRandomName("hpRSAT");
            var webAppName = CreateRandomName("wnRSAT");
            var armTemplateString = File.ReadAllText(Path.Combine(Utilities.ProjectPath, "Asset", templateFileName));

            var parsedTemplate = JObject.Parse(armTemplateString);

            if (String.Equals("ArmTemplate.json", templateFileName, StringComparison.OrdinalIgnoreCase))
            {
                parsedTemplate.SelectToken("parameters.hostingPlanName")["defaultValue"] = hostingPlanName;
                parsedTemplate.SelectToken("parameters.webSiteName")["defaultValue"] = webAppName;
                parsedTemplate.SelectToken("parameters.skuName")["defaultValue"] = "B1";
                parsedTemplate.SelectToken("parameters.skuCapacity")["defaultValue"] = 1;
            }
            else if (String.Equals("ArmTemplateVM.json", templateFileName, StringComparison.OrdinalIgnoreCase))
            {
                parsedTemplate.SelectToken("parameters.adminUsername")["defaultValue"] = adminUsername;
                parsedTemplate.SelectToken("parameters.adminPassword")["defaultValue"] = adminPassword;
            }
            return parsedTemplate.ToString();
        }

        public static string GetCertificatePath(string certificateName)
        {
            return Path.Combine(Utilities.ProjectPath, "Asset", certificateName);
        }

        private static string TrySsh(
            string host,
            int port,
            string userName,
            string password,
            string commandToExecute)
        {
            string commandOutput = null;
            var backoffTime = 30 * 1000;
            var retryCount = 3;

            while (retryCount > 0)
            {
                using (var sshClient = new SshClient(host, port, userName, password))
                {
                    try
                    {
                        sshClient.Connect();
                        if (commandToExecute != null)
                        {
                            using (var command = sshClient.CreateCommand(commandToExecute))
                            {
                                commandOutput = command.Execute();
                            }
                        }
                        break;
                    }
                    catch (Exception exception)
                    {
                        retryCount--;
                        if (retryCount == 0)
                        {
                            throw exception;
                        }
                    }
                    finally
                    {
                        try
                        {
                            sshClient.Disconnect();
                        }
                        catch { }
                    }
                }
            }

            return commandOutput;
        }
    }
}
