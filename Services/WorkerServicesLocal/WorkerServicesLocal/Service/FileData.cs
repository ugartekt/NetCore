using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerServicesLocal.Interfaces;

namespace WorkerServicesLocal.Service
{
    public class FileData : IFileData
    {
        public void EnsureDocumentDirectoryStructure()
        {
            //string documentosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string documentosPath = @"C:\";
            string basePath = Path.Combine(documentosPath, "WorkerLocal");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            string[] subfolders = { "INFO", "ERROR" };
            foreach (var folder in subfolders)
            {
                string fullPath = Path.Combine(basePath, folder);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
        }

        public void WriteFileLog(string directory, string fileName, string text)
        {
            try
            {
                string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string documentosPath = @"C:\";
                string folderPath = Path.Combine(documentosPath
                                                , "WorkerLocal"
                                                , directory
                                                , $"{fileName}_{date}.txt");

                File.WriteAllText(folderPath, text);
            }
            catch (Exception ex)
            {

            }
        }

    }
}
