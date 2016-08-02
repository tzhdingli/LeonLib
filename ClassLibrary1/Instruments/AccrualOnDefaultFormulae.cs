using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Instruments
{
    public enum AccrualOnDefaultFormulae
    {

        /// <summary>
        /// The formula in v1.8.1 and below.
        /// </summary>
        ORIGINAL_ISDA,
        /// <summary>
        /// The correction proposed by Markit (v 1.8.2).
        /// </summary>
        MARKIT_FIX,
        /// <summary>
        /// The mathematically correct formula .
        /// </summary>
        CORRECT

    }

}
