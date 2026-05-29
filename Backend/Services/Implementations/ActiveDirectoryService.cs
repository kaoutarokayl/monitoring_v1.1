using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KtcWeb.Infrastructure.ExternalServices
{
    public class ActiveDirectoryService
    {
        private readonly string _domainName;
        private readonly IConfiguration _configuration;

        public ActiveDirectoryService(IConfiguration configuration)
        {
            _configuration = configuration;
            _domainName = _configuration["ActiveDirectorySettings:DomainName"] 
                          ?? throw new InvalidOperationException("ActiveDirectorySettings:DomainName n'est pas configuré dans appsettings.json");

            Console.WriteLine($"[AD Service] Domaine DNS configuré : {_domainName}");
        }

        /// <summary>
        /// Authentifie l'utilisateur contre Active Directory via le nom de domaine
        /// </summary>
   public bool Authenticate(string username, string password)
{
    try
    {
        Console.WriteLine($"[DEBUG Auth] Tentative pour '{username}' sur domaine '{_domainName}'");
        Console.WriteLine($"[DEBUG Auth] Username length={username.Length}, Password length={password.Length}");

        using (var context = new PrincipalContext(ContextType.Domain, _domainName))
        {
            Console.WriteLine($"[DEBUG Auth] PrincipalContext créé avec succès → domaine accessible");

            // Tenter avec Negotiate (Kerberos/NTLM)
            bool success = false;
            try
            {
                success = context.ValidateCredentials(username, password, ContextOptions.Negotiate);
                Console.WriteLine($"[DEBUG Auth] Negotiate → {success}");
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"[DEBUG Auth] Negotiate échoué : {ex1.Message}");
            }

            // Fallback SimpleBind (LDAP)
            if (!success)
            {
                try
                {
                    success = context.ValidateCredentials(username, password, ContextOptions.SimpleBind);
                    Console.WriteLine($"[DEBUG Auth] SimpleBind → {success}");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"[DEBUG Auth] SimpleBind échoué : {ex2.Message}");
                }
            }

            // Diagnostic supplémentaire si échec
            if (!success)
            {
                Console.WriteLine("[DEBUG Auth] ── Vérification du compte ──────────────────────────────");
                try
                {
                    // Tente de trouver l'utilisateur sans auth pour vérifier s'il existe
                    // (nécessite que le serveur soit accessible, ce qui est déjà prouvé)
                    using var readCtx = new PrincipalContext(ContextType.Domain, _domainName);
                    var userPrincipal = UserPrincipal.FindByIdentity(readCtx, IdentityType.SamAccountName, username);
                    if (userPrincipal == null)
                    {
                        Console.WriteLine($"[DEBUG Auth] ❌ COMPTE INTROUVABLE : '{username}' n'existe pas dans AD.");
                        Console.WriteLine("[DEBUG Auth]    → Vérifiez le SamAccountName dans ADUC (onglet Compte du user)");
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG Auth] ✅ Compte trouvé : SamAccountName='{userPrincipal.SamAccountName}'");
                        Console.WriteLine($"[DEBUG Auth]    DisplayName='{userPrincipal.DisplayName}'");
                        Console.WriteLine($"[DEBUG Auth]    Enabled={userPrincipal.Enabled}");
                        Console.WriteLine($"[DEBUG Auth]    AccountExpires={userPrincipal.AccountExpirationDate?.ToString() ?? "jamais"}");
                        Console.WriteLine($"[DEBUG Auth]    IsAccountLockedOut={userPrincipal.IsAccountLockedOut()}");
                        Console.WriteLine("[DEBUG Auth]    → Le compte existe mais le MOT DE PASSE est incorrect.");
                    }
                }
                catch (Exception diagEx)
                {
                    Console.WriteLine($"[DEBUG Auth] Impossible de lire le compte sans auth : {diagEx.Message}");
                    Console.WriteLine("[DEBUG Auth]    → Probable : mot de passe incorrect.");
                }
            }

            Console.WriteLine($"[DEBUG Auth] Résultat final : {success}");
            return success;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AD Auth ERROR] {ex.GetType().Name} : {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
        return false;
    }
}

/// <summary>
/// Récupère les rôles métier de l'utilisateur depuis Active Directory (version finale et robuste pour KTC Web)
/// </summary>
public List<string> GetRoles(string username, string password)
{
    var roles = new List<string>();

    try
    {
        Console.WriteLine($"[DEBUG GetRoles] === Début pour {username} ===");

        using (var context = new PrincipalContext(ContextType.Domain, _domainName, username, password))
        {
            var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            if (user == null)
            {
                Console.WriteLine($"[DEBUG] Utilisateur {username} non trouvé");
                roles.Add("Utilisateur");
                return roles;
            }

            Console.WriteLine($"[DEBUG] Utilisateur trouvé : {user.SamAccountName}");

            var groups = user.GetAuthorizationGroups();

            foreach (var group in groups)
            {
                if (group?.Name == null) continue;

                string groupName = group.Name.Trim();

                Console.WriteLine($"[DEBUG GetRoles] Groupe détecté : '{groupName}'");

                // On accepte TOUS les groupes sauf les plus basiques
                if (!IsHighlyDefaultGroup(groupName))
                {
                    roles.Add(groupName);
                    Console.WriteLine($"[DEBUG GetRoles] → RÔLE AJOUTÉ : {groupName}");
                }
            }
        }

        if (roles.Count == 0)
        {
            roles.Add("Utilisateur");
            Console.WriteLine("[DEBUG] Aucun rôle métier → fallback 'Utilisateur'");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AD GetRoles ERROR] {ex.Message}");
    }

    Console.WriteLine($"[DEBUG GetRoles] RÔLES FINAUX : [{string.Join(", ", roles)}]");
    return roles;
}

private bool IsHighlyDefaultGroup(string groupName)
{
    var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Utilisateurs du domaine", "Domain Users", "Utilisateurs", "Users",
        "Everyone", "Authenticated Users", "Guests", "Invités"
    };

    return defaults.Contains(groupName);
} 

        /// <summary>
        /// Génère le JWT Token avec les rôles
        /// </summary>
        public string GenerateJwtToken(string username, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:SecretKey"] ?? "KtcWebSecretKey2026SuperLongAndSecure!@#"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "KtcWeb",
                audience: _configuration["Jwt:Audience"] ?? "KtcWeb",
                claims: claims,
                expires: DateTime.Now.AddHours(_configuration.GetValue<int>("Jwt:ExpirationInHours", 2)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

