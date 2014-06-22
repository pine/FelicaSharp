using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FelicaSharp
{
    /// <summary>
    /// <para><see cref="Felica"/>で発生する独自の例外クラスです。</para>
    /// <seealso cref="System.Exception"/>
    /// </summary>
    public class FelicaException : Exception
    {
        public FelicaException() : base() { }
        public FelicaException(string message) : base(message) { }
    }
}
