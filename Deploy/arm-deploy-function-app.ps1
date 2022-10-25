# create a resource group
$resourceGroup = "Akaunting"
$location = "westeurope"
az group create -n $resourceGroup -l $location

# to check our deployement app
az deployment group what-if --resource-group $resourceGroup --template-file Deploy\main.bicep

# to deploy our function app
az deployment group create --resource-group $resourceGroup --template-file Deploy\main.bicep

# see what's in the resource group we just created
az resource list -g $resourceGroup -o table

$functionName = ($functionAppName + "Function")

# check the app settings were configured correctly
az functionapp config appsettings list `
    -n $functionName -g $resourceGroup -o table

# to clean up when we're done
az group delete -n $resourceGroup --no-wait -y
