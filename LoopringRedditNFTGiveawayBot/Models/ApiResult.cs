using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class ApiResultInfo
{
    public int code { get; set; }
    public string message { get; set; }
}

public class ApiResult
{
    public ApiResultInfo resultInfo { get; set; }
}
