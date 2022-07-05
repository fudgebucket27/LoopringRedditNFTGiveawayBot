using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Settings
{
    public string LoopringApiKey { get; set; }
    public string LoopringLayer2PrivateKey { get; set; }
    public string LoopringLayer1PrivateKey { get; set; }
    public string LoopringAddress { get; set; }
    public int LoopringAccountId { get; set; }
    public long ValidUntil { get; set; }
    public int MaxFeeTokenId { get; set; }
    public string Exchange { get; set; }
    public string NftAmount { get; set; }
    public int NftTokenId { get; set; }
    public string NftData { get; set; }
    public string Subreddit { get; set; }
    public string RedditPostId { get; set; }

    public string RedditAppId { get; set; }
    public string RedditRefreshToken { get; set; }
    public string RedditAccessToken { get; set; }
}

