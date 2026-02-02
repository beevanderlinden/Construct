using Construct.Domain.Entities;

namespace Construct.Application.Interfaces
{
    public interface IAssemblageService
    {

        Task<List<AssemblageEntity>> GetByProjectAsync(ProjectEntity project);
        Task<AssemblageEntity> GetByGuidAsync(Guid guid);
        Task AddAsync(AssemblageEntity assemblage);
        Task UpdateAsync(AssemblageEntity assemblage);
        Task DeleteAsync(Guid guid);
    }
}
