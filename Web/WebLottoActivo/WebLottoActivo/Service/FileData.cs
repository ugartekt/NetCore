using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLottoActivo.Interfaces;

namespace WebLottoActivo.Service
{
    public class FileData : IFileData
    {
        public void EnsureDocumentDirectoryStructure()
        {
            string documentosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string basePath = Path.Combine(documentosPath, "BCV");

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
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                                                , "BCV"
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
