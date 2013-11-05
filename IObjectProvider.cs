using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Ausm.ObjectStore
{
    /// <summary>
    /// Interface welches alle Funktionalitäten definiert die zum Abrufen von Datenobjekten notwendig sind.
    /// </summary>
    public interface IObjectProvider
    {
        /// <summary>
        /// Gibt ein ungefiltertes IQueryable des angegebenen Types zurück.
        /// </summary>
        IQueryable<T> GetQueryable<T>() where T : class;

        /// <summary>
        /// Fordert ein neu erstelltes Objekt vom ObjectProvider an
        /// </summary>
        T CreateObject<T>() where T : class;

        /// <summary>
        /// Gibt true zurück wenn der angegebene Type vom ObjectProvider unterstützt wird.
        /// </summary>
        bool SupportsType(Type type);
    }
}
