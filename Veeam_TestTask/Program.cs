using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace Veeam_TestTask
{

    static class Program
    {
        //Any change to the file locations should be done in the Constats.cs file
        static string sourcePath = Constats.SouceFilePath;
        static string replicaPath = Constats.ReplicaFilePath;
        static string logFilePath = Constats.LogSyncFilePath;
        static int syncInterval = Constats.syncIntervalValue;

        static void Main(string[] args)
        {
            string basePath = Environment.CurrentDirectory;

            // Start synchronization
            Timer timer = new Timer(SyncFolders, null, 0, syncInterval * 1000);

            // Keep the application running
            Console.WriteLine("Press [Enter] to exit...");
            Console.ReadLine();
        }

         /// <summary>
         /// 
         /// </summary>
         /// <param name="state"></param>
        static void SyncFolders(object state)
        {
            try
            {
                if (!Directory.Exists(sourcePath))
                {
                    Log($"Source directory does not exist: {sourcePath}");
                    return;
                }

                if (!Directory.Exists(replicaPath))
                {
                    Directory.CreateDirectory(replicaPath);
                    Log($"Created replica directory: {replicaPath}");
                }

                // Synchronize files
                SyncDirectory(sourcePath, replicaPath);

                // Delete extra files and directories in replica
                CleanUpReplica(sourcePath, replicaPath);
            }
            catch (Exception ex)
            {
                Log($"Error during synchronization: {ex.Message}");
            }
        }

        /// <summary>
        /// Methiod resposable for syncronize the files betwen the two folders Source and Replica
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="replicaDir"></param>
        static void SyncDirectory(string sourceDir, string replicaDir)
        {
            // Copy all files
            foreach (string sourceFilePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string replicaFilePath = Path.Combine(replicaDir, fileName);

                if (!File.Exists(replicaFilePath) || !FilesAreEqual(sourceFilePath, replicaFilePath))
                {
                    File.Copy(sourceFilePath, replicaFilePath, true);
                    Log($"Copied file: {sourceFilePath} to {replicaFilePath}");
                }
            }

            // Recursively synchronize directories
            foreach (string sourceSubDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(sourceSubDir);
                string replicaSubDir = Path.Combine(replicaDir, dirName);

                if (!Directory.Exists(replicaSubDir))
                {
                    Directory.CreateDirectory(replicaSubDir);
                    Log($"Created directory: {replicaSubDir}");
                }

                SyncDirectory(sourceSubDir, replicaSubDir);
            }
        }

        /// <summary>
        /// Deletes the files that are deleted from the Source Folder
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="replicaDir"></param>
        static void CleanUpReplica(string sourceDir, string replicaDir)
        {
            // Delete files in replica that are not in source
            foreach (string replicaFilePath in Directory.GetFiles(replicaDir))
            {
                string fileName = Path.GetFileName(replicaFilePath);
                string sourceFilePath = Path.Combine(sourceDir, fileName);

                if (!File.Exists(sourceFilePath))
                {
                    File.Delete(replicaFilePath);
                    Log($"Deleted file: {replicaFilePath}");
                }
            }

            // Recursively delete directories
            foreach (string replicaSubDir in Directory.GetDirectories(replicaDir))
            {
                string dirName = Path.GetFileName(replicaSubDir);
                string sourceSubDir = Path.Combine(sourceDir, dirName);

                if (!Directory.Exists(sourceSubDir))
                {
                    Directory.Delete(replicaSubDir, true);
                    Log($"Deleted directory: {replicaSubDir}");
                }
                else
                {
                    CleanUpReplica(sourceSubDir, replicaSubDir);
                }
            }
        }

        /// <summary>
        /// This method compare if the files are equal and if they have the same size
        /// </summary>
        /// <param name="filePath1"></param>
        /// <param name="filePath2"></param>
        /// <returns></returns>
        static bool FilesAreEqual(string filePath1, string filePath2)
        {
            using (var hashAlgorithm = SHA256.Create())
            {
                byte[] file1Hash = hashAlgorithm.ComputeHash(File.ReadAllBytes(filePath1));
                byte[] file2Hash = hashAlgorithm.ComputeHash(File.ReadAllBytes(filePath2));
                return file1Hash.SequenceEqual(file2Hash);
            }
        }


        static void Log(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }
}
