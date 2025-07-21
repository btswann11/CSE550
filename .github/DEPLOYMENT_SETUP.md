# GitHub Actions Deployment Setup Guide

## Prerequisites Setup Required

Before the deployment workflow can run successfully, you need to configure several components in Azure and GitHub.

## 1. Azure Prerequisites

### Create Service Principal for GitHub Actions

```bash
# Set your subscription ID
SUBSCRIPTION_ID="your-subscription-id"

# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "sp-universal-translator-github" \
  --role "Contributor" \
  --scopes "/subscriptions/$SUBSCRIPTION_ID" \
  --json-auth

# Save the output - you'll need these values for GitHub secrets
```

### Create Terraform State Storage

```bash
# Variables
TERRAFORM_RG="terraform-state-rg"
TERRAFORM_STORAGE="tfstate$(date +%s)"  # Must be globally unique
TERRAFORM_CONTAINER="terraform-state"
LOCATION="East US"

# Create resource group
az group create --name $TERRAFORM_RG --location "$LOCATION"

# Create storage account
az storage account create \
  --name $TERRAFORM_STORAGE \
  --resource-group $TERRAFORM_RG \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --kind StorageV2

# Create container
az storage container create \
  --name $TERRAFORM_CONTAINER \
  --account-name $TERRAFORM_STORAGE

# Get storage account key (optional - for verification)
az storage account keys list \
  --account-name $TERRAFORM_STORAGE \
  --resource-group $TERRAFORM_RG \
  --query '[0].value' \
  --output tsv
```

## 2. GitHub Repository Configuration

### Required Secrets

Add these secrets in your GitHub repository settings (`Settings > Secrets and variables > Actions`):

#### Azure Authentication
- `AZURE_CLIENT_ID`: Client ID from service principal creation
- `AZURE_CLIENT_SECRET`: Client secret from service principal creation  
- `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID
- `AZURE_TENANT_ID`: Your Azure tenant ID

#### Terraform Backend
- `TERRAFORM_STORAGE_ACCOUNT`: Name of the storage account created above
- `TERRAFORM_CONTAINER_NAME`: `terraform-state` (or your chosen container name)
- `TERRAFORM_RESOURCE_GROUP`: `terraform-state-rg` (or your chosen RG name)

### Required Environments

Create these environments in your GitHub repository (`Settings > Environments`):

1. **dev**
   - No protection rules needed for development
   - Optional: Add reviewers if desired

2. **prod** 
   - **Recommended:** Add protection rules
   - **Recommended:** Require review from administrators
   - **Recommended:** Add deployment branches rule (only `main`)

3. **dev-destroy**
   - **Recommended:** Require review before destruction
   - Used when manually destroying dev environment

4. **prod-destroy**
   - **Required:** Require review before destruction
   - **Required:** Restrict to administrators only
   - Used when manually destroying production environment

## 3. Workflow Validation

### Expected vs. Actual Issues

The following are **EXPECTED** and indicate missing configuration (not errors):

✅ **Expected (Need Configuration):**
- `Context access might be invalid: AZURE_CLIENT_ID` - Need to add secret
- `Context access might be invalid: TERRAFORM_STORAGE_ACCOUNT` - Need to add secret
- `Unable to resolve action actions/checkout@v4` - Action version validation

❌ **Actual Issues (Fixed):**
- Function App portal link was using wrong terraform output
- Missing `function_app_name` output for portal links

### Test the Workflow

1. **Dry Run Test:**
   ```bash
   # Test locally first
   cd infrastructure/terraform
   terraform init
   terraform validate
   terraform plan -var-file="terraform.tfvars.dev"
   ```

2. **GitHub Actions Test:**
   - Create a pull request with a small change to trigger plan
   - Check if all secrets are configured correctly
   - Verify terraform plan runs successfully

## 4. Environment-Specific Configuration

### Development Environment
- Deploys on push to `develop` branch
- Uses `terraform.tfvars.dev`
- Free tier resources where possible

### Production Environment  
- Deploys on push to `main` branch
- Uses `terraform.tfvars.prod`
- Production-ready resource configurations

## 5. Manual Deployment Options

### Via GitHub Actions UI
1. Go to `Actions` tab in GitHub
2. Select "Deploy Universal Translator Infrastructure"
3. Click "Run workflow"
4. Choose:
   - Environment: `dev` or `prod`
   - Action: `plan`, `apply`, or `destroy`

### Via Git Operations
- **Plan:** Create pull request → automatic plan
- **Deploy Dev:** Push to `develop` branch → automatic apply
- **Deploy Prod:** Push to `main` branch → automatic apply

## 6. Troubleshooting

### Common Issues
1. **"Backend not configured"**: Add terraform backend secrets
2. **"Service principal permissions"**: Ensure Contributor role
3. **"Storage account not found"**: Verify storage account exists and name is correct
4. **"Environment protection"**: Configure GitHub environments

### Debugging Steps
1. Check GitHub Actions logs for specific error messages
2. Verify all secrets are correctly named and populated
3. Test terraform commands locally first
4. Ensure Azure CLI authentication works locally

### Emergency Access
If automated deployment fails, you can always deploy manually:
```bash
# Manual deployment
cd infrastructure/terraform
terraform init -backend-config="storage_account_name=<name>" # etc.
terraform plan -var-file="terraform.tfvars.prod"
terraform apply -var-file="terraform.tfvars.prod"
```

## Security Considerations

1. **Secret Rotation**: Regularly rotate service principal credentials
2. **Least Privilege**: Review and minimize service principal permissions
3. **Environment Protection**: Use GitHub environment protection rules
4. **State File Security**: Terraform state contains sensitive data
5. **Resource Tagging**: All resources are tagged for cost tracking

## Next Steps

After setup:
1. ✅ Configure all secrets and environments
2. ✅ Test with a small infrastructure change
3. ✅ Deploy to development environment
4. ✅ Validate deployment and test application
5. ✅ Deploy to production environment
6. ✅ Set up monitoring and alerting
