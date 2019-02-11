using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class nOS : MonoBehaviour
{
    private string Address;
    private string PubKey;
    private string MCTBalance;
    private string RHTBalance;
    private string MCTHash = "a87cc2a513f5d8b4a42432343687c2127c60bc3f";
    private string RHTHash = "2328008e6f6c7bd157a342e789389eb034d9cbc4";
    private string NEO = "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";
    private string GAS = "602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";
    private string LastBlock;
    private string Circulation;
    private string RHTName;
    private string TXID;

    // nOS API call wrapper
    [DllImport("__Internal")]
    private static extern void nOSAPICall(string jparams);

    // message listener that passes data to callbacks
    [DllImport("__Internal")]
    private static extern void StartEventListener();

    public TMP_Text messageText;

    private int _requestCount = 0;
    private Dictionary<string, string> ResultQueue;

    void Start()
    {
        ResultQueue = new Dictionary<string, string>();

#if UNITY_WEBGL && !UNITY_EDITOR
        StartEventListener();
        StartCoroutine(GetInfo());
#else
        Debug.LogError("Must run as WebGL inside the nOS client");
#endif

    }

    public IEnumerator GetInfo()
    {

        yield return SendRequest("getAddress", (result) => { Address = result; });
        messageText.text = $"Welcome to Unity, {Address}";

        yield return SendRequest("getLastBlock", (result) => { LastBlock = JObject.Parse(result)["index"].ToString(); });
        messageText.text += $"\nLast Block: {LastBlock}";

        yield return SendRequest("getPublicKey", (result) => { PubKey = result; });
        messageText.text += $"\nPublic Key: {PubKey}";

        yield return SendRequest("getBalance", new { asset = MCTHash }, (result) => { MCTBalance = result; });
        messageText.text += $"\nMCT Balance: {MCTBalance}";

        yield return SendRequest("getBalance", new { asset = RHTHash }, (result) => { RHTBalance = result; });
        messageText.text += $"\nRHT Balance: {RHTBalance}";

        yield return SendRequest("getStorage", new { scriptHash = RHTHash, key = "in_circulation", decodeInput = true, decodeOutput = false },
                                                         (result) => { Circulation = HexToInt(result).ToString(); });

        yield return SendRequest("testInvoke", new {
            scriptHash = RHTHash,
            operation = "name",
            args = new string[] { } },
            (result) => { RHTName = Encoding.ASCII.GetString(StringToByteArray(JObject.Parse(result)["stack"][0]["value"].ToString())); });
        messageText.text += $"\nTotal {RHTName} supply: {Circulation}";

        yield return SendRequest("invoke", new {
                scriptHash = MCTHash,
                operation = "transfer",
                args = new string[] { MCTHash, MCTHash, "01" },
                encodeArgs = false
            },
            (result) => { TXID = result; });
        messageText.text += $"\nSent 0.00000001 MCT in TX {TXID}";

        /*
        // Would need to wait for the next block since fee is UTXO      
        yield return SendRequest("send", new { 
            asset = GAS, 
            amount = "0.00000001", 
            receiver = "AMh8o3uv5PwdryBsiZPd5zoVBDVaredZLG" 
            },
            (result) => { TXID = result; });
        messageText.text += $"\nSent 0.00000001 GAS in TX {TXID}";
        */
    }

    public IEnumerator SendRequest(string request, Action<string> callback)
    {
        yield return SendRequest(request, "", callback);
    }

    public IEnumerator SendRequest(string request, object oparams, Action<string> callback)
    {
        Debug.Log("Converting object to json");
        string jparams = JsonConvert.SerializeObject(oparams);
        yield return SendRequest(request, jparams, callback);
    }

    public IEnumerator SendRequest(string request, string jparams, Action<string> callback)
    {
        _requestCount += 1;
        string reqid = _requestCount.ToString();
        Debug.Log($"Sending request {reqid}: {request}({jparams})");
        if (jparams == "")
        {
            nOSRequest req = new nOSRequest(request, reqid);
            nOSAPICall(JsonUtility.ToJson(req));
        }
        else
        {
            nOSRequestWithParameters req = new nOSRequestWithParameters(request, jparams, reqid);
            nOSAPICall(JsonUtility.ToJson(req));
        }
        yield return new WaitUntil(() => ResultQueue.ContainsKey(reqid));
        string result = ResultQueue[reqid];
        ResultQueue.Remove(reqid);
        callback(result);
    }

    public void nOSResponseHandler(string jresponse)
    {
        nOSResult response = JsonUtility.FromJson<nOSResult>(jresponse);
        if (response.errorState)
        {
            Debug.LogError($"Request {response.requestId} failed: {response.resultData}");
            ResultQueue.Add(response.requestId, "");
        }
        else
        {
            ResultQueue.Add(response.requestId, response.resultData);
        }
    }

    private ulong HexToInt(string hex)
    {
       return BitConverter.ToUInt64(StringToByteArray(hex.PadRight(16, '0')), 0);
    }

    private byte[] StringToByteArray(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        int bl = bytes.Length;
        for (int i = 0; i < bl; ++i)
        {
            bytes[i] = (byte)((hex[2 * i] > 'F' ? hex[2 * i] - 0x57 : hex[2 * i] > '9' ? hex[2 * i] - 0x37 : hex[2 * i] - 0x30) << 4);
            bytes[i] |= (byte)(hex[2 * i + 1] > 'F' ? hex[2 * i + 1] - 0x57 : hex[2 * i + 1] > '9' ? hex[2 * i + 1] - 0x37 : hex[2 * i + 1] - 0x30);
        }
        return bytes;
    }

}

public class nOSResult
{
    public string requestId;
    public string resultData;
    public bool errorState;

    public nOSResult(string _id, string _data, bool _state)
    {
        requestId = _id;
        resultData = _data;
        errorState = _state;
    }

}

public class nOSRequest
{
    public string name;
    public string reqid;

    public nOSRequest(string _name, string _reqid)
    {
        name = _name;
        reqid = _reqid;
    }
}

public class nOSRequestWithParameters
{
    public string name;
    public string config;
    public string reqid;

    public nOSRequestWithParameters(string _name, string _config, string _reqid)
    {
        name = _name;
        config = _config;
        reqid = _reqid;
    }
}