using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mechanica.SimpleBeam
{
    public abstract class BaseDirtyTracking
    {
        public bool IsDirty { get; protected set; } = true;

        /// <summary>
        /// Stelt het veld in en zet IsDirty op true als het gewijzigd wordt
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                IsDirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handmatige reset van IsDirty
        /// </summary>
        public void ResetDirty() => IsDirty = false;
    }

}
