using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography;
using System.IO;

namespace MalLoader
{
    class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 5000;
            return w;
        }
    }

    class Program
    {
        static readonly string downloadUrl = "https://urlhaus.abuse.ch/downloads/text_recent/";
        static readonly MyWebClient web = new MyWebClient();
        static readonly MD5 md5 = MD5.Create();
        static bool noDownload = false;
        static int malwareCount = 0;
        static int failCount = 0;
        static List<string> malwareUrls = new List<string>();
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-nodownload")
                    noDownload = true;
            }

            Console.Title = "MalLoader — www.Ill5.com (c) 2022";
            Console.WriteLine("MalLoader — www.Ill5.com (c) 2022\n");

            if (!noDownload)
            {
                Console.WriteLine("WARNING: This executable will download (but not execute) large amounts of malware.\nPlease press any key to continue and acknowledge the risks.\n");
                Console.ReadKey();
            }

            Console.WriteLine("Downloading malware list...\n");

            string unsplitMalwareUrls = string.Empty;

            try
            {
                unsplitMalwareUrls = web.DownloadString(downloadUrl);
            }
            catch
            {
                Console.WriteLine("Failed to download malware list!\n\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            List<string> unfilteredMalwareUrls = unsplitMalwareUrls.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string url in unfilteredMalwareUrls)
            {
                if (url.EndsWith(".exe"))
                    malwareUrls.Add(url);
            }

            Console.WriteLine($"Filtered {unfilteredMalwareUrls.Count} samples to just {malwareUrls.Count} Windows executables!\n");

            if (noDownload)
            {
                Console.WriteLine("-nodownload specified, writing filtered list to file instead of downloading samples...");

                if (File.Exists("malwareUrls.txt"))
                    File.Delete("malwareUrls.txt");

                File.WriteAllLines("malwareUrls.txt", malwareUrls.ToArray());
            }
            else
            {
                Console.WriteLine($"Starting download of {malwareUrls.Count} samples...\n");

                if (!Directory.Exists("MALWARE"))
                    Directory.CreateDirectory("MALWARE");

                DateTime nextUpdate = DateTime.Now + TimeSpan.FromSeconds(5);

                foreach (string url in malwareUrls)
                {
                    if (DateTime.Now >= nextUpdate)
                    {
                        Console.WriteLine($" - {malwareCount} samples downloaded, {failCount} downloads failed");
                        nextUpdate = DateTime.Now + TimeSpan.FromSeconds(5);
                    }

                    try
                    {
                        byte[] downloadBuffer = web.DownloadData(url);

                        if (downloadBuffer.Length < 2 || downloadBuffer[0] != 'M' || downloadBuffer[1] != 'Z')
                        {
                            failCount++;
                            continue;
                        }

                        byte[] hashBytes = md5.ComputeHash(downloadBuffer);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < hashBytes.Length; i++)
                        {
                            sb.Append(hashBytes[i].ToString("x2"));
                        }

                        string sampleName = sb.ToString();

                        if (!File.Exists($"MALWARE\\{sampleName}.exe"))
                        {
                            File.WriteAllBytes($"MALWARE\\{sampleName}.exe", downloadBuffer);

                            malwareCount++;
                        }
                    }
                    catch
                    {
                        failCount++;
                    }
                }
            }

            Console.WriteLine($"\nMalLoader finished!\nFinal statistics: {malwareCount} samples downloaded, {failCount} downloads failed\n\nPress any key to exit...");
            Console.ReadKey();
            return;
        }
    }
}
