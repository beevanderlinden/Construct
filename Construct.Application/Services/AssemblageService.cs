using Construct.Application.Interfaces;
using Construct.Domain.Entities;

namespace Construct.Application.Services
{
    public class AssemblageService : IAssemblageService
    {
        //public AssemblageService(ProjectEntity project)
        //{
        //    _project = project;
        //}


        //private ProjectEntity _project;
        private List<AssemblageEntity> Assemblages { get; set; } = [];


        public Task AddAsync(AssemblageEntity assemblage)
        {


            throw new NotImplementedException();
        }

        public async Task DeleteAsync(Guid guid)
        {
            var assemblage = Assemblages.FirstOrDefault(p => p.Guid == guid);
            if (assemblage != null)
            {
                Assemblages.Remove(assemblage);
            }
            await Task.CompletedTask;
        }

        public Task<AssemblageEntity> GetByGuidAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AssemblageEntity>> GetByProjectAsync(ProjectEntity project)
        {
            Assemblages = project.Assemblages;
            return await Task.FromResult(Assemblages);
        }

        public Task UpdateAsync(AssemblageEntity assemblage)
        {
            throw new NotImplementedException();
        }
    }
}
