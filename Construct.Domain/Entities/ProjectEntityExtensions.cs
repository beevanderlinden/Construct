using Eurocode.BetonConstructies;

namespace Construct.Domain.Entities
{
    public static class ProjectEntityExtensions
    {
        public static void InitAll(this ProjectEntity project)
        {
            foreach (var assemblage in project.Assemblages)
            {
                assemblage?.Init(project.ProjectInfo);

                if (assemblage is not null)
                {

                    var beton = assemblage.Materiaal as BetonContext;

                    if (assemblage is SteekTrapEntity steektrap)
                    {

                        steektrap.GetToetsen();
                        steektrap.ProjectInfo = project.ProjectInfo;
                        steektrap.SetBeton(beton ?? new());
                        steektrap.SetGrondslagen(steektrap.ProjectInfo.Grondslagen); // voor onderliggende onderdelen
                        steektrap.SetProfiel(steektrap.ProfielSchil);
                    }


                }

            }
        }
    }
}
