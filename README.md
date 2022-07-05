# LoopringRedditNFTGiveawayBot
This is a Loopring Reddit NFT Giveaway bot made in .NET 6. You need an appropriate IDE that can compile it. I use Visual Studio 2022. This bot only works with a Metamask/Gamestop Wallet as you also need to export out the Layer 1 Private Key to sign the transactions to the Loopring API.

Precompiled releases for windows, MAC and Linux to come. 

This bot will monitor a Reddit post for comments with Loopring Addresses and then send those addresses an NFT. This works best for an NFT minted an X amount of times.

## Setup
When compiling yourself you need an appsettings.json file in the project directory with the property "Copy to Output directory" set to "Copy if newer". The file itself needs the following properties

```json
{
  "Settings": {
    "LoopringApiKey": "xxxxx", //Your loopring api key.  DO NOT SHARE THIS AT ALL.
    "LoopringLayer2PrivateKey": "0x", //Your loopring layer 2 private key.  DO NOT SHARE THIS AT ALL.
    "LoopringLayer1PrivateKey": "xxxx", //Your loopring layer 1 private key. DO NOT SHARE THIS AT ALL.
    "LoopringAddress": "0x36Cd6b3b9329c04df55d55D41C257a5fdD387ACd", //Your loopring address
    "LoopringAccountId": 40940, //Your loopring account id
    "MaxFeeTokenId": 1, //The token id for the fee. 0 for ETH, 1 for LRC
    "NftAmount": "1", //Amount of NFT to give out per redemption
    "NftTokenId": 34091, //Nft Token Id,
    "NftData": "0x124d7d15c114fc8bed6177bfc2fab059279ebea3710147cc55aa99ddc28d9506", //Nft Data
    "Subreddit": "fudgeysfactory", //The subreddit the post is in
    "RedditPostId": "vqyxaz", //The reddit post id
    "RedditAppId": "xxxx", //Reddit App Id
    "RedditRefreshToken": "xxx", //Reddit Refresh Token DO NOT SHARE THIS AT ALL
    "RedditAccessToken": "xxx", //Reddit Access Token DO NOT SHARE THIS AT ALL
    "Exchange": "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4", //Loopring Exchange address,
    "ValidUntil": 1700000000 //How long this transfer should be valid for. Shouldn't have to change this value
  }
}
```

You can export the Loopring related keys out from https://loopring.io. The NFTs will be sent from the account exported from, so mint/transfer the giveaway NFTs to that account.The Loopring Layer 1 private key can be exported out from Metamask/Gamestop Wallet. KEEP THESE KEYS PRIVATE 

You will need to create a Reddit Installed App and get your reddit app id, refresh token and access token. KEEP THESE TOKENS PRIVATE

A reddit post id is the "vqyxaz", the subreddit is "fudgeysfactory" in this Reddit URL below:

https://www.reddit.com/r/fudgeysfactory/comments/vqyxaz/nft_giveaway_test/

You can get the nftTokenId and nftData properties of the NFT by querying this Loopring API endpoint,replacing accountId with the accountId of the account the bot is associated with:

https://api3.loopring.io/api/v3/user/nft/balances?accountId=40940&limit=50&metadata=true&offset=0

A response will look similar to below

```json
"id": 703996,
"accountId": 40940,
"tokenId": 34091,
"nftData": "0x124d7d15c114fc8bed6177bfc2fab059279ebea3710147cc55aa99ddc28d9506",
"tokenAddress": "0xd988a1a8f9fdc3b816d032044feb48d3225c99ab",
"nftId": "0x17b8e5820619cbcc6c553d9c3a83303b2bc6edf06141be0261fe4d9cc2a2f901",
"nftType": "ERC1155"
```
You want the tokenId as the nftTokenId and nftData as nftData.

Once you have setup the appsettings.json file just run the program in your IDE. It will start monitoring the reddit post and send out the NFTs as new comments come in.
