using Eurocode.BetonConstructies;

namespace Construct.Domain.Entities
{
    public static class ProjectEntityExtensions
    {
        public static void InitAll(this ProjectEntity project)
        {
            foreach (var assemblage in project.Assemblages)
            {
                if (assemblage is null) continue;

                // ✅ Init() is al aangeroepen in RestoreReferencesAfterDeserialization()
                // Dit gebeurt NADAT Materiaal hersteld is
                // InitAll() stelt ALLEEN de nested properties in

                var beton = assemblage.Materiaal as BetonContext;

                // Set nested properties voor ALLE assemblages (niet alleen SteekTrap)
                if (assemblage is SteekTrapEntity steektrap)
                {
                    steektrap.GetToetsen();
                    steektrap.ProjectInfo = project.ProjectInfo;
                    steektrap.SetBeton(beton ?? new());
                    steektrap.SetGrondslagen(steektrap.ProjectInfo.Grondslagen);
                    steektrap.SetProfiel(steektrap.ProfielSchil);
                }
                // TODO: Voeg SetBeton/SetGrondslagen/SetProfiel toe voor andere assemblagetypen als nodig
            }
        }
    }
}

