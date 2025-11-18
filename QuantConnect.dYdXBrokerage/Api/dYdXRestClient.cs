using System;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.Template.Api;

public class dYdXRestClient(string baseUrl) : IDisposable
{
    private readonly RestClient _restClient = new(baseUrl);
    private readonly RateGate _rateGate = new(250, TimeSpan.FromMinutes(1));

    public T Get<T>(string path)
    {
        _rateGate.WaitToProceed();
        var result = _restClient.Execute(new RestRequest(path));
        return EnsureSuccessAndParse<T>(result);
    }

    /// <summary>
    /// Ensures the request executed successfully and returns the parsed business object
    /// </summary>
    /// <param name="response">The response to parse</param>
    /// <typeparam name="T">The type of the response business object</typeparam>
    /// <returns>The parsed response business object</returns>
    /// <exception cref="Exception"></exception>
    [StackTraceHidden]
    private T EnsureSuccessAndParse<T>(IRestResponse response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("dYdXRestClient request failed: " +
                                $"[{(int)response.StatusCode}] {response.StatusDescription}, " +
                                $"Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
        }

        T responseObject = default;
        try
        {
            responseObject = JsonConvert.DeserializeObject<T>(response.Content);
        }
        catch (Exception e)
        {
            throw new Exception("dYdXRestClient failed deserializing response: " +
                                $"[{(int)response.StatusCode}] {response.StatusDescription}, " +
                                $"Content: {response.Content}, ErrorMessage: {response.ErrorMessage}", e);
        }

        if (Log.DebuggingEnabled)
        {
            Log.Debug(
                $"dYdX request for {response.Request.Resource} executed successfully. Response: {response.Content}");
        }

        return responseObject;
    }

    public void Dispose()
    {
        _rateGate?.DisposeSafely();
    }
}