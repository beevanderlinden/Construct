using Microsoft.Net.Http.Headers;
using Profielen.Parametrisch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Domain.Entities
{
    public class KolomEntity : AssemblageEntity
    {
        public KolomEntity() {
            Lengte = 3000;
        }

        
        private ParametrischProfielContext _profiel = new();
        
        
        
        public ParametrischProfielContext Profiel
        {
            get => _profiel;
            set
            {
                if (_profiel != value)
                {
                    _profiel = value;
                    SetNestedProperty(ref _profiel!, value);
                }
            }
        }


    }
}
