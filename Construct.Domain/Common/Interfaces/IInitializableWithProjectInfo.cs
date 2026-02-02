using Construct.Domain.Entities;

namespace Construct.Domain.Common.Interfaces
{
    public interface IInitializableWithProjectInfo
    {
        void Init(ProjectInfoEntity projectInfo);
    }
}
