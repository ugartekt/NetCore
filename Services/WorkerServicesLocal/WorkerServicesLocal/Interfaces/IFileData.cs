using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerServicesLocal.Models;

namespace WorkerServicesLocal.Interfaces
{
    public interface IFileData
    {
        public void EnsureDocumentDirectoryStructure();
        public void WriteFileLog(string directory, string fileName, string text);

    }
}
