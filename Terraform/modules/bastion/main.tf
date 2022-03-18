
resource "azurerm_container_registry" "acr" {
  name                = "${var.PREFIX}BastionContainerRegistry"
  resource_group_name = var.RESOURCE_GROUP.name
  location            = var.RESOURCE_GROUP.location
  sku                 = "Standard"
  admin_enabled       = true
}

resource "azurerm_container_registry_task" "build_bastion_image_task" {
  name                  = "build_bastion_image_task"
  container_registry_id = azurerm_container_registry.acr.id
  platform {
    os = "Linux"
  }
  docker_step {
    dockerfile_path      = "Dockerfile"
    context_path         = "https://github.com/wongcyrus/ssh-tunneling-bastion#main"
    context_access_token = "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"
    image_names          = ["ssh-tunneling-bastion:latest"]   
  }
}

data "http" "docker_file" {
  url = "https://raw.githubusercontent.com/wongcyrus/ssh-tunneling-bastion/master/Dockerfile"
}

resource "null_resource" "run_arc_task" {
  provisioner "local-exec" {
    command = "az acr task run --registry ${azurerm_container_registry.acr.name} --name build_bastion_image_task"
  }
  depends_on = [azurerm_container_registry_task.build_bastion_image_task]
  triggers = {
    dockerfile = data.http.docker_file.body
  }
}