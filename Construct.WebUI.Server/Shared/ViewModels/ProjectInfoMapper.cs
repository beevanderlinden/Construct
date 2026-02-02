using Eurocode.Grondslagen;
using Riok.Mapperly.Abstractions;

namespace Construct.WebUI.Server.Shared.ViewModels
{
    [Mapper]
    public static partial class ProjectInfoMapper
    {
        //public static partial ProjectInfoEntity MapProjectInfoToDto(this ProjectInfoViewModel projectInfo);
        //public static partial ProjectInfoViewModel MapProjectInfoToViewModel(this ProjectInfoEntity projectInfo);
    }

    public class CrashHelp
    {
        private GrondslagenContext DezeMapperVerwijderen { get; set; } = new();



    }

}
