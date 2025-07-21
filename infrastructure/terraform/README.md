# Universal Translator Infrastructure

This directory contains the Terraform configuration for deploying the Universal Translator application infrastructure to Azure.

## Architecture

The infrastructure includes:
- **Azure Resource Group**: Container for all resources
- **Azure Storage Account**: Stores application data and function app files
- **Azure Storage Table**: Stores chat messages and user data
- **Azure SignalR Service**: Provides real-time communication
- **Azure Functions**: Hosts the application logic
- **Azure Cognitive Services**: Provides translation capabilities
- **Application Insights**: Monitors application performance

## Prerequisites

1. **Azure Subscription**: You need an active Azure subscription
2. **Service Principal**: Create a service principal for GitHub Actions
3. **Terraform State Storage**: Set up a storage account for Terraform state
4. **GitHub Secrets**: Configure the required secrets (see below)

## Setup Instructions

### 1. Create Service Principal

```bash
# Create service principal for GitHub Actions
az ad sp create-for-rbac \
  --name "github-actions-universal-translator" \
  --role="Contributor" \
  --scopes="/subscriptions/YOUR_SUBSCRIPTION_ID" \
  --sdk-auth
```

### 2. Create Terraform State Storage

```bash
# Create resource group for Terraform state
az group create --name "terraform-state-rg" --location "East US"

# Create storage account for Terraform state
az storage account create \
  --name "terraformstateXXXXX" \
  --resource-group "terraform-state-rg" \
  --location "East US" \
  --sku "Standard_LRS" \
  --kind "StorageV2"

# Create container for Terraform state
az storage container create \
  --name "terraform-state" \
  --account-name "terraformstateXXXXX"
```

### 3. Configure GitHub Secrets

Add the following secrets to your GitHub repository:

#### Azure Authentication
- `AZURE_CLIENT_ID`: Service principal client ID
- `AZURE_CLIENT_SECRET`: Service principal client secret
- `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID
- `AZURE_TENANT_ID`: Your Azure tenant ID

#### Terraform Backend
- `TERRAFORM_STORAGE_ACCOUNT`: Name of the storage account for Terraform state
- `TERRAFORM_CONTAINER_NAME`: Name of the container (usually "terraform-state")
- `TERRAFORM_RESOURCE_GROUP`: Name of the resource group containing the storage account

## Local Development

### Prerequisites
- [Terraform](https://www.terraform.io/downloads.html) >= 1.0
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

### Setup
1. Login to Azure:
   ```bash
   az login
   ```

2. Initialize Terraform:
   ```bash
   cd infrastructure/terraform
   terraform init
   ```

3. Plan the deployment:
   ```bash
   terraform plan -var-file="terraform.tfvars.dev"
   ```

4. Apply the changes:
   ```bash
   terraform apply -var-file="terraform.tfvars.dev"
   ```

## Environment Configuration

### Development Environment
- File: `terraform.tfvars.dev`
- Resources optimized for development and testing
- Uses free tiers where possible

### Production Environment
- File: `terraform.tfvars.prod`
- Resources optimized for production workloads
- Includes redundancy and monitoring

## Customization

### Variables
All configurable parameters are defined in `variables.tf`. You can customize:
- Resource names and locations
- SKUs and capacity settings
- Environment-specific configurations

### Environment Files
Create additional `.tfvars` files for different environments:
```bash
cp terraform.tfvars.dev terraform.tfvars.staging
# Edit the new file with staging-specific values
```

## Deployment Pipeline

The GitHub Actions workflow (`infrastructure-deploy.yml`) handles:
1. **Terraform Plan**: Validates and plans changes on pull requests
2. **Terraform Apply**: Deploys changes on merge to main/develop
3. **Terraform Destroy**: Destroys resources when manually triggered

### Workflow Triggers
- **Pull Request**: Runs terraform plan
- **Push to main**: Deploys to production
- **Push to develop**: Deploys to development
- **Manual Trigger**: Allows choosing environment and action

## Security Considerations

1. **State File Security**: Terraform state is stored in Azure Storage with access controls
2. **Secrets Management**: All sensitive values are stored in GitHub Secrets
3. **Service Principal**: Uses least privilege access model
4. **Resource Access**: Resources are configured with appropriate access controls

## Monitoring and Logging

- **Application Insights**: Automatically configured for the Function App
- **Resource Tagging**: All resources are tagged for cost tracking and management
- **Deployment Logging**: GitHub Actions provides detailed deployment logs

## Troubleshooting

### Common Issues
1. **Storage Account Name Conflicts**: Storage account names must be globally unique
2. **Resource Quota**: Check Azure subscription limits
3. **Service Principal Permissions**: Ensure the service principal has sufficient permissions

### Debugging
1. Check GitHub Actions logs for detailed error messages
2. Use `terraform plan` locally to validate changes
3. Review Azure Activity Log for deployment issues

## Cost Optimization

- **Development**: Uses free tiers and minimal resources
- **Production**: Optimized for performance with cost-effective SKUs
- **Cleanup**: Use `terraform destroy` to remove resources when not needed

## Support

For issues related to:
- **Terraform Configuration**: Check the Terraform documentation
- **Azure Resources**: Refer to Azure documentation
- **GitHub Actions**: Review GitHub Actions documentation
- **Application Issues**: Check the application logs in Application Insights
