using CommonLibrary;
using CommonLibrary.Models;
using Construct.Domain.Common;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    //public class PrefabBetonAssemblageEntity : BaseAssemblage
    //{
    //    public List<BetonDekkingContext> Dekkingen { get; set; } = [new BetonDekkingContext(), new BetonDekkingContext()];
    //}

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(SteekTrapEntity), "steektrap")]
    [JsonDerivedType(typeof(BordesEntity), typeDiscriminator: "Bordes")]
    [JsonDerivedType(typeof(KolomEntity), typeDiscriminator: "Kolom")]
    [JsonDerivedType(typeof(LiggerEntity), typeDiscriminator: "Ligger")]



    public abstract class AssemblageEntity : BaseAssemblage
    {
        // JSON opslag
        // ✅ MateriaalReference beheert zowel EntityId als Entity

        // internal properties and methods can go here...
        internal BetonContext _beton = new("C45/55");
        
        // ✅ NIEUW: MateriaalReference voor generieke materiaal-referentie-beheer
        private readonly MateriaalReference _materiaalRef = new();

        private readonly List<BaseEurocodeContext> _toetsen = [];
        private readonly List<StrookEntity> _stroken = [];
        private ProjectInfoEntity _projectInfo = new();
        private BelastingenContext _belastingen = new();
        private DekkingContext _plaatDekking = new();
        
        [JsonPropertyOrder(100)]
        public virtual double Breedte { get; set; } = 1200;
        
        [JsonPropertyOrder(101)]
        public virtual double Lengte { get; set; } = 3000;
        
        [JsonPropertyOrder(102)]
        public virtual double Hoogte { get; set; } = 2200;
        

        public ProjectInfoEntity ProjectInfo 
        { 
            get => _projectInfo; 
            set
            {
                if (_projectInfo?.Grondslagen != null)
                    _projectInfo.Grondslagen.PropertyChanged -= OnGrondslagenPropertyChanged;
                
                _projectInfo = value;

                if (_projectInfo?.Grondslagen != null)
                    _projectInfo.Grondslagen.PropertyChanged += OnGrondslagenPropertyChanged;
            } 
        } 

        public BelastingenContext Belastingen
        {
            get => _belastingen;
            set
            {
                if (_belastingen != null)
                    _belastingen.PropertyChanged -= OnBelastingenPropertyChanged;
                _belastingen = value;
                if (_belastingen != null)
                    _belastingen.PropertyChanged += OnBelastingenPropertyChanged;
            }
        }

        /// <summary>
        /// Permanente afwerking in kN/m² voor bijvoorbeeld vloerafwerking, hekwerk etcetera.
        /// </summary>
        public double AfwerkingVlaklast { get; set; }


        protected virtual void OnGrondslagenPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle the property change event here
            // For example, you might want to notify that the AssemblageEntity has changed
            OnPropertyChanged(nameof(ProjectInfo));
        }

        protected virtual void OnBelastingenPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle the property change event here
            // For example, you might want to notify that the AssemblageEntity has changed
            OnPropertyChanged(nameof(Belastingen));
        }


        /// <summary>
        /// Het materiaal-ID van dit assemblage. Wordt geserialiseerd naar JSON.
        /// Gekoppeld aan het werkelijke Materiaal object via MateriaalReference.
        /// </summary>
        [JsonPropertyOrder(-700)]
        public Guid? MateriaalId
        {
            get => _materiaalRef.EntityId;
            set => _materiaalRef.EntityId = value;
        }

        /// <summary>
        /// Het materiaal van dit assemblage (beton, staal, hout).
        /// ⚠️ NIET geserialiseerd - gebruik MateriaalId voor JSON-opslag!
        /// </summary>
        [JsonIgnore]
        public BaseMateriaal? Materiaal
        {
            get => _materiaalRef.Entity;
            set => _materiaalRef.Attach(value);
        }

        /// <summary>
        /// Dekking context voor dekking en duurzaamheid aan de onderzijde en bovenzijde van een beton-element
        /// </summary>
        public DekkingContext PlaatDekking
        {
            get => _plaatDekking;
            set => SetNestedProperty(ref (_plaatDekking!), value);
        }


        [JsonIgnore]
        public IEnumerable<BaseEurocodeContext> Toetsen
        {
            get => _toetsen;
            //set => _toetsen = [.. value];
        }
        protected void AddToets(BaseEurocodeContext toets)
        {
            if (!_toetsen.Contains(toets))
            {
                _toetsen.Add(toets);
                OnPropertyChanged(nameof(Toetsen));
            }
        }
        protected void RemoveToets(BaseEurocodeContext toets)
        {
            _toetsen.Remove(toets);
            OnPropertyChanged(nameof(Toetsen));
        }
        protected void ClearToetsen()
        {
            _toetsen.Clear();
            OnPropertyChanged(nameof(Toetsen));
        }
        protected void AddToetsen(IEnumerable<BaseEurocodeContext> toetsen)
        {
            _toetsen.AddRange(toetsen.Where(t => !_toetsen.Contains(t)));
            OnPropertyChanged(nameof(Toetsen));
        }
        
        [JsonIgnore]
        public IEnumerable<StrookEntity> Stroken
        {
            get => _stroken;
        }
        protected void AddStrook(StrookEntity strook)
        {
            if (!_stroken.Contains(strook))
            {
                strook.Father = this;
                strook.Beam.LoadContext = this.Belastingen;
                _stroken.Add(strook);
                OnPropertyChanged(nameof(Stroken));
            }
        }
        protected void AddStroken(IEnumerable<StrookEntity> stroken)
        {
            _stroken.AddRange(stroken.Where(s=> !_stroken.Contains(s)));
            OnPropertyChanged(nameof(Stroken));
        }
        protected void ClearStroken()
        {
            _stroken.Clear();
            OnPropertyChanged(nameof(Stroken));
        }
        protected void RemoveStrook(StrookEntity strook)
        {
            _stroken.Remove(strook);
            OnPropertyChanged(nameof(Stroken));
        }

        public string EngineeringCategorie { get; set; } = "Berekening conform kiwa criteria 73 - categorie 3";

        /// <summary>
        /// Of dit assemblage een prefab element is
        /// dus een prefab beton element, stalen element of houten element
        /// 
        /// </summary>
        public bool IsPrefab { get; set; } = true;


        public AssemblageTypeEnum? AssemblageType { get; set; } = AssemblageTypeEnum.BetonAssemblage;
        public enum AssemblageTypeEnum
        {
            [Display(Name = "Beton assemblage")]
            BetonAssemblage = 1,

            [Description("Staal assemblage")]
            StaalAssemblage = 2,

            [Display(Name = "Hout assemblage")]
            HoutAssemblage = 4,

            
        }


        private GebruiksklasseEnum? _gebruiksklasse = GebruiksklasseEnum.B_kantoorgebouwen;
        public GebruiksklasseEnum? Gebruiksklasse 
        {
            get => _gebruiksklasse;
            set => SetProperty(ref _gebruiksklasse, value);
        }

        public string GebruiksklasseUserFriendlyName
        {
            get
            {
                return Gebruiksklasse?.GetDisplayName(toLower: false) ?? "Geen gebruiksklasse geselecteerd";
            }
        }


        /// <summary>
        /// Herstelt materiaal-referenties na JSON-deserialisatie.
        /// Wordt aangeroepen vanuit RestoreReferencesAfterDeserialization().
        /// </summary>
        /// <param name="project">Het project met alle beschikbare materialen</param>
        protected void RestoreMaterialReference(ProjectEntity project)
        {
            _materiaalRef.Restore(
                guid => project.Materialen.TryGetValue(guid, out var mat) ? mat : null
            );
        }

        /// <summary>
        /// Herstelt object-referenties en relaties na JSON-deserialisatie.
        /// Roept slechts eenmaal aan via ProjectStateService.RestoreNavigationProperties()
        /// </summary>
        public virtual void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            if (project == null) return;

            var projectInfo = project.ProjectInfo;

            // ✅ ProjectInfo setter
            ProjectInfo = projectInfo;

            // ✅ Belastingen getter/setter
            Belastingen ??= new(grondslagen: ProjectInfo.Grondslagen);
            Belastingen.Grondslagen = ProjectInfo.Grondslagen;

            // ✅ Controleer of BelastingCombinaties leeg zijn en genereer indien nodig
            if (Belastingen.BelastingCombinaties.Count == 0)
            {
                Belastingen.GenereerBelastingCombinaties(
                    Belastingen,
                    Belastingen.BelastingGevallen,
                    Belastingen.CombinatiesTypes);
            }

            // ✅ NIEUW: Materiaal-referentie herstellen
            RestoreMaterialReference(project);
        }

        public virtual void Bijwerken()
        {
            // iedere afgeleide mag zijn eigen interpretatie invullen
            // in de basis gebeurt er niets

        }
        public DateTime? Bijgewerkt { get; set; } = DateTime.Now;

    }

}

