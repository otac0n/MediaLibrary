// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Hosting
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Authentication.Negotiate;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    public static class Startup
    {
        public static void Build(IWebHostBuilder builder, string baseUri, MediaIndex index)
        {
            builder.UseUrls(baseUri);
            builder
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services
                        .AddSingleton(index)
                        .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                        .AddAuthorization(options =>
                        {
                            options.FallbackPolicy = options.DefaultPolicy;
                        })
                        .AddNegotiate()
                        .AddResponseCompression();
                })
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ConfigureHttpsDefaults(listenOptions =>
                    {
                        listenOptions.ServerCertificate = GetOrAddSelfSigned("cn=MediaLibrary", StoreName.My, StoreLocation.CurrentUser);
                    });
                })
                .Configure(app =>
                {
                    app
                        .UseAuthentication()
                        .UseAuthorization()
                        .UseResponseCompression()
                        .UseRouting()
                        .UseEndpoints(endpoints =>
                            endpoints
                                .MapControllers());
                });
        }

        private static X509Certificate2 GetOrAddSelfSigned(string subjectName, StoreName storeName, StoreLocation storeLocation)
        {
            var now = DateTimeOffset.Now;
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                var cert = (from X509Certificate2 c in store.Certificates
                            where c.Subject.Equals(subjectName, StringComparison.OrdinalIgnoreCase)
                            where c.NotBefore <= now && now < c.NotAfter
                            select c).FirstOrDefault();
                if (cert == null)
                {
                    using (var key = RSA.Create(2048))
                    {
                        var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                        request.CertificateExtensions.Add(
                            new X509KeyUsageExtension(
                                X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature,
                                critical: false));

                        request.CertificateExtensions.Add(
                            new X509EnhancedKeyUsageExtension(
                                new OidCollection
                                {
                                    new Oid("1.3.6.1.5.5.7.3.1"),
                                },
                                critical: false));

                        var san = new SubjectAlternativeNameBuilder();
                        san.AddIpAddress(IPAddress.Loopback);
                        san.AddIpAddress(IPAddress.IPv6Loopback);
                        san.AddDnsName("localhost");
                        san.AddDnsName(Environment.MachineName);
                        request.CertificateExtensions.Add(san.Build());

                        var password = Guid.NewGuid().ToString();
                        cert = request.CreateSelfSigned(now.AddMinutes(-4), now.AddYears(1));
                        cert = new X509Certificate2(cert.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet);

                        store.Add(cert);
                    }
                }

                return cert;
            }
        }
    }
}
