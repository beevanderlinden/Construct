using CommonLibrary;
using Construct.Application.Interfaces;
using Construct.Domain.Entities;
using Kaskon.Toolbox.PrefabModels;


namespace Construct.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IFileService _fileService;


        public ProjectService(IFileService fileService)
        {
            _fileService = fileService;
        }

        [TableColumn(Label = "test (Verwijderen)", Symbol = "x")]
        public double Test { get; set; }

        public async Task UpdateProjectAsync(Project project)
        {
            if (project != null)
            {
                await Task.CompletedTask;
            }
            //throw new NotImplementedException();
        }

        public Task UpdateProjectInfoAsync(ProjectEntity project)
        {

            throw new NotImplementedException();
        }


        public async Task VerwijderAssemblage(ProjectEntity project, Guid assemblageGuid)
        {
            var assemblage = project.Assemblages.FirstOrDefault(p => p.Guid == assemblageGuid);
            if (assemblage != null)
            {
                // controleer mogelijke link met andere assemblage
                foreach (var asmbly in project.Assemblages)
                {
                    if (asmbly is BordesEntity bordes)
                    {
                        if (bordes.Trap1.AansluitendElement?.Guid == assemblageGuid)
                        {
                            bordes.Trap1.AansluitendElement = null;
                        }
                        if (bordes.Trap2.AansluitendElement?.Guid == assemblageGuid)
                        {
                            bordes.Trap2.AansluitendElement = null;
                        }
                    }
                }
                


                project.Assemblages.Remove(assemblage);
            }
            await Task.CompletedTask;
        }

        public async Task SaveProjectAsync(string path, ProjectEntity project)
        {
            await _fileService.SaveAsync(path, project);
        }

        public async Task<string> GetGzippedJsonBase64Async(ProjectEntity project)
        {
            return await _fileService.GetGzippedJsonBase64Async(project);
        }

        public async Task<byte[]> GetGzippedBytes(ProjectEntity project)
        {
            return await _fileService.GetGzippedJsonBytesAsync(project);
        }

        public async Task<string> GetJsonBase64Async(ProjectEntity project)
        {
            return await _fileService.GetJsonBase64Async(project);
        }

        public async Task<ProjectEntity?> LoadProjectAsync(string path)
        {
            return await _fileService.LoadAsync<ProjectEntity>(path);
        }

        public string GetJson(ProjectEntity project)
        {
            return _fileService.GetJsonString(project);
        }

        public Task<string> GetJsonBase64Async<T>(T project)
        {
            return _fileService.GetJsonBase64Async(project);
        }

        public string GetJsonString<T>(T project)
        {
            return _fileService.GetJsonString(project);

        }

        //public Task<byte[]> GetProjectBytes(ProjectEntity project)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
