using NDesk.Options;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BackupLinuxFiles
{
    class Program
    {
        private static string Server { get; set; }
        private static string Username { get; set; }
        private static string Password { get; set; }
        private static string SourcePath { get; set; }
        private static string DestinationPath { get; set; }


        static void Main(string[] args)
        {
            if (!ParseArguments(args))
            {
                return;
            }
            
            bool isReady = true;
            if (string.IsNullOrWhiteSpace(Server))
            {
                isReady = false;
                Console.WriteLine("Did not specify a server");
            }
            if (string.IsNullOrWhiteSpace(Username))
            {
                isReady = false;
                Console.WriteLine("Did not specify user name");
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                isReady = false;
                Console.WriteLine("Did not specify password");
            }
            if (string.IsNullOrWhiteSpace(SourcePath))
            {
                isReady = false;
                Console.WriteLine("Did not specify source path");
            }
            if (string.IsNullOrWhiteSpace(DestinationPath))
            {
                isReady = false;
                Console.WriteLine("Did not specify destination path");
            }

            if (isReady)
            {
                var client = Connect();
                if (client != null)
                {
                    DownloadFiles(client, SourcePath);
                }
            }
        }

        private static bool ParseArguments(string[] args)
        {
            bool showHelp = false;
            var optionSet = new OptionSet()
            {
                { "s|server=", "Linux server (IP or domain name)", p => Server = p },
                { "u|username|user=", "Username", p => Username = p },
                { "p|password=", "Password", p => Password = p },
                { "sp|Source=", "Source path", p => SourcePath = p },
                { "dp|Destination=", "Destination path", p => DestinationPath = p },
                { "h|help", "Show this message", p => showHelp = p != null }
            };
            List<string> extra;

            try
            {
                extra = optionSet.Parse(args);
                if (showHelp)
                {
                    optionSet.WriteOptionDescriptions(Console.Out);
                    return false;
                }
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try type --help for more information");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return true;
        }

        private static SftpClient Connect()
        {
            try
            {
                var connectionInfo = new ConnectionInfo(Server, Username, new PasswordAuthenticationMethod(Username, Password));
                var client = new SftpClient(connectionInfo);
                client.Connect();
                return client;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        private static void DownloadFiles(SftpClient client, string sourcePath)
        {
            try
            {
                foreach (var file in client.ListDirectory(sourcePath))
                {
                    if (!file.Name.EndsWith("."))
                    {
                        if (file.IsDirectory)
                        {
                            DownloadFiles(client, file.FullName);
                        }
                        else
                        {
                            using (var stream = new MemoryStream())
                            {
                                client.DownloadFile(file.FullName, stream);
                                SaveFile(stream, file.FullName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void SaveFile(MemoryStream stream, string destinationPath)
        {
            try
            {
                destinationPath = DestinationPath + destinationPath;
                if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                }

                var filestream = File.Create(destinationPath);
                stream.WriteTo(filestream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}