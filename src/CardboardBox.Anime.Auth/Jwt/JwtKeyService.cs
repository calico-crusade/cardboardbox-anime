using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace CardboardBox.Anime.Auth.Jwt;

/// <summary>
/// A service for managing JWT keys
/// </summary>
public interface IJwtKeyService
{
	/// <summary>
	/// Encrypts the given token signature using an RSA key
	/// </summary>
	/// <param name="token">The un-encrypted token</param>
	/// <param name="cancel">The cancellation token for the request</param>
	/// <returns>The encrypted token</returns>
	Task<string> Encrypt(string token, CancellationToken cancel);

	/// <summary>
	/// Decrypts the given token signature using an RSA key
	/// </summary>
	/// <param name="token">The encrypted token</param>
	/// <param name="cancel">The cancellation token for the request</param>
	/// <returns>The un-encrypted token</returns>
	Task<string?> Decrypt(string token, CancellationToken cancel);
}

/// <inheritdoc cref="IJwtKeyService" />
internal class JwtKeyService(
	IConfiguration _configuration) : IJwtKeyService
{
	/// <summary>
	/// Create a lock to ensure we aren't check the RSA key file concurrently
	/// </summary>
	private readonly SemaphoreSlim _lock = new(1);

	/// <summary>
	/// A lazy cache for the RSA key pair to avoid reading it multiple times
	/// </summary>
	private AsymmetricCipherKeyPair? _keyPair;

	/// <summary>
	/// A lazy cache of the JWT key
	/// </summary>
	private string? _jwtKey;

	/// <summary>
	/// The algorithm used to sign the JWT tokens
	/// </summary>
	public Jose.JwsAlgorithm Algorithm => 
		Enum.TryParse<Jose.JwsAlgorithm>(_configuration["OAuth:JwtAlgorithm"], out var value) 
			? value 
			: Jose.JwsAlgorithm.RS256;

	/// <summary>
	/// The size of the RSA key to generate
	/// </summary>
	public int KeySize => int.TryParse(_configuration["OAuth:JwtKeySize"], out var value) ? value : 4096;

	/// <summary>
	/// Creates a new RSA key pair and saves it to the configured paths
	/// </summary>
	/// <param name="token">The cancellation token for the request</param>
	public async Task<string> CreateRSAKeyPair(CancellationToken token)
	{
		//Generate the RSA key
		using var rsa = new RSACryptoServiceProvider(KeySize);
		var parameters = rsa.ExportParameters(true);
		var pair = DotNetUtilities.GetRsaKeyPair(parameters);

		//Write the RSA key to the path
		using var io = new StringWriter();
		using var writer = new PemWriter(io);
		writer.WriteObject(pair);
		await io.FlushAsync(token);

		return _jwtKey ??= io.ToString();
	}

	/// <summary>
	/// Reads the given key parameter from a PEM file
	/// </summary>
	/// <param name="token">The cancellation token for the request</param>
	/// <returns>The RSA key parameter</returns>
	/// <exception cref="InvalidOperationException">Thrown if the parameter isn't valid</exception>
	public async Task<AsymmetricCipherKeyPair> ReadKey(CancellationToken token)
	{
		//Quick check to avoid needing to lock
		if (_keyPair is not null)
			return _keyPair;

		try
		{
			//Ensure we don't read the key concurrently
			await _lock.WaitAsync(token);
			//Follow up quick check to avoid needing to read the file
			if (_keyPair is not null)
				return _keyPair;

			var key = _jwtKey;

			//Ensure the key pair already exists
			if (string.IsNullOrWhiteSpace(key))
				key = await CreateRSAKeyPair(token);

			//Read the key from the PEM file
			using var reader = new StringReader(key);
			using var pemReader = new PemReader(reader);
			var keyPair = pemReader.ReadObject();
			if (keyPair is not AsymmetricCipherKeyPair parameter)
				throw new InvalidOperationException($"The key in the database is not a valid RSA key");
			//Set the key pair cache
			return _keyPair = parameter;
		}
		finally
		{
			_lock.Release();
		}
	}

	/// <summary>
	/// Gets the RSA key parameters from the configured paths
	/// </summary>
	/// <param name="publicKey">Whether or not to read the public or private key</param>
	/// <param name="token">The cancellation token for the request</param>
	/// <returns>The RSA parameters</returns>
	public async Task<RSAParameters> GetRSAParameter(bool publicKey, CancellationToken token)
	{
		var parameter = await ReadKey(token);
		if (publicKey)
		{
			var pubKey = parameter.Public as RsaKeyParameters;
			return DotNetUtilities.ToRSAParameters(pubKey);
		}

		var privateKey = parameter.Private as RsaPrivateCrtKeyParameters;
		return DotNetUtilities.ToRSAParameters(privateKey);
	}

	/// <inheritdoc />
	public async Task<string?> Decrypt(string token, CancellationToken cancel)
	{
		try
		{
			var parameters = await GetRSAParameter(true, cancel);
			using var rsa = new RSACryptoServiceProvider(KeySize);
			rsa.ImportParameters(parameters);
			return Jose.JWT.Decode(token, rsa, Algorithm);
		}
		catch (Jose.IntegrityException)
		{
			return null;
		}
	}

	/// <inheritdoc />
	public async Task<string> Encrypt(string token, CancellationToken cancel)
	{
		var parameters = await GetRSAParameter(false, cancel);
		using var rsa = new RSACryptoServiceProvider(KeySize);
		rsa.ImportParameters(parameters);
		return Jose.JWT.Encode(token, rsa, Algorithm);
	}
}
