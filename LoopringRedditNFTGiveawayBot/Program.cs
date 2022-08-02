
using Microsoft.Extensions.Configuration;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoseidonSharp;
using Reddit;
using Reddit.Controllers.EventArgs;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using Type = LoopringRedditNFTGiveawayBot.Type;

public static class Program
{
    static bool commentReply = true;
    static string ethAddressRegexPattern = @"0x[a-fA-F0-9]{40}";
    static string ensAddressRegexPattern = @"([^\s]{1,256}.\.eth)";
    static Settings settings { get; set; }
    static List<string> nftRecievers = new List<string>();
    static void Main(string[] args)
    {
        //var token = Helpers.AuthorizeUser("_zO6qlBIQxfpqk81uBq9DA");

        //Settings loaded from the appsettings.json file
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        settings = config.GetRequiredSection("Settings").Get<Settings>();

        string postId = "t3_" + settings.RedditPostId;

        var reddit = new RedditClient(appId: settings.RedditAppId, refreshToken: settings.RedditRefreshToken, accessToken: settings.RedditAccessToken);
        var post = reddit.Subreddit(settings.Subreddit).Post(postId).About();
        post.Comments.NewUpdated += C_NewPostsUpdated;
        post.Comments.MonitorNew();
        Console.WriteLine($"Now monitoring");
    }

    //methods inside aren't async because the storage id needs to be done synchronously
    public static void C_NewPostsUpdated(object sender, CommentsUpdateEventArgs e)
    {
        ILoopringService loopringService = new LoopringService();//Initialize loopring service
        foreach (var comment in e.Added)
        {
            var commentBody = "";
            if (string.IsNullOrEmpty(comment.Body))
            {
                Console.WriteLine("Comment body is empty! Skipping!");
                continue;
            }
            else
            {
                commentBody = comment.Body.ToLower();
            }

            var hexAddress = "";
            foreach (Match m in Regex.Matches(commentBody, ethAddressRegexPattern))
            {
                hexAddress = m.Value.ToLower();
                break;
            }

            var ensAddress = "";
            if (string.IsNullOrEmpty(hexAddress))
            {
                foreach (Match m in Regex.Matches(commentBody, ensAddressRegexPattern))
                {
                    ensAddress = m.Value.ToLower();
                    break;
                }
            }

            var workingAddress = "";

            if (!string.IsNullOrEmpty(ensAddress))
            {
                Console.WriteLine($"Detected Ens: {ensAddress}");
                workingAddress = ensAddress;
            }

            if (!string.IsNullOrEmpty(hexAddress))
            {
                workingAddress = hexAddress;
                Console.WriteLine($"Detected Hex Addresss: {hexAddress}");
            }

            if (string.IsNullOrEmpty(ensAddress) && string.IsNullOrEmpty(hexAddress))
            {
                Console.WriteLine("No loopring address in comment!");
                continue;
            }

            string loopringApiKey = settings.LoopringApiKey;//loopring api key KEEP PRIVATE
            string loopringPrivateKey = settings.LoopringLayer2PrivateKey; //loopring private key KEEP PRIVATE
            var metamaskPrivateKey = settings.LoopringLayer1PrivateKey; //metamask private key KEEP PRIVATE
            var fromAddress = settings.LoopringAddress; //your loopring address
            var fromAccountId = settings.LoopringAccountId; //your loopring account id
            var validUntil = settings.ValidUntil; //the examples seem to use this number
            var maxFeeTokenId = settings.MaxFeeTokenId; //0 should be for ETH, 1 is for LRC?
            var exchange = settings.Exchange; //loopring exchange address, shouldn't need to change this,
            int toAccountId = 0; //leave this as 0 DO NOT CHANGE

            string toAddress = workingAddress; //the Address to send it to
            if (toAddress.ToLower().Trim().Contains(".eth"))
            {
                var varHexAddress = loopringService.GetHexAddress(settings.LoopringApiKey, toAddress.ToLower().Trim());
                if (!String.IsNullOrEmpty(varHexAddress.data))
                {
                    toAddress = varHexAddress.data;
                }
                else
                {
                    try
                    {
                        Console.WriteLine($"{workingAddress} is not a valid ENS! Skipping!");
                        comment.Reply($"{workingAddress} is not a valid ENS! Please make a new comment with a valid address!");
                        continue;
                    }
                    catch (Reddit.Exceptions.RedditRateLimitException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                    catch (Reddit.Exceptions.RedditControllerException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }
            }

            if (nftRecievers.Contains(toAddress))
            {
                Console.WriteLine("Already sent! Skipping");
                try
                {
                    comment.Reply("You've already claimed the giveaway!");
                    continue;
                }
                catch (Reddit.Exceptions.RedditRateLimitException ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
                catch (Reddit.Exceptions.RedditControllerException ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }

            StorageId? storageId = null;
            while (storageId == null) //put in while loop as loopring api returns null sometimes
            {
                storageId = loopringService.GetNextStorageId(loopringApiKey, fromAccountId, settings.NftTokenId);
            }
            OffchainFee? offChainFee = null;
            while (offChainFee == null) //put in while loop as loopring api returns null sometimes
            {
                offChainFee = loopringService.GetOffChainFee(loopringApiKey, fromAccountId, 11, "0");
            }

            //Calculate eddsa signautre
            BigInteger[] poseidonInputs =
            {
                Utils.ParseHexUnsigned(exchange),
                (BigInteger) fromAccountId,
                (BigInteger) toAccountId,
                (BigInteger) settings.NftTokenId,
                BigInteger.Parse(settings.NftAmount),
                (BigInteger) maxFeeTokenId,
                BigInteger.Parse(offChainFee.fees[maxFeeTokenId].fee),
                Utils.ParseHexUnsigned(toAddress),
                (BigInteger) 0,
                (BigInteger) 0,
                (BigInteger) validUntil,
                (BigInteger) storageId.offchainId
};
            Poseidon poseidon = new Poseidon(13, 6, 53, "poseidon", 5, _securityTarget: 128);
            BigInteger poseidonHash = poseidon.CalculatePoseidonHash(poseidonInputs);
            Eddsa eddsa = new Eddsa(poseidonHash, loopringPrivateKey);
            string eddsaSignature = eddsa.Sign();

            //Calculate ecdsa
            string primaryTypeName = "Transfer";
            TypedData eip712TypedData = new TypedData();
            eip712TypedData.Domain = new Domain()
            {
                Name = "Loopring Protocol",
                Version = "3.6.0",
                ChainId = 1,
                VerifyingContract = "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4",
            };
            eip712TypedData.PrimaryType = primaryTypeName;
            eip712TypedData.Types = new Dictionary<string, MemberDescription[]>()
            {
                ["EIP712Domain"] = new[]
                    {
                        new MemberDescription {Name = "name", Type = "string"},
                        new MemberDescription {Name = "version", Type = "string"},
                        new MemberDescription {Name = "chainId", Type = "uint256"},
                        new MemberDescription {Name = "verifyingContract", Type = "address"},
                    },
                [primaryTypeName] = new[]
                    {
                        new MemberDescription {Name = "from", Type = "address"},            // payerAddr
                        new MemberDescription {Name = "to", Type = "address"},              // toAddr
                        new MemberDescription {Name = "tokenID", Type = "uint16"},          // token.tokenId 
                        new MemberDescription {Name = "amount", Type = "uint96"},           // token.volume 
                        new MemberDescription {Name = "feeTokenID", Type = "uint16"},       // maxFee.tokenId
                        new MemberDescription {Name = "maxFee", Type = "uint96"},           // maxFee.volume
                        new MemberDescription {Name = "validUntil", Type = "uint32"},       // validUntill
                        new MemberDescription {Name = "storageID", Type = "uint32"}         // storageId
                    },

            };
            eip712TypedData.Message = new[]
            {
                new MemberValue {TypeName = "address", Value = fromAddress},
                new MemberValue {TypeName = "address", Value = toAddress},
                new MemberValue {TypeName = "uint16", Value = settings.NftTokenId},
                new MemberValue {TypeName = "uint96", Value = BigInteger.Parse(settings.NftAmount)},
                new MemberValue {TypeName = "uint16", Value = maxFeeTokenId},
                new MemberValue {TypeName = "uint96", Value = BigInteger.Parse(offChainFee.fees[maxFeeTokenId].fee)},
                new MemberValue {TypeName = "uint32", Value = validUntil},
                new MemberValue {TypeName = "uint32", Value = storageId.offchainId},
            };

            TransferTypedData typedData = new TransferTypedData()
            {
                domain = new TransferTypedData.Domain()
                {
                    name = "Loopring Protocol",
                    version = "3.6.0",
                    chainId = 1,
                    verifyingContract = "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4",
                },
                message = new TransferTypedData.Message()
                {
                    from = fromAddress,
                    to = toAddress,
                    tokenID = settings.NftTokenId,
                    amount = settings.NftAmount,
                    feeTokenID = maxFeeTokenId,
                    maxFee = offChainFee.fees[maxFeeTokenId].fee,
                    validUntil = (int)validUntil,
                    storageID = storageId.offchainId
                },
                primaryType = primaryTypeName,
                types = new TransferTypedData.Types()
                {
                    EIP712Domain = new List<Type>()
                    {
                        new Type(){ name = "name", type = "string"},
                        new Type(){ name="version", type = "string"},
                        new Type(){ name="chainId", type = "uint256"},
                        new Type(){ name="verifyingContract", type = "address"},
                    },
                    Transfer = new List<Type>()
                    {
                        new Type(){ name = "from", type = "address"},
                        new Type(){ name = "to", type = "address"},
                        new Type(){ name = "tokenID", type = "uint16"},
                        new Type(){ name = "amount", type = "uint96"},
                        new Type(){ name = "feeTokenID", type = "uint16"},
                        new Type(){ name = "maxFee", type = "uint96"},
                        new Type(){ name = "validUntil", type = "uint32"},
                        new Type(){ name = "storageID", type = "uint32"},
                    }
                }
            };

            Eip712TypedDataSigner signer = new Eip712TypedDataSigner();
            var ethECKey = new Nethereum.Signer.EthECKey(metamaskPrivateKey.Replace("0x", ""));
            var encodedTypedData = signer.EncodeTypedData(eip712TypedData);

            var encodedTypeDataString = System.Text.Encoding.Default.GetString(encodedTypedData);

            var ECDRSASignature = ethECKey.SignAndCalculateV(Sha3Keccack.Current.CalculateHash(encodedTypedData));
            var serializedECDRSASignature = EthECDSASignature.CreateStringSignature(ECDRSASignature);
            var ecdsaSignature = serializedECDRSASignature + "0" + (int)2;

            //Submit nft transfer
            var nftTransferResponseObject = loopringService.SubmitNftTransfer(
                apiKey: loopringApiKey,
                exchange: exchange,
                fromAccountId: fromAccountId,
                fromAddress: fromAddress,
                toAccountId: toAccountId,
                toAddress: toAddress,
                nftTokenId: settings.NftTokenId,
                nftAmount: settings.NftAmount,
                maxFeeTokenId: maxFeeTokenId,
                maxFeeAmount: offChainFee.fees[maxFeeTokenId].fee,
                storageId.offchainId,
                validUntil: validUntil,
                eddsaSignature: eddsaSignature,
                ecdsaSignature: ecdsaSignature,
                nftData: settings.NftData
                );
            if (nftTransferResponseObject != null)
            {
                if (nftTransferResponseObject.Contains("resultInfo"))
                {
                    var result = JsonConvert.DeserializeObject<ApiResult>(nftTransferResponseObject);
                    Console.WriteLine(value: $"Transfer to {toAddress} failed with error: {result.resultInfo.message}");
                    try
                    {
                        if(result.resultInfo.message.ToLower().Contains("not enough"))
                        {
                            Console.WriteLine("***RAN OUT OF NFTs....TERMINATING PROGRAM***");
                            System.Environment.Exit(0);
                        }
                        else
                        {
                            comment.Reply($"Transfer failed with error: {result.resultInfo.message}. Please make a new comment with your address and try again...");
                            continue;
                        }
                    }
                    catch (Reddit.Exceptions.RedditRateLimitException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                    catch (Reddit.Exceptions.RedditControllerException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }
                else
                {
                    var result = JsonConvert.DeserializeObject<TransferResponse>(nftTransferResponseObject);
                    if (result.status.Contains("process") || result.status.Contains("received"))
                    {
                        nftRecievers.Add(toAddress);
                        Console.WriteLine($"Transfer to {toAddress} successful!");
                        if (commentReply == true)
                        {
                            try
                            {
                                comment.Reply($"Sent! Transaction Hash: {result.hash}");
                                continue;
                            }
                            catch(Reddit.Exceptions.RedditRateLimitException ex)
                            {
                                Console.WriteLine(ex.Message);
                                continue;
                            }
                            catch (Reddit.Exceptions.RedditControllerException ex)
                            {
                                Console.WriteLine(ex.Message);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Transfer to {toAddress} failed!");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Transfer to {toAddress} unsuccessful!");
            }
        }
    }
}








