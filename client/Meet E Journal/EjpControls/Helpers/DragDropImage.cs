using System.Windows.Media;

namespace SiliconStudio.Meet.EjpControls.Helpers
{
    class DragDropImage
    {
        public string TargetPathData { get; set; }
        public Color Color { get; set; }
        public string SourceUri { get; set; }
        public XpsDocumentReference Reference { get; set; }
        public ImageBrush IBrush { get; set; }
        public DragDropImage() { }
    }
}
