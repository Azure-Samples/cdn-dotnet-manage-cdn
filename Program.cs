// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Cdn.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace ManageCdn
{
    public class Program
    {
        private static readonly string Suffix = ".azurewebsites.net";

        /**
         * Azure CDN sample for managing CDN profiles:
         * - Create 8 web apps in 8 regions:
         *    * 2 in US
         *    * 2 in EU
         *    * 2 in Southeast
         *    * 1 in Brazil
         *    * 1 in Japan
         * - Create CDN profile using Standard Verizon SKU with endpoints in each region of Web apps.
         * - Load some content (referenced by Web Apps) to the CDN endpoints.
         */
        public static async Task RunSample(ArmClient client)
        {
            // Get default subscription
            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

            // Create a resource group in the EastUS region
            string rgName = Utilities.CreateRandomName("CdnRG");
            ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
            ResourceGroupResource resourceGroup = rgLro.Value;
            Utilities.Log($"created resource group:{resourceGroup.Data.Name}");

            try
            {
                // ============================================================
                // Create 8 websites
                List<WebSiteResource> websites = new List<WebSiteResource>();

                // 2 in US
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.EastUS));
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.WestUS));

                //// 2 in EU
                //websites.Add(await CreateWebApp(resourceGroup, AzureLocation.NorthEurope));
                //websites.Add(await CreateWebApp(resourceGroup, AzureLocation.WestEurope));

                //// 2 in Southeast
                //await CreateWebApp(resourceGroup, AzureLocation.EastAsia);
                //await CreateWebApp(resourceGroup, AzureLocation.SoutheastAsia);

                //// 1 in Brazil
                //await CreateWebApp(resourceGroup, AzureLocation.BrazilSouth);

                //// 1 in Japan
                //await CreateWebApp(resourceGroup, AzureLocation.JapanWest);

                // =======================================================================================
                // Create CDN profile using Standard Verizon SKU with endpoints in each region of Web apps.
                Utilities.Log("Creating a CDN Profile");
                string afdProfileName = Utilities.CreateRandomName("AFDProfile");
                ProfileData afdProfileInput = new ProfileData("Global", new CdnSku { Name = CdnSkuName.PremiumAzureFrontDoor })
                {
                    OriginResponseTimeoutSeconds = 60
                };
                var afdProfileLro = await resourceGroup.GetProfiles().CreateOrUpdateAsync(WaitUntil.Completed, afdProfileName, afdProfileInput);
                ProfileResource afdProfile = afdProfileLro.Value;


                // CreateAfdEndpoint
                Utilities.Log($"Creating a FrontDoor endpoint..");
                string afdEndpointName = Utilities.CreateRandomName("afdtestendpoint");
                FrontDoorEndpointData input = new FrontDoorEndpointData(AzureLocation.WestUS);
                var afdEndpointLro = await afdProfile.GetFrontDoorEndpoints().CreateOrUpdateAsync(WaitUntil.Completed, afdEndpointName, input);
                FrontDoorEndpointResource afdEndpointInstance = afdEndpointLro.Value;

                // CreateAfdOriginGroup
                Utilities.Log($"Creating a origin group..");
                string afdOriginGroupName = Utilities.CreateRandomName("AfdOriginGroup");
                FrontDoorOriginGroupData afdOriginGroupInput = new FrontDoorOriginGroupData
                {
                    HealthProbeSettings = new HealthProbeSettings
                    {
                        ProbePath = "/healthz",
                        ProbeRequestType = HealthProbeRequestType.Head,
                        ProbeProtocol = HealthProbeProtocol.Https,
                        ProbeIntervalInSeconds = 60
                    },
                    LoadBalancingSettings = new LoadBalancingSettings
                    {
                        SampleSize = 5,
                        SuccessfulSamplesRequired = 4,
                        AdditionalLatencyInMilliseconds = 200
                    }
                };
                var afdOriginGroupLro = await afdProfile.GetFrontDoorOriginGroups().CreateOrUpdateAsync(WaitUntil.Completed, afdOriginGroupName, afdOriginGroupInput);
                FrontDoorOriginGroupResource afdOriginGroup = afdOriginGroupLro.Value;

                foreach (var website in websites)
                {
                    // origin
                    Utilities.Log($"Creating an origin for {website.Data.Location}-{website.Data.Name}");
                    string afdOriginName = Utilities.CreateRandomName("AfdOrigin");
                    FrontDoorOriginData afdOriginInput = new FrontDoorOriginData
                    {
                        HostName = website.Data.DefaultHostName,
                        Priority = 1,
                        Weight = 1000
                    };
                    var afdOriginLro = await afdOriginGroup.GetFrontDoorOrigins().CreateOrUpdateAsync(WaitUntil.Completed, afdOriginName, afdOriginInput);
                    FrontDoorOriginResource afdOrigin = afdOriginLro.Value;
                }


                //CreateAfdRuleSet
                Utilities.Log($"Creating a rule set");
                string afdRuleSetName = Utilities.CreateRandomName("AfdRuleSet");
                var afdRuleSetLro = await afdProfile.GetFrontDoorRuleSets().CreateOrUpdateAsync(WaitUntil.Completed, afdRuleSetName);
                FrontDoorRuleSetResource afdRuleSet = afdRuleSetLro.Value;

                //CreateAfdRoute
                Utilities.Log($"Creating a route");
                string afdRouteName = Utilities.CreateRandomName("AfdRoute");
                FrontDoorRouteData afdRouteDataInput = new FrontDoorRouteData
                {
                    OriginGroupId = afdOriginGroup.Id,
                    LinkToDefaultDomain = LinkToDefaultDomain.Enabled,
                    EnabledState = EnabledState.Enabled,
                    RuleSets =
                        {
                            new WritableSubResource()
                            {
                                Id = afdRuleSet.Id
                            }
                        },
                    PatternsToMatch = { "/*" }
                };
                var afdRouteLro = await afdEndpointInstance.GetFrontDoorRoutes().CreateOrUpdateAsync(WaitUntil.Completed, afdRouteName, afdRouteDataInput);
                FrontDoorRouteResource afdRoute = afdRouteLro.Value;



                //// =======================================================================================
                //// Create CDN profile using Standard Verizon SKU with endpoints in each region of Web apps.
                //Utilities.Log("Creating a CDN Profile");

                //// create Cdn Profile definition object that will let us do a for loop
                //// to define all 8 endpoints and then parallelize their creation
                //var profileDefinition = azure.CdnProfiles.Define(cdnProfileName)
                //        .WithRegion(Region.USSouthCentral)
                //        .WithExistingResourceGroup(rgName)
                //        .WithStandardVerizonSku();

                //// define all the endpoints. We need to keep track of the last creatable stage
                //// to be able to call create on the entire Cdn profile deployment definition.
                //ICreatable<ICdnProfile> cdnCreatable = null;
                //foreach (var webSite in appNames)
                //{
                //    cdnCreatable = profileDefinition
                //            .DefineNewEndpoint()
                //                .WithOrigin(webSite + Suffix)
                //                .WithHostHeader(webSite + Suffix)
                //                .WithCompressionEnabled(true)
                //                .WithContentTypeToCompress("application/javascript")
                //                .WithQueryStringCachingBehavior(QueryStringCachingBehavior.IgnoreQueryString)
                //            .Attach();
                //}

                //// create profile and then all the defined endpoints in parallel
                //ICdnProfile profile = cdnCreatable.Create();

                //// =======================================================================================
                //// Load some content (referenced by Web Apps) to the CDN endpoints.
                //var contentToLoad = new HashSet<string>();
                //contentToLoad.Add("/server.js");
                //contentToLoad.Add("/pictures/microsoft_logo.png");

                //foreach (ICdnEndpoint endpoint in profile.Endpoints.Values)
                //{
                //    endpoint.LoadContent(contentToLoad);
                //}

            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rgName);
                    await resourceGroup.DeleteAsync(WaitUntil.Completed);
                    Utilities.Log("Deleted Resource Group: " + rgName);
                }
                catch
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                //var credentials = SdkContext.AzureCredentialsFactory.FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                // Print selected subscription
                Utilities.Log("Selected subscription: " + subscription);

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }

        private static async Task<WebSiteResource> CreateWebApp(ResourceGroupResource resourceGroup, AzureLocation location)
        {
            string appNamePrefix = "sampletestwebapp";
            var appName = Utilities.CreateRandomName(appNamePrefix);

            Utilities.Log($"Creating {location} web app {appName} with master branch...");

            WebSiteCollection collection = resourceGroup.GetWebSites();
            var data = new WebSiteData(location)
            {
                /*Reserved = false,
                IsXenon = false,
                HyperV = false,
                SiteConfig = new SiteConfig
                {
                    NetFrameworkVersion = "v4.6",
                    AppSettings =
                    {
                        new NameValuePair
                        {
                            Name = "WEBSITE_NODE_DEFAULT_VERSION",
                            Value = "10.14"
                        }
                    },
                    LocalMySqlEnabled = false,
                    Http20Enabled = true
                },
                ScmSiteAlsoStopped = false,
                HttpsOnly = false*/
            };
            ArmOperation<WebSiteResource> lro = await collection.CreateOrUpdateAsync(WaitUntil.Completed, appName, data);
            WebSiteResource app = lro.Value;

            Utilities.Log("Created web app " + app.Data.Name);

            Utilities.Log("CURLing " + app.Data.DefaultHostName + "...");
            Utilities.Log(Utilities.CheckAddress("http://" + app.Data.DefaultHostName));
            return app;
        }
    }
}
