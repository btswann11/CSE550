# Variables for Universal Translator Infrastructure
# This file defines all configurable parameters for the deployment

variable "project_name" {
  description = "The name of the project, used as a prefix for all resources"
  type        = string
  default     = "cse550-universal-translator"

  validation {
    condition     = can(regex("^[a-zA-Z0-9-]+$", var.project_name))
    error_message = "Project name must contain only alphanumeric characters and hyphens."
  }
}

variable "environment" {
  description = "The environment (dev, staging, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  description = "The Azure region where resources will be deployed"
  type        = string
  default     = "East US"
}

variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
  default     = ""
}

variable "storage_account_name" {
  description = "The name of the storage account (must be globally unique)"
  type        = string
  default     = ""
}

variable "signalr_service_name" {
  description = "The name of the SignalR service"
  type        = string
  default     = ""
}

variable "function_app_name" {
  description = "The name of the function app"
  type        = string
  default     = ""
}

variable "cognitive_account_name" {
  description = "The name of the cognitive services account"
  type        = string
  default     = ""
}

variable "service_plan_name" {
  description = "The name of the service plan"
  type        = string
  default     = ""
}

variable "storage_table_name" {
  description = "The name of the storage table for chat data"
  type        = string
  default     = "utchats"
}

variable "storage_container_name" {
  description = "The name of the storage container"
  type        = string
  default     = "function-app-storage"
}

# Configuration variables
variable "signalr_sku" {
  description = "The SKU for SignalR service"
  type = object({
    name     = string
    capacity = number
  })
  default = {
    name     = "Standard_S1"
    capacity = 1
  }
}

variable "storage_account_tier" {
  description = "The storage account tier"
  type        = string
  default     = "Standard"

  validation {
    condition     = contains(["Standard", "Premium"], var.storage_account_tier)
    error_message = "Storage account tier must be either Standard or Premium."
  }
}

variable "storage_replication_type" {
  description = "The storage account replication type"
  type        = string
  default     = "LRS"

  validation {
    condition     = contains(["LRS", "GRS", "RAGRS", "ZRS"], var.storage_replication_type)
    error_message = "Storage replication type must be one of: LRS, GRS, RAGRS, ZRS."
  }
}

variable "function_app_sku" {
  description = "The SKU for the function app service plan"
  type        = string
  default     = "FC1"
}

variable "function_app_max_instances" {
  description = "Maximum number of instances for the function app"
  type        = number
  default     = 40
}

variable "function_app_memory_mb" {
  description = "Memory allocation for function app instances in MB"
  type        = number
  default     = 1024
}

variable "cognitive_services_sku" {
  description = "The SKU for Cognitive Services (F0 for free tier, S1 for standard)"
  type        = string
  default     = "F0"

  validation {
    condition     = contains(["F0", "S1", "S2", "S3", "S4"], var.cognitive_services_sku)
    error_message = "Cognitive Services SKU must be one of: F0, S1, S2, S3, S4."
  }
}

# Tags
variable "tags" {
  description = "A map of tags to assign to the resources"
  type        = map(string)
  default = {
    Project     = "Universal Translator"
    Environment = "dev"
    ManagedBy   = "Terraform"
  }
}
