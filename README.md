---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
  services: Cdn
  platforms: dotnet
---

# Getting started on managing CDN in C# #

 Azure CDN sample for managing CDN profiles:
 - Create 8 web apps in 8 regions:
     2 in US
     2 in EU
     2 in Southeast
     1 in Brazil
     1 in Japan
 - Create `Azure Front Door and CDN profile` and its endpoint
 - Create origin for each region in the same origin group
 - Create a route for endpoint

## Running this Sample ##

To run this sample:

Set the environment variable `CLIENT_ID`,`CLIENT_SECRET`,`TENANT_ID`,`SUBSCRIPTION_ID` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/cdn-dotnet-manage-cdn.git

    cd cdn-dotnet-manage-cdn

    dotnet build

    bin\Debug\net452\ManageCdn.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.