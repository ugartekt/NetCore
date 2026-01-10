using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLottoActivo.Models;

namespace WebLottoActivo.Interfaces
{
    public interface IFileData
    {
        public void EnsureDocumentDirectoryStructure();
        public void WriteFileLog(string directory, string fileName, string text);

    }
}
