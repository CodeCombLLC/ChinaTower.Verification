using System.Collections.Generic;

namespace ChinaTower.Verification.Models.Infrastructures
{
    public static class Unique
    {
        public static Dictionary<FormType, int> Dictionary = new Dictionary<FormType, int>
        {
            { FormType.衰减器, 0},
            { FormType.直流配电设备, 0 },
            { FormType.合路器,1},
            { FormType.负载,0 },
            { FormType.交流配电,0 },
            { FormType.高压配电,0  },
            { FormType.室内天线,0 },
            { FormType.低压配电,0  },
            { FormType.机房,1 },
            { FormType.机房空调设备,4 },
            { FormType.其他设备,3},
            { FormType.室外天线,1 },
            { FormType.外市电引入, 3 },
            { FormType.POI,0},
            { FormType.动力及环境单元,0 },
            { FormType.铁塔,1 },
            { FormType.铁塔平面,1},
            { FormType.整流器设备,0},
            { FormType.站址,1},
            { FormType.UPS,1 },
            { FormType.变压稳压,1 },
            { FormType.功分器,3 }
        };
    }
}