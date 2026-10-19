using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Services;

public sealed class WeChatOptions
{
    public string MiniProgramAppId { get; set; } = string.Empty;
    public string MiniProgramSecret { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantSerialNumber { get; set; } = string.Empty;
    public string ApiV3Key { get; set; } = string.Empty;
    public string MerchantPrivateKeyPem { get; set; } = string.Empty;
    public string MerchantPrivateKeyPath { get; set; } = string.Empty;
    public string PlatformCertificatePem { get; set; } = string.Empty;
    public string PlatformCertificatePath { get; set; } = string.Empty;
    public string NotifyBaseUrl { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.mch.weixin.qq.com";
}

public sealed record WeChatConfigurationStatus(
    bool MiniProgramConfigured,
    bool PaymentConfigured,
    string AppId,
    string MerchantId,
    string MerchantSerialNumber,
    string NotifyUrl,
    bool HasApiV3Key,
    bool HasMerchantPrivateKey,
    bool HasPlatformCertificate,
    IReadOnlyList<string> Missing);

public sealed record WeChatJsapiPayment(
    string AppId,
    string TimeStamp,
    string NonceStr,
    string Package,
    string SignType,
    string PaySign);

public sealed record WeChatPaymentNotification(
    string OutTradeNumber,
    string TransactionId,
    string TradeState,
    int TotalAmountCents,
    DateTimeOffset? SuccessTime);

public sealed record WeChatCode2Session(
    string OpenId,
    string? UnionId,
    string? SessionKey);

public sealed class WeChatPayService(
    HttpClient httpClient,
    IOptions<WeChatOptions> options,
    ILogger<WeChatPayService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim AccessTokenLock = new(1, 1);
    private static string? cachedMiniProgramAccessToken;
    private static DateTimeOffset cachedMiniProgramAccessTokenExpiresAt;
    private readonly WeChatOptions _options = options.Value;

    public bool IsMiniProgramConfigured =>
        !string.IsNullOrWhiteSpace(_options.MiniProgramAppId) &&
        !string.IsNullOrWhiteSpace(_options.MiniProgramSecret);

    public bool IsPaymentConfigured =>
        !string.IsNullOrWhiteSpace(_options.MiniProgramAppId) &&
        !string.IsNullOrWhiteSpace(_options.MerchantId) &&
        !string.IsNullOrWhiteSpace(_options.MerchantSerialNumber) &&
        Encoding.UTF8.GetByteCount(_options.ApiV3Key) == 32 &&
        HasMerchantPrivateKey;

    public WeChatConfigurationStatus GetConfigurationStatus()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.MiniProgramAppId)) missing.Add("WeChat:MiniProgramAppId");
        if (string.IsNullOrWhiteSpace(_options.MiniProgramSecret)) missing.Add("WeChat:MiniProgramSecret");
        if (string.IsNullOrWhiteSpace(_options.MerchantId)) missing.Add("WeChat:MerchantId");
        if (string.IsNullOrWhiteSpace(_options.MerchantSerialNumber)) missing.Add("WeChat:MerchantSerialNumber");
        if (string.IsNullOrWhiteSpace(_options.ApiV3Key) || Encoding.UTF8.GetByteCount(_options.ApiV3Key) != 32)
        {
            missing.Add("WeChat:ApiV3Key");
        }
        if (!HasMerchantPrivateKey) missing.Add("WeChat:MerchantPrivateKeyPem/Path");
        if (!HasPlatformCertificate) missing.Add("WeChat:PlatformCertificatePem/Path");
        if (!Uri.TryCreate(_options.NotifyBaseUrl, UriKind.Absolute, out _))
        {
            missing.Add("WeChat:NotifyBaseUrl");
        }

        return new WeChatConfigurationStatus(
            IsMiniProgramConfigured,
            IsPaymentConfigured &&
            HasPlatformCertificate &&
            Uri.TryCreate(_options.NotifyBaseUrl, UriKind.Absolute, out _),
            _options.MiniProgramAppId,
            _options.MerchantId,
            _options.MerchantSerialNumber,
            BuildNotifyUrl(),
            Encoding.UTF8.GetByteCount(_options.ApiV3Key) == 32,
            HasMerchantPrivateKey,
            HasPlatformCertificate,
            missing);
    }

    private bool HasMerchantPrivateKey
    {
        get
        {
            if (!TryLoadMerchantPrivateKey(out var rsa))
            {
                return false;
            }
            rsa!.Dispose();
            return true;
        }
    }

    public bool HasPlatformCertificate =>
        !string.IsNullOrWhiteSpace(_options.PlatformCertificatePem) ||
        (!string.IsNullOrWhiteSpace(_options.PlatformCertificatePath) &&
         File.Exists(_options.PlatformCertificatePath));

    public async Task<WeChatCode2Session> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        if (!IsMiniProgramConfigured)
        {
            throw new InvalidOperationException("微信小程序 AppID 或 Secret 尚未配置");
        }

        var query = new Dictionary<string, string>
        {
            ["appid"] = _options.MiniProgramAppId,
            ["secret"] = _options.MiniProgramSecret,
            ["js_code"] = code,
            ["grant_type"] = "authorization_code"
        };
        var queryString = string.Join(
            "&",
            query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        var url = $"https://api.weixin.qq.com/sns/jscode2session?{queryString}";
        var payload = await httpClient.GetFromJsonAsync<Code2SessionResponse>(url, cancellationToken);
        if (payload is null || payload.ErrCode != 0 || string.IsNullOrWhiteSpace(payload.OpenId))
        {
            throw new InvalidOperationException(payload?.ErrMsg ?? "微信登录换取 openid 失败");
        }

        return new WeChatCode2Session(payload.OpenId, payload.UnionId, payload.SessionKey);
    }

    public async Task<string> ExchangePhoneNumberAsync(
        string phoneCode,
        CancellationToken cancellationToken)
    {
        if (!IsMiniProgramConfigured)
        {
            throw new InvalidOperationException("微信小程序 AppID 或 Secret 尚未配置");
        }
        if (string.IsNullOrWhiteSpace(phoneCode))
        {
            throw new InvalidOperationException("手机号授权凭证不能为空");
        }

        var accessToken = await GetMiniProgramAccessTokenAsync(cancellationToken);
        var url = $"https://api.weixin.qq.com/wxa/business/getuserphonenumber?access_token={Uri.EscapeDataString(accessToken)}";
        var requestBody = JsonSerializer.SerializeToUtf8Bytes(
            new { code = phoneCode.Trim() },
            JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ByteArrayContent(requestBody)
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        request.Content.Headers.ContentLength = requestBody.Length;

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        PhoneNumberResponse? payload = null;
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                payload = JsonSerializer.Deserialize<PhoneNumberResponse>(responseBody, JsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(
                    ex,
                    "微信手机号接口返回无法解析的响应 (HTTP {StatusCode})",
                    (int)response.StatusCode);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "微信手机号接口返回 HTTP {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode,
                response.ReasonPhrase);
        }

        if (!response.IsSuccessStatusCode ||
            payload is null ||
            payload.ErrCode != 0 ||
            string.IsNullOrWhiteSpace(payload.PhoneInfo?.PurePhoneNumber))
        {
            var message = !string.IsNullOrWhiteSpace(payload?.ErrMsg)
                ? payload.ErrMsg
                : !response.IsSuccessStatusCode
                    ? $"微信手机号接口返回 HTTP {(int)response.StatusCode} {response.ReasonPhrase}，响应体为空或无法识别"
                    : payload is null
                        ? "微信手机号接口返回了无法解析的响应"
                        : "微信未返回手机号";
            logger.LogWarning("微信手机号授权失败: {Message}", message);
            throw new InvalidOperationException(message);
        }

        return payload.PhoneInfo.PurePhoneNumber!;
    }

    private async Task<string> GetMiniProgramAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(cachedMiniProgramAccessToken) &&
            cachedMiniProgramAccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return cachedMiniProgramAccessToken;
        }

        await AccessTokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(cachedMiniProgramAccessToken) &&
                cachedMiniProgramAccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return cachedMiniProgramAccessToken;
            }

            var query = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credential",
                ["appid"] = _options.MiniProgramAppId,
                ["secret"] = _options.MiniProgramSecret
            };
            var queryString = string.Join(
                "&",
                query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
            var url = $"https://api.weixin.qq.com/cgi-bin/token?{queryString}";
            var payload = await httpClient.GetFromJsonAsync<AccessTokenResponse>(url, JsonOptions, cancellationToken);
            if (payload is null || payload.ErrCode != 0 || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                throw new InvalidOperationException(payload?.ErrMsg ?? "微信 access_token 获取失败");
            }

            cachedMiniProgramAccessToken = payload.AccessToken;
            cachedMiniProgramAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(
                Math.Max(60, payload.ExpiresIn - 120));
            return payload.AccessToken;
        }
        finally
        {
            AccessTokenLock.Release();
        }
    }

    public async Task<WeChatJsapiPayment> CreateJsapiPaymentAsync(
        BookingOrder order,
        string openId,
        CancellationToken cancellationToken)
    {
        EnsurePaymentConfigured();
        if (string.IsNullOrWhiteSpace(openId))
        {
            throw new InvalidOperationException("缺少微信用户 openid");
        }

        var requestBody = new
        {
            appid = _options.MiniProgramAppId,
            mchid = _options.MerchantId,
            description = $"民宿预订-{order.OrderNumber}",
            out_trade_no = order.OrderNumber,
            notify_url = BuildNotifyUrl(),
            amount = new
            {
                total = order.TotalAmountCents,
                currency = order.Currency
            },
            payer = new { openid = openId }
        };
        var payload = await SendAsync<JsapiPrepayResponse>(
            HttpMethod.Post,
            "/v3/pay/transactions/jsapi",
            requestBody,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(payload?.PrepayId))
        {
            throw new InvalidOperationException("微信支付未返回 prepay_id");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var package = $"prepay_id={payload.PrepayId}";
        var paySign = Sign($"{_options.MiniProgramAppId}\n{timestamp}\n{nonce}\n{package}\n");
        return new WeChatJsapiPayment(
            _options.MiniProgramAppId,
            timestamp,
            nonce,
            package,
            "RSA",
            paySign);
    }

    public async Task<string> CreateRefundAsync(
        BookingOrder order,
        RefundRecord refund,
        CancellationToken cancellationToken)
    {
        EnsurePaymentConfigured();
        var requestBody = new
        {
            out_trade_no = order.OrderNumber,
            out_refund_no = refund.RefundNumber,
            reason = refund.Reason,
            amount = new
            {
                refund = refund.AmountCents,
                total = order.TotalAmountCents,
                currency = order.Currency
            }
        };
        var payload = await SendAsync<RefundResponse>(
            HttpMethod.Post,
            "/v3/refund/domestic/refunds",
            requestBody,
            cancellationToken);
        return string.IsNullOrWhiteSpace(payload?.RefundId)
            ? refund.RefundNumber
            : payload.RefundId;
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        EnsurePaymentConfigured();
        _ = await SendAsync<JsonElement>(
            HttpMethod.Get,
            "/v3/certificates",
            null,
            cancellationToken);
    }

    public WeChatPaymentNotification VerifyAndDecryptNotification(
        IReadOnlyDictionary<string, string?> headers,
        string body)
    {
        if (!headers.TryGetValue("Wechatpay-Timestamp", out var timestamp) ||
            !headers.TryGetValue("Wechatpay-Nonce", out var nonce) ||
            !headers.TryGetValue("Wechatpay-Signature", out var signature) ||
            !headers.TryGetValue("Wechatpay-Serial", out var serial) ||
            string.IsNullOrWhiteSpace(timestamp) ||
            string.IsNullOrWhiteSpace(nonce) ||
            string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(serial))
        {
            throw new CryptographicException("微信支付回调签名头缺失");
        }

        if (!long.TryParse(timestamp, out var timestampValue) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestampValue) > 300)
        {
            throw new CryptographicException("微信支付回调时间戳已失效");
        }

        try
        {
            using var certificate = LoadPlatformCertificate();
            if (!string.Equals(
                    NormalizeSerial(serial),
                    NormalizeSerial(certificate.SerialNumber),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new CryptographicException("微信支付平台证书序列号不匹配");
            }

            var message = $"{timestamp}\n{nonce}\n{body}\n";
            using var rsa = certificate.GetRSAPublicKey()
                ?? throw new CryptographicException("微信支付平台证书不包含 RSA 公钥");
            if (!rsa.VerifyData(
                    Encoding.UTF8.GetBytes(message),
                    Convert.FromBase64String(signature),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1))
            {
                throw new CryptographicException("微信支付回调签名校验失败");
            }

            var envelope = JsonSerializer.Deserialize<NotificationEnvelope>(body, JsonOptions)
                ?? throw new CryptographicException("微信支付回调内容为空");
            if (envelope.Resource is null)
            {
                throw new CryptographicException("微信支付回调资源缺失");
            }

            var plainText = DecryptResource(envelope.Resource);
            var notification = JsonSerializer.Deserialize<PaymentNotificationPayload>(plainText, JsonOptions)
                ?? throw new CryptographicException("微信支付回调资源无法解析");
            if (string.IsNullOrWhiteSpace(notification.OutTradeNo) ||
                string.IsNullOrWhiteSpace(notification.TransactionId))
            {
                throw new CryptographicException("微信支付回调缺少订单号或微信交易号");
            }

            return new WeChatPaymentNotification(
                notification.OutTradeNo,
                notification.TransactionId,
                notification.TradeState ?? string.Empty,
                notification.Amount?.Total ?? 0,
                notification.SuccessTime);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("微信支付回调签名或密文格式不正确", ex);
        }
        catch (JsonException ex)
        {
            throw new CryptographicException("微信支付回调内容无法解析", ex);
        }
    }

    private async Task<T?> SendAsync<T>(
        HttpMethod method,
        string path,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        EnsurePaymentConfigured();
        var body = requestBody is null
            ? string.Empty
            : JsonSerializer.Serialize(requestBody, JsonOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var signature = Sign($"{method.Method}\n{path}\n{timestamp}\n{nonce}\n{body}\n");
        using var request = new HttpRequestMessage(method, new Uri(new Uri(GetApiBaseUrl()), path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"WECHATPAY2-SHA256-RSA2048 mchid=\"{_options.MerchantId}\"," +
            $"nonce_str=\"{nonce}\",timestamp=\"{timestamp}\"," +
            $"serial_no=\"{_options.MerchantSerialNumber}\",signature=\"{signature}\"");
        if (requestBody is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "微信支付接口调用失败，Path={Path}, StatusCode={StatusCode}, Body={Body}",
                path,
                (int)response.StatusCode,
                responseBody.Length > 500 ? responseBody[..500] : responseBody);
            throw new HttpRequestException($"微信支付接口返回 {(int)response.StatusCode}");
        }

        if (typeof(T) == typeof(JsonElement))
        {
            return (T?)(object?)JsonSerializer.Deserialize<JsonElement>(responseBody, JsonOptions);
        }
        return string.IsNullOrWhiteSpace(responseBody)
            ? default
            : JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
    }

    private string Sign(string message)
    {
        using var rsa = LoadMerchantPrivateKey();
        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(message),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    private string DecryptResource(NotificationResource resource)
    {
        if (Encoding.UTF8.GetByteCount(_options.ApiV3Key) != 32)
        {
            throw new CryptographicException("微信支付 API v3 Key 必须为 32 字节");
        }

        var nonce = Encoding.UTF8.GetBytes(resource.Nonce ?? string.Empty);
        var associatedData = Encoding.UTF8.GetBytes(resource.AssociatedData ?? string.Empty);
        var encrypted = Convert.FromBase64String(resource.Ciphertext ?? string.Empty);
        if (nonce.Length != 12 || encrypted.Length <= 16)
        {
            throw new CryptographicException("微信支付回调加密资源格式不正确");
        }

        var ciphertext = encrypted[..^16];
        var tag = encrypted[^16..];
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(Encoding.UTF8.GetBytes(_options.ApiV3Key), 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        return Encoding.UTF8.GetString(plaintext);
    }

    private X509Certificate2 LoadPlatformCertificate()
    {
        if (!string.IsNullOrWhiteSpace(_options.PlatformCertificatePem))
        {
            return X509Certificate2.CreateFromPem(_options.PlatformCertificatePem);
        }
        if (!string.IsNullOrWhiteSpace(_options.PlatformCertificatePath) &&
            File.Exists(_options.PlatformCertificatePath))
        {
            var certificateFile = File.ReadAllText(_options.PlatformCertificatePath);
            return certificateFile.Contains("BEGIN CERTIFICATE", StringComparison.Ordinal)
                ? X509Certificate2.CreateFromPem(certificateFile)
                : X509CertificateLoader.LoadCertificateFromFile(_options.PlatformCertificatePath);
        }
        throw new InvalidOperationException("微信支付平台证书尚未配置");
    }

    private RSA LoadMerchantPrivateKey()
    {
        if (!TryLoadMerchantPrivateKey(out var rsa) || rsa is null)
        {
            throw new InvalidOperationException("微信支付商户私钥尚未配置");
        }
        return rsa;
    }

    private bool TryLoadMerchantPrivateKey(out RSA? rsa)
    {
        rsa = null;
        try
        {
            var pem = _options.MerchantPrivateKeyPem;
            if (string.IsNullOrWhiteSpace(pem) &&
                !string.IsNullOrWhiteSpace(_options.MerchantPrivateKeyPath) &&
                File.Exists(_options.MerchantPrivateKeyPath))
            {
                pem = File.ReadAllText(_options.MerchantPrivateKeyPath);
            }
            if (string.IsNullOrWhiteSpace(pem))
            {
                return false;
            }

            rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        {
            rsa?.Dispose();
            rsa = null;
            return false;
        }
    }

    private void EnsurePaymentConfigured()
    {
        if (!IsPaymentConfigured ||
            !Uri.TryCreate(_options.NotifyBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("微信支付配置不完整，请先配置商户号、证书序列号、API v3 Key、商户私钥和回调地址");
        }
    }

    private string GetApiBaseUrl()
    {
        return string.IsNullOrWhiteSpace(_options.ApiBaseUrl)
            ? "https://api.mch.weixin.qq.com"
            : _options.ApiBaseUrl.TrimEnd('/');
    }

    private string BuildNotifyUrl()
    {
        var configuredUrl = _options.NotifyBaseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri))
        {
            return string.Empty;
        }

        var path = baseUri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/api/v1/payments/wechat/notify", StringComparison.OrdinalIgnoreCase))
        {
            return baseUri.ToString();
        }
        if (path.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            return new UriBuilder(baseUri)
            {
                Path = $"{path}/payments/wechat/notify"
            }.Uri.ToString();
        }

        return new UriBuilder(baseUri)
        {
            Path = $"{path}/api/v1/payments/wechat/notify"
        }.Uri.ToString();
    }

    private static string NormalizeSerial(string value) =>
        value.Replace(" ", string.Empty, StringComparison.Ordinal).TrimStart('0').ToUpperInvariant() switch
        {
            "" => "0",
            var normalized => normalized
        };

    private sealed class Code2SessionResponse
    {
        [JsonPropertyName("errcode")]
        public int ErrCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }

        [JsonPropertyName("openid")]
        public string? OpenId { get; set; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; set; }

        [JsonPropertyName("session_key")]
        public string? SessionKey { get; set; }
    }

    private sealed class AccessTokenResponse
    {
        [JsonPropertyName("errcode")]
        public int ErrCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class PhoneNumberResponse
    {
        [JsonPropertyName("errcode")]
        public int ErrCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrMsg { get; set; }

        [JsonPropertyName("phone_info")]
        public PhoneInfo? PhoneInfo { get; set; }
    }

    private sealed class PhoneInfo
    {
        [JsonPropertyName("purePhoneNumber")]
        public string? PurePhoneNumber { get; set; }
    }

    private sealed class JsapiPrepayResponse
    {
        [JsonPropertyName("prepay_id")]
        public string? PrepayId { get; set; }
    }

    private sealed class RefundResponse
    {
        [JsonPropertyName("refund_id")]
        public string? RefundId { get; set; }
    }

    private sealed class NotificationEnvelope
    {
        [JsonPropertyName("resource")]
        public NotificationResource? Resource { get; set; }
    }

    private sealed class NotificationResource
    {
        [JsonPropertyName("ciphertext")]
        public string? Ciphertext { get; set; }

        [JsonPropertyName("associated_data")]
        public string? AssociatedData { get; set; }

        [JsonPropertyName("nonce")]
        public string? Nonce { get; set; }
    }

    private sealed class PaymentNotificationPayload
    {
        [JsonPropertyName("out_trade_no")]
        public string? OutTradeNo { get; set; }

        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("trade_state")]
        public string? TradeState { get; set; }

        [JsonPropertyName("amount")]
        public PaymentAmount? Amount { get; set; }

        [JsonPropertyName("success_time")]
        public DateTimeOffset? SuccessTime { get; set; }
    }

    private sealed class PaymentAmount
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
