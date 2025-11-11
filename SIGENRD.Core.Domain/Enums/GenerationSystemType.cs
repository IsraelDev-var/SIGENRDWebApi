using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGENRD.Core.Domain.Enums
{
    public enum GenerationSystemType
    {
        Photovoltaic = 1,   // Solar
        Wind = 2,           // Eólico
        Biomass = 3,
        Hydro = 4,          // Microhidroeléctrico
        Hybrid = 5,
        Other = 6
    }
}
