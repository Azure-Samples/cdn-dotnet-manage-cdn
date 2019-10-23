---
page_type: sample
languages:
- csharp
products:
- dotnet
- azure
- azure-cdn
extensions:
- services: Cdn
- platforms: dotnet
urlFragment: getting-started-on-managing-cdn-in-c
description: "Azure CDN sample for managing CDN profiles."
---

# Get started managing Azure CDN (C#)

Azure CDN sample for managing CDN profiles.

- Create 8 web apps in 8 regions:
    2 in US
    2 in EU
    2 in Southeast
    1 in Brazil
    1 in Japan
- Create CDN profile using Standard Verizon SKU with endpoints in each region of Web apps.
- Load some content (referenced by Web Apps) to the CDN endpoints.


## Running this sample

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

```bash
git clone https://github.com/Azure-Samples/cdn-dotnet-manage-cdn.git
cd cdn-dotnet-manage-cdn
dotnet build
bin\Debug\net452\ManageCdn.exe
```

## More information

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
