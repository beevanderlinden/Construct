using Construct.Domain.Entities;
using ExportFactory.MigraDocContentModels;

namespace Construct.Application.Services
{
    public class RapportService
    {

        public required DocumentContent Content { get; set; }
        public required MigraDoc.DocumentObjectModel.Document Document { get; set; }

        public required ProjectEntity Project { get; set; }

    }
}
