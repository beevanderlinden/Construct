using Construct.Domain.Entities;

namespace Construct.Application.Interfaces
{
    public interface IProjectService
    {
        Task UpdateProjectAsync(Kaskon.Toolbox.PrefabModels.Project project);

        Task UpdateProjectInfoAsync(ProjectEntity project);

        Task VerwijderAssemblage(ProjectEntity project, Guid assemblageGuid);

        Task SaveProjectAsync(string path, ProjectEntity project);

        Task<string> GetGzippedJsonBase64Async(ProjectEntity project);

        Task<byte[]> GetGzippedBytes(ProjectEntity project);

        //Task<byte[]> GetProjectBytes(ProjectEntity project);

        Task<string> GetJsonBase64Async<T>(T project);
        string GetJsonString<T>(T project);

        Task<ProjectEntity?> LoadProjectAsync(string path);



    }
}
