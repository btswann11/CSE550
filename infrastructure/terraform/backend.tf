# Terraform Backend Configuration
# This file configures the remote state storage for Terraform

terraform {
  backend "azurerm" {
    # Backend configuration is provided via command line or environment variables
    # See the GitHub Actions workflow for the configuration details
  }
}
