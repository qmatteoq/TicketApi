#!/bin/bash

# Prompt for resource group name
read -p "Enter the name of the resource group: " resourceGroupName

# Set default location
location="westeurope"

# Check if the resource group exists
az group show --name "$resourceGroupName" &> /dev/null

if [ $? -ne 0 ]; then
    echo "Resource group '$resourceGroupName' does not exist. Creating it in location '$location'..."
    az group create --name "$resourceGroupName" --location "$location"
else
    echo "Resource group '$resourceGroupName' already exists."
fi

# Deploy the Bicep template
az deployment group create --resource-group "$resourceGroupName" --template-file deploy.bicep
