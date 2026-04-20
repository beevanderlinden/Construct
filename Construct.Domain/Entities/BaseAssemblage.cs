using CommonLibrary.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    public abstract class BaseAssemblage : IInitializeWithProjectInfo, IInitializeWithAssemblage, INotifyPropertyChanged
    {
        public virtual void Init(ProjectInfoEntity projectInfo) { }

        public void InitProjectInfo(object projectInfo)
        {
            throw new NotImplementedException();
        }

        public void Initialize(object assemblage)
        {
            throw new NotImplementedException();
        }

        [JsonPropertyOrder(-1000)]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [JsonPropertyOrder(-900)]
        public string? Merk { get; set; }

        [JsonPropertyOrder(-800)]
        public string? Naam { get; set; }

        /// <summary>
        /// Optionle omschrijving van deze assemblage, bijvoorbeeld voor extra details of opmerkingen.
        /// Bijvoorbeeld. Deze berekening geldt voor alle trappen kleiner of gelijk aan 8 treden.
        /// </summary>
        [JsonPropertyOrder(-700)]
        public string? Omschrijving { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
       

        protected bool SetNestedProperty<T>(
            ref T? field,
            T? value,
            [CallerMemberName] string? propertyName = null)
            where T : class, INotifyPropertyChanged
        {
            if (ReferenceEquals(field, value))
                return false;

            // oude handler loskoppelen
            if (field != null)
                field.PropertyChanged -= NestedPropertyChanged;

            field = value;

            // nieuwe handler koppelen
            if (field != null)
                field.PropertyChanged += NestedPropertyChanged;

            OnPropertyChanged(propertyName ?? string.Empty);
            return true;
        }

        // wordt overschreven in de child (optioneel)
        protected virtual void NestedPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // standaard niks
            Console.WriteLine($"[NestedPropertyChanged] ({e.PropertyName}) fires");
        }


        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));



    }

    public abstract class BasisBerekening
    {
        public string? Naam { get; set; }

    }

    public class BetonToets : BasisBerekening
    {

        public InterneKrachtenContext InterneKrachten { get; set; } = new();

    }

    public class InterneKrachtenContext
    {
        /// <summary>
        /// Normaalkracht
        /// </summary>
        public double Nx { get; set; }

        /// <summary>
        /// Dwarskracht
        /// </summary>
        public double Vz { get; set; }
        public double Vy { get; set; }

        /// <summary>
        /// Buiging
        /// </summary>
        public double My { get; set; }
        public double Mz { get; set; }

        /// <summary>
        /// Torsie
        /// </summary>
        public double Tx { get; set; }



    }



}
