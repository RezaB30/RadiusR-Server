using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Implementations.IntermediateContainers
{
    class InterrimAccountingRecord
    {
        public long ID { get; set; }

        public string UniqueID { get; set; }

        public long UploadBytes { get; set; }

        public long DownloadBytes { get; set; }

        public DateTime? StopTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        public long SessionTime { get; set; }

        #region container only
        public short TerminateCause { get; set; }
        #endregion
    }
}
