using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class LoopringService : ILoopringService
{
    const string _baseUrl = "https://api3.loopring.io";

    readonly RestClient _client;

    public LoopringService()
    {
        _client = new RestClient(_baseUrl);
    }

    public StorageId? GetNextStorageId(string apiKey, int accountId, int sellTokenId)
    {
        var request = new RestRequest("api/v3/storageId");
        request.AddHeader("x-api-key", apiKey);
        request.AddParameter("accountId", accountId);
        request.AddParameter("sellTokenId", sellTokenId);
        try
        {
            var response = _client.Get<StorageId>(request);
            return response.Data;
        }
        catch (HttpRequestException httpException)
        {
            Console.WriteLine($"Error getting storage id: {httpException.Message}");
            return null;
        }
    }

    public OffchainFee? GetOffChainFee(string apiKey, int accountId, int requestType, string amount)
    {
        var request = new RestRequest("api/v3/user/nft/offchainFee");
        request.AddHeader("x-api-key", apiKey);
        request.AddParameter("accountId", accountId);
        request.AddParameter("requestType", requestType);
        request.AddParameter("amount", amount);
        try
        {
            var response = _client.Get<OffchainFee>(request);
            return response.Data;
        }
        catch (HttpRequestException httpException)
        {
            Console.WriteLine($"Error getting off chain fee: {httpException.Message}");
            return null;
        }
    }


    public string SubmitNftTransfer(
        string apiKey,
        string exchange,
        int fromAccountId,
        string fromAddress,
             int toAccountId,
             string toAddress,
             int nftTokenId,
             string nftAmount,
             int maxFeeTokenId,
             string maxFeeAmount,
             int storageId,
             long validUntil,
             string eddsaSignature,
             string ecdsaSignature,
             string nftData
        )
    {
        var request = new RestRequest("api/v3/nft/transfer", Method.POST);
        request.AddHeader("x-api-key", apiKey);
        request.AddHeader("x-api-sig", ecdsaSignature);
        request.AlwaysMultipartFormData = true;
        request.AddParameter("exchange", exchange);
        request.AddParameter("fromAccountId", fromAccountId);
        request.AddParameter("fromAddress", fromAddress);
        request.AddParameter("toAccountId", toAccountId);
        request.AddParameter("toAddress", toAddress);
        request.AddParameter("token.tokenId", nftTokenId);
        request.AddParameter("token.amount", nftAmount);
        request.AddParameter("token.nftData", nftData);
        request.AddParameter("maxFee.tokenId", maxFeeTokenId);
        request.AddParameter("maxFee.amount", maxFeeAmount);
        request.AddParameter("storageId", storageId);
        request.AddParameter("validUntil", validUntil);
        request.AddParameter("eddsaSignature", eddsaSignature);
        request.AddParameter("ecdsaSignature", ecdsaSignature);
        try
        {
            var response = _client.Execute(request);
            return response.Content;
        }
        catch (HttpRequestException httpException)
        {
            return null;
        }
    }

    public EnsResult GetHexAddress(string apiKey, string ens)
    {
        var request = new RestRequest("api/wallet/v3/resolveEns");
        request.AddHeader("x-api-key", apiKey);
        request.AddParameter("fullName", ens);
        try
        {
            var response = _client.Get<EnsResult>(request);
            return response.Data;
        }
        catch (HttpRequestException httpException)
        {
            Console.WriteLine($"Error getting ens: {httpException.Message}");
            return null;
        }
    }
}

