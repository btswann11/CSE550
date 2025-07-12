# Local values for computed names and configurations
# This file contains computed values based on variables

locals {
  # Computed names based on project name and environment
  resource_group_name    = var.resource_group_name != "" ? var.resource_group_name : "${var.project_name}-${var.environment}-rg"
  storage_account_name   = var.storage_account_name != "" ? var.storage_account_name : replace("${var.project_name}${var.environment}storage", "-", "")
  signalr_service_name   = var.signalr_service_name != "" ? var.signalr_service_name : "${var.project_name}-${var.environment}-signalr"
  function_app_name      = var.function_app_name != "" ? var.function_app_name : "${var.project_name}-${var.environment}-func"
  cognitive_account_name = var.cognitive_account_name != "" ? var.cognitive_account_name : "${var.project_name}-${var.environment}-cognitive"
  service_plan_name      = var.service_plan_name != "" ? var.service_plan_name : "${var.project_name}-${var.environment}-plan"

  # Ensure storage account name meets Azure requirements (3-24 chars, lowercase, no hyphens)
  storage_account_name_clean = lower(substr(replace(local.storage_account_name, "-", ""), 0, 24))

  # Merged tags
  common_tags = merge(var.tags, {
    Environment = var.environment
    Location    = var.location
  })
}
