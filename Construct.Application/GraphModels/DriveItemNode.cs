using Microsoft.Graph.Models;

namespace Construct.Application.GraphModels
{
    public class DriveItemNode
    {
        public DriveItem Item { get; set; } = default!;
        public List<DriveItemNode> Children { get; set; } = [];
        public string? Path { get; set; } = "";
        public string? WebUrl { get; set; } = "";
        public bool IsExpanded { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsFolder => Item.Folder != null;
    }
}
