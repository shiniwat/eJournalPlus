using System;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
    [Serializable]
    public class ejpDocumentReference
    {
        public Guid TargetLineId { get; set; }
        public int PageNumber { get; set; }
        public Guid DocumentId { get; set; }
        public string DocumentTitle { get; set; }
        public Guid DocumentParentStudyId { get; set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public string Content { get; set; }
        public ejpDocumentReference()
        {

        }
    }
}
