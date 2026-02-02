namespace Construct.WebUI.Server.Adapters
{
    using Construct.Application.GraphModels;
    using Microsoft.AspNetCore.Components;
    using Microsoft.FluentUI.AspNetCore.Components;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

    public class DriveItemTreeViewAdapter : ITreeViewItem
    {
        private readonly DriveItemNode _node;
        private readonly Func<DriveItemNode, Task>? _loadChildrenAsync;


        public DriveItemTreeViewAdapter(DriveItemNode node, Func<DriveItemNode, Task>? loadChildrenAsync)
        {
            _node = node;
            _loadChildrenAsync = loadChildrenAsync;
        }

        public string Id => _node.Item?.Id ?? Guid.NewGuid().ToString();

        public string Title => _node.Item?.Name ?? "(zonder naam)";

        public string? Icon => _node.Item?.Folder != null ? "folder" : "document";

        private Icon _iconCollapsed = new Icons.Regular.Size20.Folder();
        private Icon _iconExpanded = new Icons.Regular.Size20.FolderOpen();
        private Icon _iconItem = new Icons.Color.Size20.Document();

        public bool HasChildren => _node.Children.Any();

        public IEnumerable<ITreeViewItem> GetChildren() =>
            _node.Children.Select(child => new DriveItemTreeViewAdapter(child, _loadChildrenAsync));

        public DriveItemNode SourceNode => _node;

        // 👇 Implementatie van ITreeViewItem interface

        string ITreeViewItem.Id
        {
            get => Id;
            set { /* geen set nodig in jouw geval */ }
        }

        public string Text
        {
            get => Title;
            set { /* niet nodig: DriveItemNode bepaalt de naam */ }
        }

        public IEnumerable<ITreeViewItem>? Items
        {
            get => HasChildren ? GetChildren() : null;
            set { /* niet nodig */ }
        }

        public Icon? IconCollapsed
        {
            get => Icon == "folder" ? _iconCollapsed : _iconItem;
            set { /* niet nodig */ }
        }

        public Icon? IconExpanded
        {
            get => Icon == "folder" ? _iconExpanded : _iconItem;
            set { /* niet nodig */ }
        }

        public bool Disabled
        {
            get => false;
            set { /* niet nodig */ }
        }

        public bool Expanded
        {
            get => _node.IsExpanded;
            set => _node.IsExpanded = value;
        }

        public Func<TreeViewItemExpandedEventArgs, Task>? OnExpandedAsync =>
             async args =>
             {
                 if (args.Expanded && !_node.IsLoaded && _loadChildrenAsync is not null)
                 {
                     await _loadChildrenAsync(_node);
                     _node.IsLoaded = true;
                 }
             };

        // Explicit interface implementatie
        Func<TreeViewItemExpandedEventArgs, Task>? ITreeViewItem.OnExpandedAsync
        {
            get => OnExpandedAsync;
            set { /* setter genegeerd */ }
        }



        [Obsolete("Gebruik binnen de treeview eigen code, omdat anders null referentie error")]
        public RenderFragment? ChildContent => builder =>
        {

            var sequence = 0;
#pragma warning disable ASP0006 // Component parameter should be assigned using an integer literal

            builder.OpenComponent<FluentMenuButton>(sequence++);
            builder.AddAttribute(sequence++, "Icon", new Icons.Regular.Size20.MoreHorizontal());
            builder.AddAttribute(sequence++, "AriaLabel", "Bestandsmenu");
            builder.AddAttribute(sequence++, "ChildContent", (RenderFragment)(menuBuilder =>
            {
                var menuSeq = 0;

                menuBuilder.OpenComponent<FluentMenuItem>(menuSeq++);
                menuBuilder.AddAttribute(menuSeq++, "Text", "Openen");
                menuBuilder.AddAttribute(menuSeq++, "OnClick", EventCallback.Factory.Create(this, () => OnOpenClicked()));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<FluentMenuItem>(menuSeq++);
                menuBuilder.AddAttribute(menuSeq++, "Text", "Hernoem");
                menuBuilder.AddAttribute(menuSeq++, "OnClick", EventCallback.Factory.Create(this, () => OnRenameClicked()));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<FluentMenuItem>(menuSeq++);
                menuBuilder.AddAttribute(menuSeq++, "Text", "Verwijderen");
                menuBuilder.AddAttribute(menuSeq++, "OnClick", EventCallback.Factory.Create(this, () => OnDeleteClicked()));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<FluentMenuItem>(menuSeq++);
                menuBuilder.AddAttribute(menuSeq++, "Text", "Kopiëren");
                menuBuilder.AddAttribute(menuSeq++, "OnClick", EventCallback.Factory.Create(this, () => OnCopyClicked()));
                menuBuilder.CloseComponent();
            }));
            builder.CloseComponent();
#pragma warning restore ASP0006
        };

        private void OnOpenClicked()
        {
            Console.WriteLine($"Openen: {_node.Item?.Name}");
            // TODO: Open-bestand logica
        }

        private void OnRenameClicked()
        {
            Console.WriteLine($"Hernoem: {_node.Item?.Name}");
            // TODO: Rename UI tonen
        }

        private void OnDeleteClicked()
        {
            Console.WriteLine($"Verwijderen: {_node.Item?.Name}");
            // TODO: Verwijder logica
        }

        private void OnCopyClicked()
        {
            Console.WriteLine($"Kopieer: {_node.Item?.Name}");
            // TODO: Kopieer logica
        }


    }
}
