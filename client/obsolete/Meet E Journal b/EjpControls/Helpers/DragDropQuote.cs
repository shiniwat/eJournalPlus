using System.Windows.Media;

namespace SiliconStudio.Meet.EjpControls.Helpers
{
    public class DragDropQuote
    {
        public Color Color { get; set; }
        public string UnicodeString { get; set; }
        public string CommentString { get; set; }

        public XpsDocumentReference Reference { get; set; }
        public DragDropQuote() { }
    }
}
