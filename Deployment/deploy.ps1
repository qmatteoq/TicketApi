# Prompt for resource group name
$resourceGroupName = Read-Host "Enter the name of the resource group"

# Set default location
$location = "westeurope"

# Check if the resource group exists
$rg = az group show --name $resourceGroupName --output none 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Resource group '$resourceGroupName' does not exist. Creating it in location '$location'..."
    az group create --name $resourceGroupName --location $location
} else {
    Write-Host "Resource group '$resourceGroupName' already exists."
}

# Deploy the Bicep template
az deployment group create --resource-group $resourceGroupName --template-file deploy.bicep