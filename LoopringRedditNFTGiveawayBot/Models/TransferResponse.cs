using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class TransferResponse
{

    public string hash { get; set; }
    public string status { get; set; }
    public bool isIdempotent { get; set; }
    public int accountId { get; set; }
    public int tokenId { get; set; }
    public int storageId { get; set; }
}
