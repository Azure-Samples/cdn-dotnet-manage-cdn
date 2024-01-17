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

namespace ManageCdn
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

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
            try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("CdnRG");
                Utilities.Log($"Creating a resource group..");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log($"Created a resource group with name: {resourceGroup.Data.Name}");

                // ============================================================
                // Create 8 websites
                List<WebSiteResource> websites = new List<WebSiteResource>();

                // 2 in US
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.EastUS));
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.WestUS));

                // 2 in EU
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.NorthEurope));
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.WestEurope));

                // 2 in Southeast
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.EastAsia));
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.SoutheastAsia));

                // 1 in Brazil
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.BrazilSouth));

                // 1 in Japan
                websites.Add(await CreateWebApp(resourceGroup, AzureLocation.JapanWest));

                // =======================================================================================
                // Create CDN profile using Standard Verizon SKU with endpoints in each region of Web apps.
                Utilities.Log("Creating a CDN Profile");
                string afdProfileName = Utilities.CreateRandomName("AFDProfile");
                ProfileData afdProfileInput = new ProfileData("Global", new CdnSku { Name = CdnSkuName.PremiumAzureFrontDoor });
                var afdProfileLro = await resourceGroup.GetProfiles().CreateOrUpdateAsync(WaitUntil.Completed, afdProfileName, afdProfileInput);
                ProfileResource afdProfile = afdProfileLro.Value;


                // Create an endpoint
                Utilities.Log($"Creating a FrontDoor endpoint..");
                string afdEndpointName = Utilities.CreateRandomName("afdtestendpoint");
                FrontDoorEndpointData input = new FrontDoorEndpointData(AzureLocation.WestUS)
                {
                    EnabledState = EnabledState.Enabled,
                };
                var afdEndpointLro = await afdProfile.GetFrontDoorEndpoints().CreateOrUpdateAsync(WaitUntil.Completed, afdEndpointName, input);
                FrontDoorEndpointResource afdEndpoint = afdEndpointLro.Value;

                // Create an origin group
                Utilities.Log($"Creating an origin group..");
                string afdOriginGroupName = Utilities.CreateRandomName("AfdOriginGroup");
                FrontDoorOriginGroupData afdOriginGroupInput = new FrontDoorOriginGroupData
                {
                    HealthProbeSettings = new HealthProbeSettings
                    {
                        ProbePath = "/",
                        ProbeProtocol = HealthProbeProtocol.Http,
                        ProbeRequestType = HealthProbeRequestType.Head,
                        ProbeIntervalInSeconds = 100
                    },
                    LoadBalancingSettings = new LoadBalancingSettings
                    {
                        SampleSize = 4,
                        SuccessfulSamplesRequired = 3,
                        AdditionalLatencyInMilliseconds = 50
                    }
                };
                var afdOriginGroupLro = await afdProfile.GetFrontDoorOriginGroups().CreateOrUpdateAsync(WaitUntil.Completed, afdOriginGroupName, afdOriginGroupInput);
                FrontDoorOriginGroupResource afdOriginGroup = afdOriginGroupLro.Value;

                foreach (var website in websites)
                {
                    // create origin for each region
                    Utilities.Log($"Creating an origin for {website.Data.Location}-{website.Data.Name}");
                    string afdOriginName = Utilities.CreateRandomName("AfdOrigin");
                    FrontDoorOriginData afdOriginInput = new FrontDoorOriginData
                    {
                        HostName = website.Data.DefaultHostName,
                        OriginHostHeader = website.Data.DefaultHostName,
                        HttpPort  = 80,
                        HttpsPort = 443,
                        Priority = 1,
                        Weight = 1000,
                        EnabledState = EnabledState.Enabled,
                    };
                    _ =  await afdOriginGroup.GetFrontDoorOrigins().CreateOrUpdateAsync(WaitUntil.Completed, afdOriginName, afdOriginInput);
                }

                //CreateAfdRoute
                Utilities.Log($"Creating a route");
                string afdRouteName = Utilities.CreateRandomName("AfdRoute");
                FrontDoorRouteData afdRouteDataInput = new FrontDoorRouteData
                {
                    OriginGroupId = afdOriginGroup.Id,
                    LinkToDefaultDomain = LinkToDefaultDomain.Enabled,
                    EnabledState = EnabledState.Enabled,
                    PatternsToMatch = { "/*" },
                    ForwardingProtocol  = ForwardingProtocol.MatchRequest,
                    HttpsRedirect = HttpsRedirect.Enabled
                };
                var afdRouteLro = await afdEndpoint.GetFrontDoorRoutes().CreateOrUpdateAsync(WaitUntil.Completed, afdRouteName, afdRouteDataInput);
                FrontDoorRouteResource afdRoute = afdRouteLro.Value;

                Utilities.Log("Usually, deploying Azure Front Door takes a few minutes.");
                Utilities.Log($"After the AFD deployment is complete, you can browse {afdRoute.Data.EndpointName} to verify.");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
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
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

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
            Utilities.Log($"Creating {location} web app: {appName}...");

            WebSiteCollection collection = resourceGroup.GetWebSites();
            WebSiteData data = new WebSiteData(location){};
            ArmOperation<WebSiteResource> lro = await collection.CreateOrUpdateAsync(WaitUntil.Completed, appName, data);
            WebSiteResource app = lro.Value;

            Utilities.Log("Created web app " + app.Data.Name);
            Utilities.Log("CURLing " + app.Data.DefaultHostName + "...");
            Utilities.Log(Utilities.CheckAddress("http://" + app.Data.DefaultHostName));
            return app;
        }
    }
}
