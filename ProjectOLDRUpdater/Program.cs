using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectOLDRUpdater
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length < 2)
            {
                MessageBox.Show("Usage: ProjectOLDRUpdater.exe <download_url> <target_exe_path>", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string downloadUrl = args[0];
            string targetExePath = args[1];

            Task.Run(async () => await PerformUpdateAsync(downloadUrl, targetExePath)).GetAwaiter().GetResult();
        }

        private static async Task PerformUpdateAsync(string downloadUrl, string targetExePath)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "ProjectOLDR_downloaded.exe");

            try
            {
                Thread.Sleep(1500);

                string targetProcessName = Path.GetFileNameWithoutExtension(targetExePath);
                Process[] runningProcesses = Process.GetProcessesByName(targetProcessName);
                foreach (var p in runningProcesses)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                    catch
                    {
                    }
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "ProjectOLDRUpdater");
                    byte[] data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempFile, data);
                }

                int attempts = 0;
                while (attempts < 10)
                {
                    try
                    {
                        if (File.Exists(targetExePath))
                        {
                            File.Delete(targetExePath);
                        }
                        File.Move(tempFile, targetExePath);
                        break;
                    }
                    catch (IOException)
                    {
                        attempts++;
                        Thread.Sleep(500);
                    }
                }

                if (File.Exists(targetExePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Failed to replace application binary.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
                Environment.Exit(0);
            }
        }
    }
}