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

    /// <summary>
    /// Initializes a new instance of the Wallet class
    /// </summary>
    /// <param name="privateKey">The private key for the wallet. Can be null if constructing from mnemonic.</param>
    /// <param name="address">The address associated with the mnemonic</param>
    private Wallet(string privateKey, string address)
    {
        Address = address;
        PrivateKey = privateKey;
    }

    /// <summary>
    /// Creates a wallet from a BIP39 mnemonic phrase
    /// </summary>
    /// <param name="mnemonic">The mnemonic phrase (12, 15, 18, 21, or 24 words)</param>
    /// <param name="address">The address associated with the mnemonic</param>
    /// <returns>A new Wallet instance</returns>
    /// <exception cref="ArgumentException">Thrown when mnemonic is null, empty, or whitespace</exception>
    public static Wallet FromMnemonic(string mnemonic, string address)
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
            throw new ArgumentException("Mnemonic cannot be null or empty", nameof(mnemonic));
        }

        var privateKeyHex = PrivateKeyHexFromMnemonic(mnemonic);
        return new Wallet(privateKeyHex, address);
    }

    /// <summary>
    /// Creates a wallet from an existing private key
    /// </summary>
    /// <param name="privateKeyHex">The hexadecimal private key string</param>
    /// <returns>A new Wallet instance initialized with the provided private key</returns>
    /// <exception cref="ArgumentException">Thrown when privateKey is null, empty, or whitespace</exception>
    public static Wallet FromPrivateKey(string privateKeyHex, string address)
    {
        if (string.IsNullOrWhiteSpace(privateKeyHex))
        {
            throw new ArgumentException("Private key cannot be null or empty", nameof(privateKeyHex));
        }

        return new Wallet(privateKeyHex, address);
    }

    private static string PrivateKeyHexFromMnemonic(string mnemonicPhrase)
    {
        // TODO: Implement BIP39 mnemonic to private key derivation
        throw new NotImplementedException();
    }
}