using System.ComponentModel;

namespace Construct.Domain.Entities
{
    public enum SteekTrapTypeEnum
    {
        [Description("Type 1 | standaard")] Standaard = 1,
        [Description("Type 2 | trap-bordes")] TrapBordes = 2,
        [Description("Type 3 | bordes-trap")] BordesTrap = 3,
        [Description("Type 4 | bordes-trap-bordes")] BordesTrapBordes = 4,
        [Description("Type 6 | trap-bordes-trap")] TrapBordesTrap = 6,
        [Description("Type 8 | b-t-b-t")] BordesTrapBordesTrap = 8,
        [Description("Type 9 | t-b-t-b")] TrapBordesTrapBordes = 9,
        [Description("Type 10 | b-t-b-t-b")] BordesTrapBordesTrapBordes = 10,
    }





}
