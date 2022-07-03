using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class EnsResultInfo
{
    public int code { get; set; }
    public string message { get; set; }
}

public class EnsResult
{
    public EnsResultInfo resultInfo { get; set; }
    public string data { get; set; }
}

