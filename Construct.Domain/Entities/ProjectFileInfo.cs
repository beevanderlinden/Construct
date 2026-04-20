
namespace Construct.Domain.Entities
{

    public class ProjectFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public ProjectInfoEntity Info { get; set; } = default!;

        public string DriveId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;


        public string Base64 { get; set; } = "";
        public DateTime LastModified { get; set; }
        public DateTimeOffset? LastModifiedDateTime { get; set; }
        public long? Size { get; set; } = 0;
        public double SizeInKB { get; set; }

        public string SizeUserFriendly
        {
            get
            {
                if (Size == null) return "0";

                if (Size < 1000000)
                {
                    return $"{Math.Ceiling((Size.Value / 1000.0)):0} kB";
                }
                else
                {
                    return $"{Math.Ceiling((Size.Value / 1000000.0)):0} MB";
                }
            }
        }

    }

    /// <summary>
    /// Wrapper voor ProjectInfoEntity, om de JSON-structuur te vereenvoudigen.
    /// </summary>
    public class ProjectFileRoot
    {
        public ProjectInfoEntity ProjectInfo { get; set; } = default!;
    }

}