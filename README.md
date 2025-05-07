# Ticket API

This repository an Azure Function which simulates an API for a ticketing system. It is designed to be used as a mock API for testing and development purposes. The API provides endpoints to create, retrieve, update, and delete tickets.

## Architecture
The project consists of an Azure Function, based on .NET 8, which serves a single API endpoint, `/tickets`, which supports the following HTTP methods:

- `POST`, to create a new ticket
- `GET`, to retrieve tickets (you can get the full list or use different query string parameters to filter the results) 
- `DELETE`, to delete a ticket

Once you run the project, you can explore the endpoints and the required payloads using Swagger, which is available at `http://localhost:7071/api/swagger/ui`.
The OpenAPI specification is also available at `http://localhost:7071//api/swagger.json`.

## Requirements

- [Visual Studio Code](https://code.visualstudio.com/)
- [The Azure Tools extensions pack](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-node-azure-pack)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)

# Getting Started

1) Clone the repository to your local machine.
2) Open the project in Visual Studio Code.
3) Open the **Run and debug** panel in Visual Studio Code
4) Press **F5** or click on "Attach to .NET Functions" to start the Azure Function locally.
5) Once the function is running, you can explore and test the API by using Swagger, which is available at `http://localhost:7071/api/swagger/ui`.

# Deploying to Azure

1) Make sure you have the Azure CLI installed and configured.
2) Run the following command to log in to your Azure account and to select the subscription you want to use:

   ```bash
   az login
   ```

3) Open a terminal and navigate to the Deployment folder in the repository.
4) By default, the script will deploy the required resources in the Western Europe region. If you want to deploy it in a different region, open the `deploy.ps1` file and change the `$location` variable to your desired region:
   
   ```powershell
    # Set default location
    $location = "westeurope"
   ```
5) Run the `deploy.ps1` script to deploy the Azure Function and the required resources:

   ```powershell
   .\deploy.ps1
   ```

   At first, you will be asked to provide a name for the resource group which will host the resources. If it doesn't exist, it will be created. If it already exists, the script will use the existing resource group.

    The script will deploy the following resources:
        
     - An App Service Plan
     - An Azure Function App
     - An Azure Storage Account

    It will also assign the proper permissions to the Function App to access the Storage Account using a [managed identity](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview).

6) Now make sure, in Visual Studio Code, to be signed with the same Azure account in the Azure extension. You can do this by clicking on the Azure icon in the left sidebar and then clicking on the "Sign in to Azure" button.
7) Once you are signed in, you can deploy the function to Azure by right-clicking on an empty space in the Explorer panel and choosing **Deploy to Function App**
8) Pick the Azure subscription you want to use and select the Function App that was created by the script. The deployment will take a few minutes to complete.

You're good to go! Now your Ticket API is available at the URL assigned to your Azure Function app.