using System;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
    [Serializable]
    public class ejpDocumentImageBorder
    {
        public double MarginLeft { get; set; }
        public double MarginTop { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Guid ParentStudyId { get; set; }
        public Guid ParentDocumentId { get; set; }
        public ejpSolidColor Color { get; set; }
        public string PathData { get; set; }
        public int LineType { get; set; }
        public Guid Id { get; set; }

        public ejpDocumentImageBorder()
        {
        }
    }
}
