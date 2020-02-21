using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace TeamsGraphChangeNotification
{
    public class TokenValidator
    {
        private static readonly string issuerPrefix = "https://sts.windows.net/";
        private static readonly string wellKnownUri = "https://login.microsoftonline.com/common/.well-known/openid-configuration";
        private readonly string _tenantId;
        private readonly IEnumerable<string> _appIds;
        private string issuerToValidate
        {
            get
            {
                return $"{issuerPrefix}{_tenantId}/";
            }
        }
        public TokenValidator(string tenantId, IEnumerable<string> appIds)
        {
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
            _appIds = appIds ?? throw new ArgumentNullException(nameof(appIds));
        }
        public async Task<bool> ValidateToken(string token)
        {
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(wellKnownUri, new OpenIdConnectConfigurationRetriever());
            var openIdConfig = await configurationManager.GetConfigurationAsync();
            var handler = new JwtSecurityTokenHandler();
            try
            {
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuerToValidate,
                    ValidAudiences = _appIds,
                    IssuerSigningKeys = openIdConfig.SigningKeys
                }, out _);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{ex.Message}:{ex.StackTrace}");
                return false;
            }
        }
    }
}
