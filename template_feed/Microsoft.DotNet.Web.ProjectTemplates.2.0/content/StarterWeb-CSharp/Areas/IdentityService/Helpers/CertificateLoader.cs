using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Company.WebApplication1.Identity
{
    public class CertificateLoader
    {
        private const string File = "File";
        private const string Store = "Store";

        public static IEnumerable<X509Certificate2> LoadCertificates(IConfiguration certificates)
        {
            return LoadCertificates().ToList();
            IEnumerable<X509Certificate2> LoadCertificates()
            {
                var certificateSources = certificates.GetChildren();
                foreach (var certificateSource in certificateSources)
                {
                    var sourceKind = certificateSource.GetValue<string>("Source");
                    switch (sourceKind)
                    {
                        case File:
                            var fileSource = new CertificateFileSource();
                            certificateSource.Bind(fileSource);
                            yield return LoadFromFile(fileSource);
                            break;
                        case Store:
                            var storeSource = new CertificateStoreSource();
                            certificateSource.Bind(storeSource);
                            var certificatesFromStore = LoadFromStore(storeSource);
                            foreach (var loadedCertificate in certificatesFromStore)
                            {
                                yield return loadedCertificate;
                            }
                            break;
                    }
                }

                X509Certificate2 LoadFromFile(CertificateFileSource source)
                {
                    var certificate = TryLoad(X509KeyStorageFlags.DefaultKeySet, out var error) ??
                        TryLoad(X509KeyStorageFlags.UserKeySet, out error) ??
                        TryLoad(X509KeyStorageFlags.EphemeralKeySet, out error);

                    if (error != null)
                    {
                        throw error;
                    }

                    return certificate;

                    X509Certificate2 TryLoad(X509KeyStorageFlags flags, out Exception exception)
                    {
                        try
                        {
                            var loadedCertificate = new X509Certificate2(source.Path, source.Password);
                            exception = null;
                            return loadedCertificate;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                            return null;
                        }
                    }
                }

                IEnumerable<X509Certificate2> LoadFromStore(CertificateStoreSource source)
                {
                    if (!Enum.TryParse(source.Location, true, out StoreLocation location))
                    {
                        throw new InvalidOperationException($"Invalid store location: {source.Location}");
                    }

                    using (var store = new X509Store(source.Name, location))
                    {
                        store.Open(OpenFlags.ReadOnly);
                        var foundCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, source.Subject, validOnly: false)
                            .OfType<X509Certificate2>()
                            .ToList();
                        store.Close();

                        return foundCertificates;
                    }
                }
            }
        }

        private abstract class CertificateSource
        {
            public string Source { get; set; }
        }

        private class CertificateFileSource : CertificateSource
        {
            public string Path { get; set; }
            public string Password { get; set; }
        }

        private class CertificateStoreSource : CertificateSource
        {
            public string Location { get; set; }
            public string Name { get; set; }
            public string Subject { get; set; }
        }
    }
}
