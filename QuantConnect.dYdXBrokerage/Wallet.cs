/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Linq;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using QuantConnect.Brokerages.dYdX.Api;

namespace QuantConnect.Brokerages.dYdX;

/// <summary>
/// Represents a cryptocurrency wallet for dYdX operations
/// </summary>
public class Wallet
{
    /// <summary>
    /// Gets or sets the private key for the wallet
    /// </summary>
    public string PrivateKey { get; set; }

    public string Address { get; set; }
    public ulong AccountNumber { get; set; }
    public int SubaccountNumber { get; set; }
    public ulong Sequence { get; set; }

    /// <summary>
    /// Initializes a new instance of the Wallet class
    /// </summary>
    /// <param name="privateKey">The private key for the wallet. Can be null if constructing from mnemonic.</param>
    /// <param name="address">The address associated with the mnemonic</param>
    private Wallet(string privateKey, string address, ulong accountNumber, int subaccountNumber, ulong sequence)
    {
        Address = address;
        PrivateKey = privateKey;
        AccountNumber = accountNumber;
        SubaccountNumber = subaccountNumber;
        Sequence = sequence;
    }

    /// <summary>
    /// Creates a wallet from a BIP39 mnemonic phrase
    /// </summary>
    /// <param name="mnemonic">The mnemonic phrase (12, 15, 18, 21, or 24 words)</param>
    /// <param name="address">The address associated with the mnemonic</param>
    /// <param name="subaccountNumber">The subaccount number to use for this wallet</param>
    /// <returns>A new Wallet instance</returns>
    /// <exception cref="ArgumentException">Thrown when mnemonic is null, empty, or whitespace</exception>
    public static Wallet FromMnemonic(dYdXApiClient apiClient, string mnemonic, string address, int subaccountNumber)
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
            throw new ArgumentException("Mnemonic cannot be null or empty", nameof(mnemonic));
        }

        var privateKeyHex = PrivateKeyHexFromMnemonic(mnemonic);
        return FromPrivateKey(apiClient, privateKeyHex, address, subaccountNumber);
    }

    /// <summary>
    /// Creates a wallet from an existing private key
    /// </summary>
    /// <param name="apiClient">The dYdX API client</param>
    /// <param name="privateKeyHex">The hexadecimal private key string</param>
    /// <param name="address">The address associated with the mnemonic</param>
    /// <param name="subaccountNumber">The subaccount number to use for this wallet</param>
    /// <returns>A new Wallet instance initialized with the provided private key</returns>
    /// <exception cref="ArgumentException">Thrown when privateKey is null, empty, or whitespace</exception>
    public static Wallet FromPrivateKey(dYdXApiClient apiClient,
        string privateKeyHex,
        string address,
        int subaccountNumber)
    {
        if (string.IsNullOrWhiteSpace(privateKeyHex))
        {
            throw new ArgumentException("Private key cannot be null or empty", nameof(privateKeyHex));
        }

        var account = apiClient.GetAccount(address);

        return new Wallet(privateKeyHex, address, account.AccountNumber, subaccountNumber, account.Sequence);
    }

    private static string PrivateKeyHexFromMnemonic(string mnemonicPhrase)
    {
        // TODO: Implement BIP39 mnemonic to private key derivation
        throw new NotImplementedException();
    }

    public byte[] Sign(byte[] signDocBytes)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] messageHash32 = sha256.ComputeHash(signDocBytes);

        var privateKey32 = Convert.FromHexString(PrivateKey);

        // privateKey32: 32 bytes
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var d = new BigInteger(1, privateKey32);
        var priv = new ECPrivateKeyParameters(d, domain);

        var signer = new ECDsaSigner();
        signer.Init(true, priv);
        var rs = signer.GenerateSignature(messageHash32); // r, s as BigInteger

        BigInteger r = rs[0];
        BigInteger s = rs[1];

        // enforce low-s (s = min(s, n - s))
        if (s.CompareTo(domain.N.ShiftRight(1)) > 0)
            s = domain.N.Subtract(s);

        byte[] rBytes = r.ToByteArrayUnsigned();
        byte[] sBytes = s.ToByteArrayUnsigned();

        byte[] rPadded = new byte[32];
        byte[] sPadded = new byte[32];
        Buffer.BlockCopy(rBytes, 0, rPadded, 32 - rBytes.Length, rBytes.Length);
        Buffer.BlockCopy(sBytes, 0, sPadded, 32 - sBytes.Length, sBytes.Length);

        return rPadded.Concat(sPadded).ToArray(); // 64 bytes r||s
    }
}